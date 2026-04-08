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
  status?: number | string;
  businessStatus?: number | string;
  providerStatus?: string | null;
  providerReference?: string | null;
  paymentId?: string | null;
  webhookEvent?: string | null;
  correlationId?: string | null;
  providerUpdatedAt?: string | null;
  riskLevel?: string;
  fraudDecision?: string;
  category?: string;
  description?: string | null;
  transactionDate: string;
  createdAt: string;
  updatedAt: string;
  accountId: number;
  userId: string;
}

export interface ApiCreatePaymentIntentPayload {
  amount: number;
  currency: string;
  description?: string;
  transactionId?: string;
}

export interface ApiPaymentIntentResponse {
  paymentId: string;
  stripePaymentIntentId: string;
  clientSecret: string;
  status: string;
  amount: number;
  currency: string;
  transactionId?: string | null;
  fraudDecision?: string | null;
  fraudScore?: number | null;
  fraudEvaluationId?: string | null;
}

export interface ApiPayment {
  id: string;
  userId: string;
  amount: number;
  currency: string;
  status: string;
  stripePaymentIntentId: string;
  transactionId?: string | null;
  lastWebhookEvent?: string | null;
  lastStripeEventId?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ApiPlatformBalance {
  available: number;
  pending: number;
  currency: string;
  source: string;
  syncedAt: string;
}

export interface ApiPlatformSummary {
  processedVolume: number;
  successfulPayments: number;
  failedPayments: number;
  pendingReviewCount: number;
  fraudBlockedCount: number;
  currency: string;
  source: string;
  syncedAt: string;
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

export interface ApiAuthOperationResult {
  success: boolean;
  message: string;
}

export interface ApiUserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phone: string;
  location: string;
  createdAt: string;
  emailVerified: boolean;
  role: string;
}

export interface ApiUpdateUserProfilePayload {
  firstName: string;
  lastName: string;
  phone: string;
  location: string;
}

export interface ApiUserSettings {
  theme: string;
  language: string;
  defaultCurrency: string;
  transactionNotifications: boolean;
  sessionTimeout: string;
  lockedFields: string[];
}

export interface ApiUpdateUserSettingsPolicyPayload {
  lockedFields: string[];
}

export interface ApiCreatePayoutPayload {
  amount: number;
  currency: string;
  description?: string;
  stripeAccountId?: string;
  externalReference?: string;
}

export interface ApiPayout {
  id: string;
  userId: string;
  amount: number;
  currency: string;
  status: string;
  stripePayoutId: string;
  stripeAccountId?: string | null;
  reserveStatus: string;
  reserveId: string;
  failureCode?: string | null;
  failureMessage?: string | null;
  externalReference?: string | null;
  createdAt: string;
  updatedAt: string;
}

export const currencyLabels: Record<number, string> = {
  0: "USD",
  1: "EUR",
  2: "GBP",
  3: "JPY",
  4: "CNY",
  6: "AUD",
  7: "CAD",
  8: "CHF",
  9: "INR",
} as const;

const transactionTypeLabels = ["Income", "Expense", "Transfer"] as const;

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? "").replace(
  /\/$/,
  "",
);

function getStoredToken(): string | null {
  return localStorage.getItem("fintech_token") || localStorage.getItem("token");
}

function getStoredRefreshToken(): string | null {
  return localStorage.getItem("fintech_refresh_token");
}

function storeTokens(token: string, refreshToken?: string) {
  localStorage.setItem("fintech_token", token);
  if (refreshToken) {
    localStorage.setItem("fintech_refresh_token", refreshToken);
  }
}

function clearTokens() {
  localStorage.removeItem("fintech_token");
  localStorage.removeItem("fintech_refresh_token");
  localStorage.removeItem("token");
}

function buildUrl(path: string): string {
  if (API_BASE_URL) {
    return `${API_BASE_URL}${path}`;
  }

  return path;
}

let isRefreshing = false;
let refreshPromise: Promise<boolean> | null = null;

