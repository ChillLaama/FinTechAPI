import { Outlet, Link, useLocation } from "react-router";
import {
  LayoutDashboard,
  CreditCard,
  PlusCircle,
  Wallet,
  FolderOpen,
  Activity,
  Settings,
  User,
  LogOut,
  Shield,
  BarChart3,
  UsersRound,
  ClipboardList,
  RefreshCw,
  Bell,
  LayoutGrid,
} from "lucide-react";
import { useState } from "react";
import { useAuth } from "../auth/AuthContext";
import { NotificationBell } from "./NotificationBell";

export function Layout() {
  const location = useLocation();
  const { user, logout } = useAuth();
  const [showProfileMenu, setShowProfileMenu] = useState(false);

  const initials = user
    ? `${(user.firstName?.[0] ?? "").toUpperCase()}${(user.lastName?.[0] ?? "").toUpperCase()}` ||
      "?"
    : "?";
  const displayName = user
    ? `${user.firstName} ${user.lastName}`.trim() || user.email
    : "User";
  const displayEmail = user?.email ?? "";

  const isAdmin = user?.role.toLowerCase() === "admin";

  const navItems = [
    { path: "/", label: "Dashboard", icon: LayoutDashboard },
    { path: "/transactions", label: "Transactions", icon: CreditCard },
    { path: "/create-payment", label: "Create payment", icon: PlusCircle },
    { path: "/payouts", label: "Payouts", icon: Wallet },
    { path: "/accounts", label: "Account profiles", icon: FolderOpen },
  ];

  const adminNavItems = [
    { path: "/admin", label: "Admin Panel", icon: LayoutGrid },
    { path: "/fraud-cases", label: "Fraud cases", icon: Shield },
    { path: "/fraud-dashboard", label: "Fraud monitoring", icon: BarChart3 },
    { path: "/admin/reconciliation", label: "Reconciliation", icon: RefreshCw },
    { path: "/admin/audit-log", label: "Audit Trail", icon: ClipboardList },
    { path: "/admin/alerts", label: "System Alerts", icon: Bell },
    { path: "/user-management", label: "User management", icon: UsersRound },
  ];

  return (
    <div className="flex h-screen bg-background">
      {/* Sidebar */}
      <aside className="w-64 border-r border-border bg-card flex flex-col">
        <div className="p-6 border-b border-border">
          <div className="flex items-center gap-2">
            <Activity className="w-8 h-8 text-accent" />
            <h1 className="text-xl text-foreground">FinanceHub</h1>
          </div>
        </div>

        <nav className="flex-1 p-4 overflow-y-auto">
          <ul className="space-y-1">
            {navItems.map((item) => {
              const Icon = item.icon;
              const isActive = location.pathname === item.path;

              return (
                <li key={item.path}>
                  <Link
                    to={item.path}
                    className={`flex items-center gap-3 px-4 py-2.5 rounded-lg transition-colors ${
                      isActive
                        ? "bg-primary text-primary-foreground"
                        : "text-muted-foreground hover:bg-secondary hover:text-foreground"
                    }`}
                  >
                    <Icon className="w-4 h-4 flex-shrink-0" />
                    <span className="text-sm">{item.label}</span>
                  </Link>
                </li>
              );
            })}
          </ul>

          {/* Admin section */}
          {isAdmin && (
            <div className="mt-5 pt-4 border-t border-border">
              <p className="px-4 mb-2 text-[10px] font-semibold text-muted-foreground uppercase tracking-wider">
                Admin & Ops
              </p>
              <ul className="space-y-1">
                {adminNavItems.map((item) => {
                  const Icon = item.icon;
                  const isActive =
                    item.path === "/admin"
                      ? location.pathname === "/admin"
                      : location.pathname.startsWith(item.path);

                  return (
                    <li key={item.path}>
                      <Link
                        to={item.path}
                        className={`flex items-center gap-3 px-4 py-2.5 rounded-lg transition-colors ${
                          isActive
                            ? "bg-primary text-primary-foreground"
                            : "text-muted-foreground hover:bg-secondary hover:text-foreground"
                        }`}
                      >
                        <Icon className="w-4 h-4 flex-shrink-0" />
                        <span className="text-sm">{item.label}</span>
                      </Link>
                    </li>
                  );
                })}
              </ul>
            </div>
          )}
        </nav>

        <div className="p-4 border-t border-border">
          <Link
            to="/settings"
            className={`flex items-center gap-3 px-4 py-3 rounded-lg transition-colors w-full ${
              location.pathname === "/settings"
                ? "bg-primary text-primary-foreground"
                : "text-muted-foreground hover:bg-secondary hover:text-foreground"
            }`}
          >
            <Settings className="w-5 h-5" />
            <span>Settings</span>
          </Link>
        </div>
      </aside>

      {/* Main Content */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Top Bar */}
        <header className="h-16 border-b border-border bg-card px-6 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="w-2 h-2 rounded-full bg-success animate-pulse" />
            <span className="text-sm text-muted-foreground">
              API Status: Online
            </span>
          </div>

          <div className="flex items-center gap-4">
            <NotificationBell />

            <div className="relative pl-4 border-l border-border">
              <button
                onClick={() => setShowProfileMenu(!showProfileMenu)}
                className="flex items-center gap-3 hover:opacity-80 transition-opacity"
              >
                <div className="w-9 h-9 rounded-full bg-primary flex items-center justify-center">
                  <span className="text-sm text-primary-foreground">
                    {initials}
                  </span>
                </div>
              </button>

              {showProfileMenu && (
                <>
                  <div
                    className="fixed inset-0 z-10"
                    onClick={() => setShowProfileMenu(false)}
                  />
                  <div className="absolute right-0 top-full mt-2 w-56 bg-card border border-border rounded-lg shadow-lg z-20 overflow-hidden">
                    <div className="p-3 border-b border-border">
                      <p className="text-sm text-foreground">{displayName}</p>
                      <p className="text-xs text-muted-foreground">
                        {displayEmail}
                      </p>
                    </div>
                    <div className="py-1">
                      <Link
                        to="/profile"
                        className="flex items-center gap-3 px-4 py-2 text-sm text-foreground hover:bg-secondary transition-colors"
                        onClick={() => setShowProfileMenu(false)}
                      >
                        <User className="w-4 h-4" />
                        Profile
                      </Link>
                      <Link
                        to="/settings"
                        className="flex items-center gap-3 px-4 py-2 text-sm text-foreground hover:bg-secondary transition-colors"
                        onClick={() => setShowProfileMenu(false)}
                      >
                        <Settings className="w-4 h-4" />
                        Settings
                      </Link>
                    </div>
                    <div className="border-t border-border py-1">
                      <button
                        className="flex items-center gap-3 px-4 py-2 text-sm text-destructive hover:bg-destructive/10 transition-colors w-full"
                        onClick={() => {
                          setShowProfileMenu(false);
                          logout();
                        }}
                      >
                        <LogOut className="w-4 h-4" />
                        Sign out
                      </button>
                    </div>
                  </div>
                </>
              )}
            </div>
          </div>
        </header>

        {/* Page Content */}
        <main className="flex-1 overflow-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
