import { createBrowserRouter } from "react-router";
import { RootLayout } from "./components/RootLayout";
import { Layout } from "./components/Layout";
import { AuthLayout } from "./components/AuthLayout";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { Dashboard } from "./components/Dashboard";
import { Transactions } from "./components/Transactions";
import { CreatePayment } from "./components/CreatePayment";
import { Payouts } from "./components/Payouts";
import { PaymentDetails } from "./components/PaymentDetails";
import { AccountsProfiles } from "./components/AccountsProfiles";
import { Profile } from "./components/Profile";
import { Settings } from "./components/Settings";
import { Login } from "./components/Login";
import { Register } from "./components/Register";
import { ForgotPassword } from "./components/ForgotPassword";
import { VerifyEmail } from "./components/VerifyEmail";
import { ResetPassword } from "./components/ResetPassword";
import { TermsOfService } from "./components/TermsOfService";
import { PrivacyPolicy } from "./components/PrivacyPolicy";
import { Help } from "./components/Help";
import { FraudCases } from "./components/FraudCases";
import { FraudCaseDetails } from "./components/FraudCaseDetails";
import { FraudDashboard } from "./components/FraudDashboard";
import { UserRoleManager } from "./components/UserRoleManager";
import { AdminPanel } from "./components/AdminPanel";
import { AuditLog } from "./components/AuditLog";
import { ReconciliationCenter } from "./components/ReconciliationCenter";
import { SystemAlerts } from "./components/SystemAlerts";

export const router = createBrowserRouter([
  {
    Component: RootLayout,
    children: [
      {
        path: "/",
        Component: ProtectedRoute,
        children: [
          {
            Component: Layout,
            children: [
              { index: true, Component: Dashboard },
              { path: "transactions", Component: Transactions },
              { path: "create-payment", Component: CreatePayment },
              { path: "payouts", Component: Payouts },
              { path: "payments/:paymentId", Component: PaymentDetails },
              { path: "accounts", Component: AccountsProfiles },
              { path: "profile", Component: Profile },
              { path: "settings", Component: Settings },
              { path: "fraud-cases", Component: FraudCases },
              { path: "fraud-cases/:caseId", Component: FraudCaseDetails },
              { path: "fraud-dashboard", Component: FraudDashboard },
              { path: "user-management", Component: UserRoleManager },
              // ── Admin & Ops ──────────────────────────────────────────
              { path: "admin", Component: AdminPanel },
              { path: "admin/audit-log", Component: AuditLog },
              { path: "admin/reconciliation", Component: ReconciliationCenter },
              { path: "admin/alerts", Component: SystemAlerts },
            ],
          },
        ],
      },
      {
        path: "/",
        Component: AuthLayout,
        children: [
          { path: "login", Component: Login },
          { path: "register", Component: Register },
          { path: "forgot-password", Component: ForgotPassword },
          { path: "verify-email", Component: VerifyEmail },
          { path: "reset-password", Component: ResetPassword },
          { path: "terms", Component: TermsOfService },
          { path: "privacy", Component: PrivacyPolicy },
          { path: "help", Component: Help },
        ],
      },
    ],
  },
]);