async function tryRefreshToken(): Promise<boolean> {
  const rt = getStoredRefreshToken();
  if (!rt) return false;

  try {
    const response = await fetch(buildUrl("/api/auth/refresh"), {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      credentials: "include",
      body: JSON.stringify({ refreshToken: rt }),
    });

    if (!response.ok) return false;

    const data = (await response.json()) as AuthResponse & {
      refreshToken?: string;
    };
    if (data.token) {
      storeTokens(data.token, data.refreshToken);
      return true;
    }
    return false;
  } catch {
    return false;
  }
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

  // Auto-refresh on 401 (skip for auth endpoints to avoid loops)
  if (response.status === 401 && !path.startsWith("/api/auth/")) {
    if (!isRefreshing) {
      isRefreshing = true;
      refreshPromise = tryRefreshToken().finally(() => {
        isRefreshing = false;
        refreshPromise = null;
      });
    }

    const refreshed = await (refreshPromise ?? Promise.resolve(false));
    if (refreshed) {
      // Retry with new token
      const retryHeaders = new Headers(init?.headers);
      if (!retryHeaders.has("Content-Type") && init?.body) {
        retryHeaders.set("Content-Type", "application/json");
      }
      const newToken = getStoredToken();
      if (newToken) {
        retryHeaders.set("Authorization", `Bearer ${newToken}`);
      }

      const retryResponse = await fetch(buildUrl(path), {
        ...init,
        credentials: "include",
        headers: retryHeaders,
      });

      if (retryResponse.ok) {
        if (retryResponse.status === 204) return undefined as T;
        return (await retryResponse.json()) as T;
      }
    }

    // Refresh failed — clear tokens
    clearTokens();
  }

  if (!response.ok) {
    let errorMessage = `HTTP ${response.status}`;

    try {
      const data = (await response.json()) as ApiError | ApiIdentityError[];

      if (Array.isArray(data)) {
        errorMessage =
          data
            .map((item) => item.description || item.code)
            .filter(Boolean)
            .join("; ") || errorMessage;
      } else {
        errorMessage =
          data.message || data.title || data.description || errorMessage;
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
  const entry = Object.entries(currencyLabels).find(
    ([, label]) => label === value,
  );
  return entry ? Number(entry[0]) : 1; // default EUR
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
    storeTokens(response.token, response.refreshToken);
  }

  return response;
}

export function register(payload: RegisterPayload) {
  return apiRequest("/api/auth/register", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function requestPasswordReset(email: string) {
  return apiRequest<ApiAuthOperationResult>("/api/auth/forgot-password", {
    method: "POST",
    body: JSON.stringify({ email }),
  });
}

export function resetPassword(oobCode: string, newPassword: string) {
  return apiRequest<ApiAuthOperationResult>("/api/auth/reset-password", {
    method: "POST",
    body: JSON.stringify({ oobCode, newPassword }),
  });
}

export function sendVerificationEmail() {
  return apiRequest<ApiAuthOperationResult>(
    "/api/auth/send-verification-email",
    {
      method: "POST",
    },
  );
}

export function verifyEmail(oobCode: string) {
  return apiRequest<ApiAuthOperationResult>("/api/auth/verify-email", {
    method: "POST",
    body: JSON.stringify({ oobCode }),
  });
}

export function getMyProfile() {
  return apiRequest<ApiUserProfile>("/api/users/me/profile");
}

export function updateMyProfile(payload: ApiUpdateUserProfilePayload) {
  return apiRequest<ApiUserProfile>("/api/users/me/profile", {
    method: "PATCH",
    body: JSON.stringify(payload),
  });
}

export function getMySettings() {
  return apiRequest<ApiUserSettings>("/api/users/me/settings");
}

export function updateMySettings(payload: ApiUserSettings) {
  return apiRequest<ApiUserSettings>("/api/users/me/settings", {
    method: "PATCH",
    body: JSON.stringify(payload),
  });
}

export function updateUserSettingsPolicy(
  uid: string,
  payload: ApiUpdateUserSettingsPolicyPayload,
) {
  return apiRequest<ApiUserSettings>(`/api/users/${uid}/settings-policy`, {
    method: "PATCH",
    body: JSON.stringify(payload),
  });
}

export function logout() {
  clearTokens();
}

export function getAccounts() {
  return apiRequest<ApiAccount[]>("/api/accounts");
}

export interface ApiCreateAccountPayload {
  name: string;
  accountType: number;
  currency: number;
}

export function createAccount(payload: ApiCreateAccountPayload) {
  return apiRequest<ApiAccount>("/api/accounts", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function getTransactions() {
  return apiRequest<ApiTransaction[]>("/api/transactions");
}

export function createTransaction(payload: {
  amount: number;
  currency: number;
  type: number;
  status?: number;
  category: string;
  description: string;
  transactionDate: string;
  accountId: string;
}) {
  return apiRequest<ApiTransaction>("/api/transactions", {
    method: "POST",
    body: JSON.stringify(payload),
  });
}

export function deleteTransaction(transactionId: string) {
  return apiRequest<void>(`/api/transactions/${transactionId}`, {
    method: "DELETE",
  });
}

export function updateTransactionStatus(transactionId: string, status: number) {
  return apiRequest<ApiTransaction>(
    `/api/transactions/${transactionId}/status`,
    {
      method: "PATCH",
      body: JSON.stringify({ status }),
    },
  );
}

export function createIdempotencyKey(): string {
  if (
    typeof crypto !== "undefined" &&
    typeof crypto.randomUUID === "function"
  ) {
    return crypto.randomUUID();
  }

  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

export function createPaymentIntent(
  payload: ApiCreatePaymentIntentPayload,
  idempotencyKey: string,
) {
  return apiRequest<ApiPaymentIntentResponse>("/api/payments/intents", {
    method: "POST",
    headers: {
      "Idempotency-Key": idempotencyKey,
    },
    body: JSON.stringify(payload),
  });
}

export function getPaymentById(paymentId: string) {
  return apiRequest<ApiPayment>(`/api/payments/${paymentId}`);
}

export function reconcilePayment(paymentId: string) {
  return apiRequest<ApiPayment>(`/api/payments/${paymentId}/reconcile`, {
    method: "POST",
  });
}

export function createPayout(
  payload: ApiCreatePayoutPayload,
  idempotencyKey: string,
) {
  return apiRequest<ApiPayout>("/api/payouts", {
    method: "POST",
    headers: {
      "Idempotency-Key": idempotencyKey,
    },
    body: JSON.stringify(payload),
  });
}

export function getPayouts() {
  return apiRequest<ApiPayout[]>("/api/payouts");
}

export function getPayoutById(payoutId: string) {
  return apiRequest<ApiPayout>(`/api/payouts/${payoutId}`);
}

export function reconcilePayout(payoutId: string) {
  return apiRequest<ApiPayout>(`/api/payouts/${payoutId}/reconcile`, {
    method: "POST",
  });
}

export function getPlatformBalance(currency = "usd") {
  const queryCurrency = encodeURIComponent(currency);
  return apiRequest<ApiPlatformBalance>(
    `/api/platform/balance?currency=${queryCurrency}`,
  );
}

export function getPlatformSummary(currency = "usd") {
  const queryCurrency = encodeURIComponent(currency);
  return apiRequest<ApiPlatformSummary>(
    `/api/platform/summary?currency=${queryCurrency}`,
  );
}

export async function measureApiLatency(): Promise<number> {
  const start = performance.now();
  await apiRequest<{ status: string }>("/api/test/status");
  return performance.now() - start;
}

// ── Fraud types ─────────────────────────────────────────────

export interface ApiFraudEvaluation {
  id: string;
  userId: string;
  paymentId?: string | null;
  transactionId?: string | null;
  fraudScore: number;
  riskLevel: string;
  decision: string;
  reasons: string[];
  rulesTriggered: string[];
  rulesVersion: string;
  amountMinorUnits: number;
  currency: string;
  mlAnomalyScore?: number | null;
  mlModelVersion?: string | null;
  createdAt: string;
}

export interface ApiFraudCase {
  id: string;
  evaluationId: string;
  userId: string;
  paymentId?: string | null;
  status: string;
  riskLevel: string;
  fraudScore: number;
  amountMinorUnits: number;
  currency: string;
  assignee?: string | null;
  reasons: string[];
  rulesTriggered: string[];
  mlAnomalyScore?: number | null;
  mlModelVersion?: string | null;
  analystNotes?: string | null;
  resolvedBy?: string | null;
  resolvedAt?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ApiFraudCasePage {
  items: ApiFraudCase[];
  totalCount: number;
}

// ── Fraud API methods ───────────────────────────────────────

export function getFraudCases(params?: {
  status?: string;
  limit?: number;
  startAfter?: string;
}) {
  const query = new URLSearchParams();
  if (params?.status) query.set("status", params.status);
  if (params?.limit) query.set("limit", String(params.limit));
  if (params?.startAfter) query.set("startAfter", params.startAfter);

  const qs = query.toString();
  return apiRequest<ApiFraudCasePage>(`/api/fraud-cases${qs ? `?${qs}` : ""}`);
}

export function getFraudCaseById(caseId: string) {
  return apiRequest<ApiFraudCase>(
    `/api/fraud-cases/${encodeURIComponent(caseId)}`,
  );
}

export function getFraudCaseEvaluation(caseId: string) {
  return apiRequest<ApiFraudEvaluation>(
    `/api/fraud-cases/${encodeURIComponent(caseId)}/evaluation`,
  );
}

export function approveFraudCase(caseId: string, analystNotes?: string) {
  return apiRequest<ApiFraudCase>(
    `/api/fraud-cases/${encodeURIComponent(caseId)}/approve`,
    {
      method: "POST",
      body: JSON.stringify({ analystNotes: analystNotes ?? null }),
    },
  );
}

export function rejectFraudCase(caseId: string, analystNotes?: string) {
  return apiRequest<ApiFraudCase>(
    `/api/fraud-cases/${encodeURIComponent(caseId)}/reject`,
    {
      method: "POST",
      body: JSON.stringify({ analystNotes: analystNotes ?? null }),
    },
  );
}

export function escalateFraudCase(caseId: string, analystNotes?: string) {
  return apiRequest<ApiFraudCase>(
    `/api/fraud-cases/${encodeURIComponent(caseId)}/escalate`,
    {
      method: "POST",
      body: JSON.stringify({ analystNotes: analystNotes ?? null }),
    },
  );
}

export function assignFraudCase(caseId: string, assignee: string) {
  return apiRequest<ApiFraudCase>(
    `/api/fraud-cases/${encodeURIComponent(caseId)}/assign`,
    {
      method: "POST",
      body: JSON.stringify({ assignee }),
    },
  );
}

// ── Notifications ───────────────────────────────────────────

export interface ApiNotification {
  id: string;
  type: string;
  title: string;
  message: string;
  entityType?: string | null;
  entityId?: string | null;
  isRead: boolean;
  createdAt: string;
}

export function getNotifications(limit = 50) {
  return apiRequest<ApiNotification[]>(`/api/notifications?limit=${limit}`);
}

export function getUnreadNotificationCount() {
  return apiRequest<{ count: number }>("/api/notifications/unread-count");
}

export function markNotificationAsRead(notificationId: string) {
  return apiRequest<void>(
    `/api/notifications/${encodeURIComponent(notificationId)}/read`,
    { method: "PATCH" },
  );
}

export function markAllNotificationsAsRead() {
  return apiRequest<void>("/api/notifications/read-all", {
    method: "POST",
  });
}

// ── User management (admin only) ────────────────────────────

export interface ApiUserListItem {
  uid: string;
  email: string;
  displayName: string;
  disabled: boolean;
}

export interface ApiUserRole {
  uid: string;
  role: string | null;
}

export function getUsers(maxResults = 100) {
  return apiRequest<ApiUserListItem[]>(
    `/api/users?maxResults=${maxResults}`,
  );
}

export function getUserRole(uid: string) {
  return apiRequest<ApiUserRole>(`/api/roles/${encodeURIComponent(uid)}`);
}

export function setUserRole(uid: string, role: string) {
  return apiRequest<ApiUserRole>(`/api/roles/${encodeURIComponent(uid)}`, {
    method: "POST",
    body: JSON.stringify({ role }),
  });
}

export function removeUserRole(uid: string) {
  return apiRequest<void>(`/api/roles/${encodeURIComponent(uid)}`, {
    method: "DELETE",
  });
}
