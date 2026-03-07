import { createBrowserRouter } from "react-router";
import { Layout } from "./components/Layout";
import { AuthLayout } from "./components/AuthLayout";
import { Dashboard } from "./components/Dashboard";
import { Transactions } from "./components/Transactions";
import { CreatePayment } from "./components/CreatePayment";
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
    Component: Layout,
    children: [
      { index: true, Component: Dashboard },
      { path: "transactions", Component: Transactions },
      { path: "create-payment", Component: CreatePayment },
      { path: "profile", Component: Profile },
      { path: "settings", Component: Settings },
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
