import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { authStore } from '../store/auth';

export const api = axios.create({ baseURL: '/api' });

api.interceptors.request.use((config) => {
  const token = authStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

let refreshing: Promise<string | null> | null = null;

async function refreshAccessToken(): Promise<string | null> {
  const { refreshToken, setTokens, clear } = authStore.getState();
  if (!refreshToken) return null;
  try {
    // Interceptor'sız ham çağrı (sonsuz döngüyü önle)
    const res = await axios.post('/api/auth/refresh', { refreshToken });
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
