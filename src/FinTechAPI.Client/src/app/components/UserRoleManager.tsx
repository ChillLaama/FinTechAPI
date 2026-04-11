import { useCallback, useEffect, useMemo, useState } from "react";
import { RefreshCw, Users } from "lucide-react";
import {
  getUsers,
  getUserRole,
  setUserRole,
  removeUserRole,
  type ApiUserListItem,
} from "../api/client";
import { useAuth } from "../auth/AuthContext";

type Role = "admin" | "analyst" | "user" | "none";

interface UserRow extends ApiUserListItem {
  role: string | null;
  roleLoading: boolean;
  roleSaving: boolean;
  roleError: string | null;
  pendingRole: Role;
}

const ROLES: Role[] = ["admin", "analyst", "user", "none"];

function roleLabel(role: Role) {
  if (role === "none") return "— no role —";
  return role.charAt(0).toUpperCase() + role.slice(1);
}

function roleBadge(role: string | null) {
  if (!role)
    return (
      <span className="text-xs text-muted-foreground italic">no role</span>
    );
  const colours: Record<string, string> = {
    admin: "bg-destructive/15 text-destructive",
    analyst: "bg-accent/15 text-accent",
    user: "bg-secondary text-secondary-foreground",
  };
  const cls =
    colours[role.toLowerCase()] ?? "bg-secondary text-secondary-foreground";
  return (
    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${cls}`}>
      {role}
    </span>
  );
}

export function UserRoleManager() {
  const { user: currentUser, logout } = useAuth();
  const [rows, setRows] = useState<UserRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const currentUid = currentUser?.id ?? "";

  const fetchUsers = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const users = await getUsers();
      const withRoles: UserRow[] = users.map((u) => ({
        ...u,
        role: null,
        roleLoading: true,
        roleSaving: false,
        roleError: null,
        pendingRole: "user" as Role,
      }));
      setRows(withRoles);

      // Load roles in parallel
      const settled = await Promise.allSettled(
        users.map((u) => getUserRole(u.uid)),
      );
      setRows((prev) =>
        prev.map((row, i) => {
          const result = settled[i];
          const role =
            result.status === "fulfilled" ? (result.value.role ?? null) : null;
          return {
            ...row,
            role,
            pendingRole: (role as Role) ?? "user",
            roleLoading: false,
          };
        }),
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load users");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

  const handleRoleChange = useCallback((uid: string, value: Role) => {
    setRows((prev) =>
      prev.map((r) => (r.uid === uid ? { ...r, pendingRole: value } : r)),
    );
  }, []);

  const handleApply = useCallback(
    async (uid: string) => {
      const row = rows.find((r) => r.uid === uid);
      if (!row) return;

      setRows((prev) =>
        prev.map((r) =>
          r.uid === uid ? { ...r, roleSaving: true, roleError: null } : r,
        ),
      );

      try {
        if (row.pendingRole === "none") {
          await removeUserRole(uid);
          setRows((prev) =>
            prev.map((r) =>
              r.uid === uid ? { ...r, role: null, roleSaving: false } : r,
            ),
          );
        } else {
          const updated = await setUserRole(uid, row.pendingRole);
          setRows((prev) =>
            prev.map((r) =>
              r.uid === uid
                ? { ...r, role: updated.role, roleSaving: false }
                : r,
            ),
          );
        }
      } catch (err) {
        const message =
          err instanceof Error ? err.message : "Failed to update role";
        setRows((prev) =>
          prev.map((r) =>
            r.uid === uid ? { ...r, roleSaving: false, roleError: message } : r,
          ),
        );
      }
    },
    [rows],
  );

  const isSelfRow = useCallback(
    (uid: string) => uid === currentUid,
    [currentUid],
  );

  const sortedRows = useMemo(
    () =>
      [...rows].sort((a, b) =>
        isSelfRow(a.uid) ? -1 : isSelfRow(b.uid) ? 1 : 0,
      ),
    [rows, isSelfRow],
  );

  if (!currentUser || currentUser.role.toLowerCase() !== "admin") {
    return (
      <div className="p-4 rounded-lg border border-destructive/30 bg-destructive/10 text-destructive text-sm">
        Access denied. Admin role required.
      </div>
    );
  }

  if (loading) {
    return <div className="text-muted-foreground">Loading users...</div>;
  }

  if (error) {
    return (
      <div className="space-y-3">
        <h1 className="text-3xl">User role management</h1>
        <div className="p-4 rounded-lg border border-destructive/30 bg-destructive/10 text-destructive text-sm">
          {error}
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-5xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl mb-2">User role management</h1>
          <p className="text-muted-foreground">
            Assign Firebase custom claim roles. Users must re-login after a role
            change.
          </p>
        </div>
        <button
          onClick={fetchUsers}
          className="flex items-center gap-2 px-4 py-2 rounded-lg border border-border hover:bg-secondary transition-colors text-sm"
        >
          <RefreshCw className="w-4 h-4" />
          Refresh
        </button>
      </div>

      <div className="bg-card border border-border rounded-xl overflow-hidden">
        <div className="flex items-center gap-2 p-4 border-b border-border">
          <Users className="w-5 h-5 text-accent" />
          <h2 className="text-base">{rows.length} users</h2>
        </div>

        <div className="divide-y divide-border">
          {sortedRows.map((row) => {
            const isSelf = isSelfRow(row.uid);
            const isDirty =
              row.pendingRole !== (row.role ?? "none") &&
              !(row.pendingRole === "user" && row.role === null);

            return (
              <div
                key={row.uid}
                className={`p-4 flex flex-col sm:flex-row sm:items-center gap-3 ${
                  isSelf ? "bg-accent/5" : ""
                }`}
              >
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap">
                    <p className="text-sm font-medium truncate">
                      {row.displayName || row.email}
                    </p>
                    {isSelf && (
                      <span className="text-xs text-muted-foreground">
                        (you)
                      </span>
                    )}
                    {row.disabled && (
                      <span className="text-xs px-2 py-0.5 rounded-full bg-warning/15 text-warning">
                        disabled
                      </span>
                    )}
                  </div>
                  <p className="text-xs text-muted-foreground truncate">
                    {row.email}
                  </p>
                  <p className="text-xs text-muted-foreground mt-0.5">
                    UID: {row.uid}
                  </p>
                </div>

                <div className="flex items-center gap-3 flex-shrink-0">
                  {row.roleLoading ? (
                    <span className="text-xs text-muted-foreground">
                      Loading role...
                    </span>
                  ) : (
                    <>
                      <div className="flex items-center gap-2">
                        <span className="text-xs text-muted-foreground">
                          Current:
                        </span>
                        {roleBadge(row.role)}
                      </div>

                      <select
                        value={row.pendingRole}
                        onChange={(e) =>
                          handleRoleChange(row.uid, e.target.value as Role)
                        }
                        disabled={row.roleSaving}
                        className="text-sm bg-secondary border border-border rounded-lg px-3 py-1.5 disabled:opacity-60 focus:outline-none focus:ring-1 focus:ring-accent"
                      >
                        {ROLES.map((r) => (
                          <option key={r} value={r}>
                            {roleLabel(r)}
                          </option>
                        ))}
                      </select>

                      <button
                        onClick={() => handleApply(row.uid)}
                        disabled={row.roleSaving || !isDirty}
                        className="px-3 py-1.5 rounded-lg bg-primary text-primary-foreground text-sm hover:bg-primary/90 transition-colors disabled:opacity-60 whitespace-nowrap"
                      >
                        {row.roleSaving ? "Saving..." : "Apply"}
                      </button>
                    </>
                  )}
                </div>

                {row.roleError && (
                  <p className="text-xs text-destructive w-full sm:w-auto">
                    {row.roleError}
                  </p>
                )}

                {isSelf &&
                  row.role !== null &&
                  row.pendingRole !== (row.role as Role) &&
                  !isDirty === false && (
                    <p className="text-xs text-muted-foreground w-full">
                      After applying — sign out and back in to get the updated
                      token.{" "}
                      <button
                        className="underline text-accent hover:text-accent/80"
                        onClick={logout}
                      >
                        Sign out now
                      </button>
                    </p>
                  )}
              </div>
            );
          })}
        </div>
      </div>

      <div className="p-4 rounded-lg border border-border bg-secondary/30 text-sm text-muted-foreground space-y-1">
        <p className="font-medium text-foreground">How roles work</p>
        <p>
          Roles are stored as Firebase custom claims. After assigning a new role
          the user must sign out and back in — the browser token is refreshed
          only on re-authentication.
        </p>
        <p>
          <span className="text-destructive font-medium">admin</span> — full
          access, including this page.{" "}
          <span className="text-accent font-medium">analyst</span> — fraud
          review access.{" "}
          <span className="text-secondary-foreground font-medium">user</span> —
          standard access.
        </p>
      </div>
    </div>
  );
}
