import axios from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';

// Geliştirmede backend adresi (cihazdan erişilebilir IP ile değiştirin)
export const API_BASE = 'http://10.0.2.2:5080/api'; // Android emülatör -> host makinesi

export const api = axios.create({ baseURL: API_BASE });

api.interceptors.request.use(async (config) => {
  const token = await AsyncStorage.getItem('accessToken');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export async function login(userName: string, password: string) {
  const res = await api.post('/auth/login', { userName, password });
  await AsyncStorage.setItem('accessToken', res.data.accessToken);
  await AsyncStorage.setItem('refreshToken', res.data.refreshToken);
  return res.data;
}

export async function logout() {
  await AsyncStorage.multiRemove(['accessToken', 'refreshToken']);
}
