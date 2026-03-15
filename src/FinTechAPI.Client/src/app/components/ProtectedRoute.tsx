import { Navigate, Outlet } from "react-router";

/** Decodes the `exp` claim from a JWT without verifying the signature. */
function getJwtExpiry(token: string): number | null {
  try {
    const parts = token.split(".");
    if (parts.length < 2) return null;
    // Base64url → Base64 → JSON
    const payload = JSON.parse(
      atob(parts[1].replace(/-/g, "+").replace(/_/g, "/"))
    );
    return typeof payload.exp === "number" ? payload.exp : null;
  } catch {
    return null;
  }
}

function isAuthenticated(): boolean {
  const token =
    localStorage.getItem("fintech_token") || localStorage.getItem("token");
  if (!token) return false;

  const exp = getJwtExpiry(token);
  // If we can't decode the token, let the API decide (it will 401 if invalid).
  if (exp === null) return true;

  // exp is Unix seconds; give a 30-second grace window for clock skew.
  return exp * 1000 > Date.now() - 30_000;
}

export function ProtectedRoute() {
  if (!isAuthenticated()) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}
