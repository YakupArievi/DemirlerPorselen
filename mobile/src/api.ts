import axios from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';

// Backend adresi (PC LAN IP — DHCP ile değişirse güncelle):
export const API_BASE = 'http://192.168.1.17:5080/api';

export type Mode = 'staff' | 'portal';

const MODE = 'mode';
const ACCESS = 'access';
const REFRESH = 'refresh';
const USER = 'user';

export const api = axios.create({ baseURL: API_BASE });

api.interceptors.request.use(async (config) => {
  const token = await AsyncStorage.getItem(ACCESS);
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

let refreshing: Promise<string | null> | null = null;

async function doRefresh(): Promise<string | null> {
  const rt = await AsyncStorage.getItem(REFRESH);
  const mode = (await AsyncStorage.getItem(MODE)) as Mode | null;
  if (!rt || !mode) return null;
  const endpoint = mode === 'staff' ? '/auth/refresh' : '/portal/auth/refresh';
  try {
    const res = await axios.post(`${API_BASE}${endpoint}`, { refreshToken: rt });
    await AsyncStorage.multiSet([[ACCESS, res.data.accessToken], [REFRESH, res.data.refreshToken]]);
    return res.data.accessToken as string;
  } catch {
    await AsyncStorage.multiRemove([ACCESS, REFRESH, MODE, USER]);
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
      if (token) { original.headers.Authorization = `Bearer ${token}`; return api(original); }
    }
    return Promise.reject(error);
  }
);

export interface Session { mode: Mode; user: any; }

export async function staffLogin(userName: string, password: string): Promise<Session> {
  const res = await axios.post(`${API_BASE}/auth/login`, { userName, password });
  await AsyncStorage.multiSet([
    [MODE, 'staff'], [ACCESS, res.data.accessToken], [REFRESH, res.data.refreshToken],
    [USER, JSON.stringify(res.data.user)],
  ]);
  return { mode: 'staff', user: res.data.user };
}

export async function portalLogin(phone: string, password: string): Promise<Session> {
  const res = await axios.post(`${API_BASE}/portal/auth/login`, { phone, password });
  await AsyncStorage.multiSet([
    [MODE, 'portal'], [ACCESS, res.data.accessToken], [REFRESH, res.data.refreshToken],
    [USER, JSON.stringify(res.data.customer)],
  ]);
  return { mode: 'portal', user: res.data.customer };
}

export async function getSession(): Promise<Session | null> {
  const mode = (await AsyncStorage.getItem(MODE)) as Mode | null;
  const access = await AsyncStorage.getItem(ACCESS);
  const userStr = await AsyncStorage.getItem(USER);
  if (!mode || !access) return null;
  return { mode, user: userStr ? JSON.parse(userStr) : null };
}

export async function logout() {
  const rt = await AsyncStorage.getItem(REFRESH);
  const mode = (await AsyncStorage.getItem(MODE)) as Mode | null;
  const at = await AsyncStorage.getItem(ACCESS);
  const endpoint = mode === 'staff' ? '/auth/logout' : '/portal/auth/logout';
  try { if (rt) await axios.post(`${API_BASE}${endpoint}`, { refreshToken: rt }, { headers: { Authorization: `Bearer ${at}` } }); } catch { /* yoksay */ }
  await AsyncStorage.multiRemove([ACCESS, REFRESH, MODE, USER]);
}

export function apiError(e: any): string {
  const d = e?.response?.data;
  if (d?.errors) return Object.values(d.errors).flat().join(' ');
  return d?.message ?? 'Bağlantı hatası. Sunucuya ulaşılamıyor olabilir.';
}

export async function getAccessToken() { return AsyncStorage.getItem(ACCESS); }

export const tl = (v: number) => `${(v ?? 0).toFixed(2)} TL`;
export const dateStr = (s: string) => new Date(s).toLocaleDateString('tr-TR');
