import { Outlet } from "react-router";
import { AuthProvider } from "../auth/AuthContext";

export function RootLayout() {
  return (
    <AuthProvider>
      <Outlet />
    </AuthProvider>
  );
}
