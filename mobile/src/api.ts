import axios from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';

// Geliştirmede backend adresi:
//  - Android emülatör -> http://10.0.2.2:5080
//  - iOS simülatör   -> http://localhost:5080
//  - Gerçek cihaz    -> http://<bilgisayar-LAN-IP>:5080
export const API_BASE = 'http://10.0.2.2:5080/api';

const ACCESS = 'portalAccess';
const REFRESH = 'portalRefresh';

export const api = axios.create({ baseURL: API_BASE });

api.interceptors.request.use(async (config) => {
  const token = await AsyncStorage.getItem(ACCESS);
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

let refreshing: Promise<string | null> | null = null;

async function doRefresh(): Promise<string | null> {
  const rt = await AsyncStorage.getItem(REFRESH);
  if (!rt) return null;
  try {
    const res = await axios.post(`${API_BASE}/portal/auth/refresh`, { refreshToken: rt });
    await AsyncStorage.multiSet([[ACCESS, res.data.accessToken], [REFRESH, res.data.refreshToken]]);
    return res.data.accessToken as string;
  } catch {
    await AsyncStorage.multiRemove([ACCESS, REFRESH]);
    return null;
  }
}

api.interceptors.response.use(
  (r) => r,
  async (error) => {
    const original = error.config;
    if (error.response?.status === 401 && original && !original._retry) {
      original._retry = true;
      refreshing ??= doRefresh().finally(() => { refreshing = null; });
      const token = await refreshing;
      if (token) {
        original.headers.Authorization = `Bearer ${token}`;
        return api(original);
      }
    }
    return Promise.reject(error);
  }
);

export async function portalLogin(phone: string, password: string) {
  const res = await axios.post(`${API_BASE}/portal/auth/login`, { phone, password });
  await AsyncStorage.multiSet([[ACCESS, res.data.accessToken], [REFRESH, res.data.refreshToken]]);
  return res.data;
}

export async function portalLogout() {
  const rt = await AsyncStorage.getItem(REFRESH);
  const at = await AsyncStorage.getItem(ACCESS);
  try {
    if (rt) await axios.post(`${API_BASE}/portal/auth/logout`, { refreshToken: rt }, { headers: { Authorization: `Bearer ${at}` } });
  } catch { /* yoksay */ }
  await AsyncStorage.multiRemove([ACCESS, REFRESH]);
}

export async function hasSession() {
  return !!(await AsyncStorage.getItem(ACCESS));
}

// Veri uçları (yalnızca giriş yapan müşteriye ait)
export const getBalance = async () => (await api.get('/portal/me/balance')).data as number;
export const getStatement = async () => (await api.get('/portal/me/statement')).data;
export const getSales = async () => (await api.get('/portal/me/sales')).data;
