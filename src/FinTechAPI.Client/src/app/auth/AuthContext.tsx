import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  type ReactNode,
} from "react";
import { useNavigate } from "react-router";
import {
  login as apiLogin,
  logout as apiLogout,
  getMyProfile,
  type ApiUserProfile,
} from "../api/client";

interface AuthState {
  user: ApiUserProfile | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  refreshProfile: () => Promise<void>;
}

const AuthContext = createContext<AuthState | null>(null);

function hasStoredToken(): boolean {
  return !!(
    localStorage.getItem("fintech_token") || localStorage.getItem("token")
  );
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<ApiUserProfile | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const navigate = useNavigate();

  const fetchProfile = useCallback(async () => {
    if (!hasStoredToken()) {
      setUser(null);
      setIsLoading(false);
      return;
    }

    try {
      const profile = await getMyProfile();
      setUser(profile);
    } catch {
      // token might be expired – clear it
      apiLogout();
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchProfile();
  }, [fetchProfile]);

  const login = useCallback(async (email: string, password: string) => {
    await apiLogin(email, password);
    const profile = await getMyProfile();
    setUser(profile);
  }, []);

  const logout = useCallback(() => {
    apiLogout();
    setUser(null);
    navigate("/login", { replace: true });
  }, [navigate]);

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        login,
        logout,
        refreshProfile: fetchProfile,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within <AuthProvider>");
  return ctx;
}
