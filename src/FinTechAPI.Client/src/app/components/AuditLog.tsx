import { useEffect, useState, useCallback } from "react";
import { ClipboardList, RefreshCw, Search, ChevronDown, ChevronUp, X } from "lucide-react";
import { getAuditLogs, type ApiAuditLog } from "../api/client";

function timeAgo(dateStr: string): string {
  const seconds = Math.floor((Date.now() - new Date(dateStr).getTime()) / 1000);
  if (seconds < 60) return "just now";
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  return new Date(dateStr).toLocaleDateString();
}

function actionBadgeClass(action: string): string {
  const a = action.toLowerCase();
  if (a.includes("delete") || a.includes("reject") || a.includes("block")) {
    return "bg-destructive/10 text-destructive";
  }
  if (a.includes("create") || a.includes("approve") || a.includes("paid")) {
    return "bg-success/10 text-success";
  }
  if (a.includes("fraud") || a.includes("escalat") || a.includes("reconcil")) {
    return "bg-yellow-500/10 text-yellow-600";
  }
  return "bg-secondary text-muted-foreground";
}

function DetailsExpander({ json }: { json: string | null | undefined }) {
  const [open, setOpen] = useState(false);
  if (!json) return <span className="text-muted-foreground/50">—</span>;

  let parsed: unknown;
  try {
    parsed = JSON.parse(json);
  } catch {
    return <span className="font-mono text-xs text-muted-foreground">{json}</span>;
  }

  return (
    <div>
      <button
        onClick={() => setOpen((v) => !v)}
        className="flex items-center gap-1 text-xs text-accent hover:text-accent/80 transition-colors"
      >
        {open ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />}
        {open ? "Hide" : "Show"} details
      </button>
      {open && (
        <pre className="mt-1 text-[11px] bg-secondary rounded p-2 overflow-auto max-h-32 text-foreground">
          {JSON.stringify(parsed, null, 2)}
        </pre>
      )}
    </div>
  );
}

const ENTITY_TYPES = ["", "Payment", "Transaction", "FraudCase", "Account", "System", "User"];
const COMMON_ACTIONS = [
  "",
  "PaymentIntent.Created",
  "Payment.Reconciled",
  "fraud_case_created",
  "fraud_case_approved",
  "fraud_case_rejected",
  "fraud_case_escalated",
  "Reconciliation.CycleCompleted",
  "SystemAlert.Dismissed",
];

