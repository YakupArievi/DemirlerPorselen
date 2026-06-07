import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { authStore } from '../store/auth';

// Dev'de '/api' (vite proxy -> 5080). Yayında VITE_API_URL ile ngrok/bulut backend adresi verilir,
// ör: VITE_API_URL=https://demirler.ngrok-free.app/api
export const API_BASE = import.meta.env.VITE_API_URL ?? '/api';

export const api = axios.create({ baseURL: API_BASE });

api.interceptors.request.use((config) => {
  const token = authStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  // ngrok ücretsiz katmanındaki tarayıcı uyarı sayfasını atla
  config.headers['ngrok-skip-browser-warning'] = 'true';
  return config;
});

let refreshing: Promise<string | null> | null = null;

async function refreshAccessToken(): Promise<string | null> {
  const { refreshToken, setTokens, clear } = authStore.getState();
  if (!refreshToken) return null;
  try {
    // Interceptor'sız ham çağrı (sonsuz döngüyü önle)
    const res = await axios.post(`${API_BASE}/auth/refresh`, { refreshToken },
      { headers: { 'ngrok-skip-browser-warning': 'true' } });
    setTokens(res.data.accessToken, res.data.refreshToken);
    return res.data.accessToken as string;
  } catch {
    clear();
    return null;
  }
}

api.interceptors.response.use(
  (res) => res,
  async (error: AxiosError) => {
    const original = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
    if (error.response?.status === 401 && original && !original._retry) {
      original._retry = true;
      refreshing ??= refreshAccessToken().finally(() => { refreshing = null; });
      const newToken = await refreshing;
      if (newToken) {
        original.headers.Authorization = `Bearer ${newToken}`;
        return api(original);
      }
    }
    return Promise.reject(error);
  }
);

export function apiErrorMessage(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data as { message?: string; errors?: Record<string, string[]> } | undefined;
    if (data?.errors) return Object.values(data.errors).flat().join(' ');
    if (data?.message) return data.message;
    return err.message;
  }
  return 'Beklenmeyen bir hata oluştu.';
}
