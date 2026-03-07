export interface ApiAccount {
  id: number;
  name: string;
  balance: number;
}

export interface ApiTransaction {
  id: number;
  amount: number;
  currency: number | string;
  type: number | string;
  description?: string | null;
  transactionDate: string;
  createdAt: string;
  updatedAt: string;
  accountId: number;
  userId: string;
}

interface ApiError {
  message?: string;
  title?: string;
  description?: string;
}

interface ApiIdentityError {
  code?: string;
  description?: string;
}

export interface AuthResponse {
  token: string;
  refreshToken?: string;
  expiration: string;
  success: boolean;
  errorMessage?: string;
}

export interface RegisterPayload {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

const currencyLabels = [
  "USD",
  "EUR",
  "GBP",
  "JPY",
  "CNY",
  "RUB",
  "AUD",
  "CAD",
  "CHF",
  "INR",
] as const;

const transactionTypeLabels = ["Income", "Expense", "Transfer"] as const;

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? "").replace(/\/$/, "");

function getStoredToken(): string | null {
  return localStorage.getItem("fintech_token") || localStorage.getItem("token");
}

function buildUrl(path: string): string {
  if (API_BASE_URL) {
    return `${API_BASE_URL}${path}`;
  }

  return path;
}

async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getStoredToken();
  const headers = new Headers(init?.headers);

  if (!headers.has("Content-Type") && init?.body) {
    headers.set("Content-Type", "application/json");
  }

  if (token && !headers.has("Authorization")) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(buildUrl(path), {
    ...init,
    credentials: "include",
    headers,
  });

  if (!response.ok) {
    let errorMessage = `HTTP ${response.status}`;

    try {
      const data = (await response.json()) as ApiError | ApiIdentityError[];

      if (Array.isArray(data)) {
        errorMessage = data.map((item) => item.description || item.code).filter(Boolean).join("; ") || errorMessage;
      } else {
        errorMessage = data.message || data.title || data.description || errorMessage;
      }
    } catch {
      // ignore JSON parse errors for non-JSON responses
    }

    throw new Error(errorMessage);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function getCurrencyLabel(value: number | string): string {
  if (typeof value === "string") {
    return value;
  }

  return currencyLabels[value] ?? "UNKNOWN";
}

export function getTransactionTypeLabel(value: number | string): string {
  if (typeof value === "string") {
    return value;
  }

  return transactionTypeLabels[value] ?? "Unknown";
}

export function toCurrencyValue(value: string): number {
  const index = currencyLabels.findIndex((entry) => entry === value);
  return index >= 0 ? index : 5;
}

export const transactionTypeValues = {
  income: 0,
  expense: 1,
  transfer: 2,
} as const;

export async function login(email: string, password: string) {
  const response = await apiRequest<AuthResponse>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });

  if (response.token) {
    localStorage.setItem("fintech_token", response.token);
  }

  return response;
}

export function register(payload: RegisterPayload) {
  return apiRequest("/api/auth/register", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function logout() {
  localStorage.removeItem("fintech_token");
  localStorage.removeItem("token");
}

export function getAccounts() {
  return apiRequest<ApiAccount[]>("/api/accounts");
}

export function getTransactions() {
  return apiRequest<ApiTransaction[]>("/api/transactions");
}

export function createTransaction(payload: {
  amount: number;
  currency: number;
  type: number;
  description: string;
  transactionDate: string;
  accountId: number;
}) {
  return apiRequest<ApiTransaction>("/api/transactions", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export async function measureApiLatency(): Promise<number> {
  const start = performance.now();
  await apiRequest<{ status: string }>("/api/test/status");
  return performance.now() - start;
}