export function AuditLog() {
  const [logs, setLogs] = useState<ApiAuditLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Filters
  const [userId, setUserId] = useState("");
  const [entityType, setEntityType] = useState("");
  const [action, setAction] = useState("");
  const [limit, setLimit] = useState(50);

  const fetch = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getAuditLogs({
        userId: userId || undefined,
        entityType: entityType || undefined,
        action: action || undefined,
        limit,
      });
      setLogs(data);
    } catch {
      setError("Failed to load audit logs.");
    } finally {
      setLoading(false);
    }
  }, [userId, entityType, action, limit]);

  useEffect(() => {
    fetch();
  }, [fetch]);

  const clearFilters = () => {
    setUserId("");
    setEntityType("");
    setAction("");
    setLimit(50);
  };

  const hasFilters = userId || entityType || action;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <ClipboardList className="w-6 h-6 text-accent" />
          <div>
            <h1 className="text-2xl font-semibold text-foreground">Audit Trail</h1>
            <p className="text-sm text-muted-foreground">Full history of system and user actions.</p>
          </div>
        </div>
        <button
          onClick={fetch}
          disabled={loading}
          className="flex items-center gap-2 px-4 py-2 text-sm border border-border rounded-lg hover:bg-secondary transition-colors disabled:opacity-50"
        >
          <RefreshCw className={`w-4 h-4 ${loading ? "animate-spin" : ""}`} />
          Refresh
        </button>
      </div>

      {/* Filters */}
      <div className="bg-card border border-border rounded-lg p-4">
        <div className="flex flex-wrap gap-3 items-end">
          {/* User ID */}
          <div className="flex-1 min-w-[180px]">
            <label className="block text-xs text-muted-foreground mb-1">User ID</label>
            <div className="relative">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-muted-foreground" />
              <input
                type="text"
                value={userId}
                onChange={(e) => setUserId(e.target.value)}
                placeholder="Filter by user..."
                className="w-full pl-8 pr-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:border-accent"
              />
            </div>
          </div>

          {/* Entity type */}
          <div className="flex-1 min-w-[140px]">
            <label className="block text-xs text-muted-foreground mb-1">Entity Type</label>
            <select
              value={entityType}
              onChange={(e) => setEntityType(e.target.value)}
              className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:border-accent"
            >
              {ENTITY_TYPES.map((t) => (
                <option key={t} value={t}>{t || "All types"}</option>
              ))}
            </select>
          </div>

          {/* Action */}
          <div className="flex-1 min-w-[200px]">
            <label className="block text-xs text-muted-foreground mb-1">Action</label>
            <select
              value={action}
              onChange={(e) => setAction(e.target.value)}
              className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:border-accent"
            >
              {COMMON_ACTIONS.map((a) => (
                <option key={a} value={a}>{a || "All actions"}</option>
              ))}
            </select>
          </div>

          {/* Limit */}
          <div className="w-28">
            <label className="block text-xs text-muted-foreground mb-1">Limit</label>
            <select
              value={limit}
              onChange={(e) => setLimit(Number(e.target.value))}
              className="w-full px-3 py-2 text-sm bg-background border border-border rounded-lg focus:outline-none focus:border-accent"
            >
              {[25, 50, 100, 200].map((l) => (
                <option key={l} value={l}>{l} rows</option>
              ))}
            </select>
          </div>

          {hasFilters && (
            <button
              onClick={clearFilters}
              className="flex items-center gap-1.5 px-3 py-2 text-sm text-muted-foreground hover:text-foreground border border-border rounded-lg hover:bg-secondary transition-colors"
            >
              <X className="w-3.5 h-3.5" />
              Clear
            </button>
          )}
        </div>
      </div>

      {error && (
        <div className="bg-destructive/10 border border-destructive/30 text-destructive text-sm rounded-lg px-4 py-3">
          {error}
        </div>
      )}

      {/* Table */}
      <div className="bg-card border border-border rounded-lg overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border bg-secondary/30">
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">
                  Timestamp
                </th>
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">
                  Action
                </th>
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">
                  Entity
                </th>
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">
                  User
                </th>
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">
                  Details
                </th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                Array.from({ length: 6 }).map((_, i) => (
                  <tr key={i} className="border-b border-border/50 animate-pulse">
                    {Array.from({ length: 5 }).map((_, j) => (
                      <td key={j} className="px-4 py-3">
                        <div className="h-4 bg-secondary rounded w-24" />
                      </td>
                    ))}
                  </tr>
                ))
              ) : logs.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-4 py-12 text-center text-muted-foreground">
                    <ClipboardList className="w-8 h-8 mx-auto mb-2 opacity-40" />
                    <p>No audit logs found.</p>
                  </td>
                </tr>
              ) : (
                logs.map((log) => (
                  <tr
                    key={log.id}
                    className="border-b border-border/50 hover:bg-secondary/20 transition-colors"
                  >
                    <td className="px-4 py-3 whitespace-nowrap">
                      <span
                        className="text-muted-foreground text-xs"
                        title={new Date(log.timestamp).toISOString()}
                      >
                        {timeAgo(log.timestamp)}
                      </span>
                      <br />
                      <span className="text-[10px] text-muted-foreground/60">
                        {new Date(log.timestamp).toLocaleString()}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <span
                        className={`inline-block px-2 py-0.5 rounded-full text-[11px] font-medium ${actionBadgeClass(log.action)}`}
                      >
                        {log.action}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <span className="text-foreground">{log.entityType}</span>
                      {log.entityId && (
                        <span className="block text-[10px] text-muted-foreground font-mono truncate max-w-[120px]">
                          {log.entityId}
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      <span className="font-mono text-xs text-muted-foreground truncate block max-w-[100px]">
                        {log.userId === "system" ? (
                          <span className="text-accent">system</span>
                        ) : (
                          log.userId
                        )}
                      </span>
                    </td>
                    <td className="px-4 py-3 max-w-[200px]">
                      <DetailsExpander json={log.details} />
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
        {!loading && logs.length > 0 && (
          <div className="px-4 py-3 border-t border-border text-xs text-muted-foreground">
            Showing {logs.length} entries
          </div>
        )}
      </div>
    </div>
  );
}

