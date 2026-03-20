import { createBrowserRouter } from "react-router";
import { Layout } from "./components/Layout";
import { AuthLayout } from "./components/AuthLayout";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { Dashboard } from "./components/Dashboard";
import { Transactions } from "./components/Transactions";
import { CreatePayment } from "./components/CreatePayment";
import { PaymentDetails } from "./components/PaymentDetails";
import { AccountsProfiles } from "./components/AccountsProfiles";
import { Profile } from "./components/Profile";
import { Settings } from "./components/Settings";
import { Login } from "./components/Login";
import { Register } from "./components/Register";
import { ForgotPassword } from "./components/ForgotPassword";
import { VerifyEmail } from "./components/VerifyEmail";
import { ResetPassword } from "./components/ResetPassword";

export const router = createBrowserRouter([
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
          { path: "payments/:paymentId", Component: PaymentDetails },
          { path: "accounts", Component: AccountsProfiles },
          { path: "profile", Component: Profile },
          { path: "settings", Component: Settings },
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
    ],
  },
]);
