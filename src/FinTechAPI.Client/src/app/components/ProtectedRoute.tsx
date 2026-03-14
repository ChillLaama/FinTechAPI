import { Navigate, Outlet } from "react-router";

function isAuthenticated(): boolean {
  return !!(
    localStorage.getItem("fintech_token") || localStorage.getItem("token")
  );
}

export function ProtectedRoute() {
  if (!isAuthenticated()) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}
