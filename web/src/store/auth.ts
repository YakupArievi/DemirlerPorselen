import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { AuthResponse, UserInfo } from '../api/types';

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: UserInfo | null;
  setAuth: (auth: AuthResponse) => void;
  setTokens: (accessToken: string, refreshToken: string) => void;
  clear: () => void;
}

export const useAuth = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      user: null,
      setAuth: (auth) =>
        set({ accessToken: auth.accessToken, refreshToken: auth.refreshToken, user: auth.user }),
      setTokens: (accessToken, refreshToken) => set({ accessToken, refreshToken }),
      clear: () => set({ accessToken: null, refreshToken: null, user: null }),
    }),
    { name: 'toptanci-auth' }
  )
);

// React dışından (axios interceptor) erişim için
export const authStore = useAuth;
