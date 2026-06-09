import { useEffect, useState, useCallback } from "react";
import { RefreshCw, AlertTriangle, CheckCircle, Clock, CreditCard, Wrench } from "lucide-react";
import {
  getReconciliationSummary,
  getPendingPayments,
  type ApiReconciliationSummary,
  type ApiPendingPayment,
} from "../api/client";

function StaleBadge({ minutes }: { minutes: number }) {
  if (minutes > 60) {
    return (
      <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[11px] font-medium bg-destructive/10 text-destructive">
        <AlertTriangle className="w-3 h-3" />
        {minutes}m stale
      </span>
    );
  }
  if (minutes > 15) {
    return (
      <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[11px] font-medium bg-yellow-500/10 text-yellow-600">
        <Clock className="w-3 h-3" />
        {minutes}m stale
      </span>
    );
  }
  return (
    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[11px] font-medium bg-secondary text-muted-foreground">
      <Clock className="w-3 h-3" />
      {minutes}m
    </span>
  );
}

function statusBadge(status: string) {
  const s = status.toLowerCase().replace(/_/g, " ");
  const cls =
    status === "processing"
      ? "bg-accent/10 text-accent"
      : "bg-yellow-500/10 text-yellow-600";
  return (
    <span className={`px-2 py-0.5 rounded-full text-[11px] font-medium ${cls}`}>
      {s}
    </span>
  );
}

export function ReconciliationCenter() {
  const [summary, setSummary] = useState<ApiReconciliationSummary | null>(null);
  const [payments, setPayments] = useState<ApiPendingPayment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [staleMinutes, setStaleMinutes] = useState(5);
  const [reconcilingId, setReconcilingId] = useState<string | null>(null);
  const [reconcileResults, setReconcileResults] = useState<
    Record<string, "ok" | "error">
  >({});

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [s, p] = await Promise.all([
        getReconciliationSummary(staleMinutes),
        getPendingPayments(staleMinutes, 100),
      ]);
      setSummary(s);
      setPayments(p);
    } catch {
      setError("Failed to load reconciliation data.");
    } finally {
      setLoading(false);
    }
  }, [staleMinutes]);

  useEffect(() => {
    load();
  }, [load]);

  const handleReconcile = async (paymentId: string) => {
    setReconcilingId(paymentId);
    try {
      const token = localStorage.getItem("fintech_token") || localStorage.getItem("token") || "";
      const res = await fetch(
        `/api/payments/${encodeURIComponent(paymentId)}/reconcile`,
        {
          method: "POST",
          headers: { Authorization: `Bearer ${token}` },
        },
      );
      setReconcileResults((prev) => ({
        ...prev,
        [paymentId]: res.ok ? "ok" : "error",
      }));
      if (res.ok) {
        setPayments((prev) => prev.filter((p) => p.id !== paymentId));
        setSummary((prev) =>
          prev
            ? {
                ...prev,
                pendingPaymentsCount: Math.max(0, prev.pendingPaymentsCount - 1),
                stuckPaymentsCount: Math.max(0, prev.stuckPaymentsCount - 1),
              }
            : prev,
        );
      }
    } catch {
      setReconcileResults((prev) => ({ ...prev, [paymentId]: "error" }));
    } finally {
      setReconcilingId(null);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <RefreshCw className="w-6 h-6 text-accent" />
          <div>
            <h1 className="text-2xl font-semibold text-foreground">
              Reconciliation Center
            </h1>
            <p className="text-sm text-muted-foreground">
              View and fix payments stuck in non-terminal states.
            </p>
          </div>
        </div>
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2">
            <label className="text-xs text-muted-foreground">Stale after</label>
            <select
              value={staleMinutes}
              onChange={(e) => setStaleMinutes(Number(e.target.value))}
              className="text-sm px-2 py-1.5 bg-background border border-border rounded-lg focus:outline-none focus:border-accent"
            >
              {[2, 5, 10, 30, 60].map((m) => (
                <option key={m} value={m}>
                  {m} min
                </option>
              ))}
            </select>
          </div>
          <button
            onClick={load}
            disabled={loading}
            className="flex items-center gap-2 px-4 py-2 text-sm border border-border rounded-lg hover:bg-secondary transition-colors disabled:opacity-50"
          >
            <RefreshCw className={`w-4 h-4 ${loading ? "animate-spin" : ""}`} />
            Refresh
          </button>
        </div>
      </div>

      {error && (
        <div className="bg-destructive/10 border border-destructive/30 text-destructive text-sm rounded-lg px-4 py-3">
          {error}
        </div>
      )}

      {/* Summary cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-card border border-border rounded-lg p-5">
          <div className="flex items-center gap-3 mb-2">
            <CreditCard className="w-5 h-5 text-accent" />
            <p className="text-xs text-muted-foreground uppercase tracking-wide">
              Pending Payments
            </p>
          </div>
          <p className="text-3xl font-semibold text-accent">
            {loading ? "…" : summary?.pendingPaymentsCount ?? 0}
          </p>
        </div>

        <div className="bg-card border border-border rounded-lg p-5">
          <div className="flex items-center gap-3 mb-2">
            <AlertTriangle className="w-5 h-5 text-destructive" />
            <p className="text-xs text-muted-foreground uppercase tracking-wide">
              Stuck {">"} 30 min
            </p>
          </div>
          <p className="text-3xl font-semibold text-destructive">
            {loading ? "…" : summary?.stuckPaymentsCount ?? 0}
          </p>
        </div>

        <div className="bg-card border border-border rounded-lg p-5">
          <div className="flex items-center gap-3 mb-2">
            <CheckCircle className="w-5 h-5 text-success" />
            <p className="text-xs text-muted-foreground uppercase tracking-wide">
              Background service
            </p>
          </div>
          <p className="text-sm font-medium text-success mt-1">Running</p>
          <p className="text-xs text-muted-foreground">Every 5 min</p>
        </div>
      </div>

      {/* Info banner */}
      <div className="bg-accent/5 border border-accent/20 rounded-lg px-4 py-3 text-sm text-muted-foreground">
        <strong className="text-foreground">How it works:</strong> The background
        reconciliation service runs every 5 minutes and auto-syncs stuck
        payments from Stripe. Use the manual reconcile buttons below for
        immediate resolution.
      </div>

      {/* Pending payments table */}
      <div className="bg-card border border-border rounded-lg overflow-hidden">
        <div className="px-4 py-3 border-b border-border flex items-center justify-between">
          <h2 className="text-sm font-medium text-foreground">
            Pending / Stuck Payments
          </h2>
          {payments.length > 0 && (
            <span className="text-xs text-muted-foreground">
              {payments.length} payment{payments.length !== 1 ? "s" : ""}
            </span>
          )}
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border bg-secondary/30">
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">
                  Payment ID
                </th>
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">
                  Amount
                </th>
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">
                  Status
                </th>
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">
                  Stale
                </th>
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">
                  Last Event
                </th>
                <th className="text-left px-4 py-3 text-xs font-medium text-muted-foreground uppercase tracking-wide">
                  Action
                </th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                Array.from({ length: 4 }).map((_, i) => (
                  <tr key={i} className="border-b border-border/50 animate-pulse">
                    {Array.from({ length: 6 }).map((_, j) => (
                      <td key={j} className="px-4 py-3">
                        <div className="h-4 bg-secondary rounded w-20" />
                      </td>
                    ))}
                  </tr>
                ))
              ) : payments.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-12 text-center">
                    <CheckCircle className="w-8 h-8 mx-auto mb-2 text-success opacity-60" />
                    <p className="text-muted-foreground">
                      No stuck payments detected.
                    </p>
                  </td>
                </tr>
              ) : (
                payments.map((p) => {
                  const result = reconcileResults[p.id];
                  return (
                    <tr
                      key={p.id}
                      className="border-b border-border/50 hover:bg-secondary/20 transition-colors"
                    >
                      <td className="px-4 py-3">
                        <span className="font-mono text-xs text-muted-foreground truncate block max-w-[120px]">
                          {p.id}
                        </span>
                        <span className="font-mono text-[10px] text-muted-foreground/50 truncate block max-w-[120px]">
                          {p.stripePaymentIntentId}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-foreground font-medium">
                        {p.amount.toFixed(2)}{" "}
                        <span className="text-muted-foreground font-normal uppercase text-xs">
                          {p.currency}
                        </span>
                      </td>
                      <td className="px-4 py-3">{statusBadge(p.status)}</td>
                      <td className="px-4 py-3">
                        <StaleBadge minutes={p.staleMinutes} />
                      </td>
                      <td className="px-4 py-3 text-xs text-muted-foreground">
                        {p.lastWebhookEvent ?? "—"}
                      </td>
                      <td className="px-4 py-3">
                        {result === "ok" ? (
                          <span className="inline-flex items-center gap-1 text-xs text-success">
                            <CheckCircle className="w-3.5 h-3.5" />
                            Reconciled
                          </span>
                        ) : result === "error" ? (
                          <span className="inline-flex items-center gap-1 text-xs text-destructive">
                            <AlertTriangle className="w-3.5 h-3.5" />
                            Failed
                          </span>
                        ) : (
                          <button
                            onClick={() => handleReconcile(p.id)}
                            disabled={reconcilingId === p.id}
                            className="flex items-center gap-1.5 px-3 py-1.5 text-xs bg-accent/10 text-accent rounded-lg hover:bg-accent/20 transition-colors disabled:opacity-50"
                          >
                            {reconcilingId === p.id ? (
                              <RefreshCw className="w-3 h-3 animate-spin" />
                            ) : (
                              <Wrench className="w-3 h-3" />
                            )}
                            Reconcile
                          </button>
                        )}
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      {summary && (
        <p className="text-xs text-muted-foreground text-right">
          Data generated at {new Date(summary.generatedAt).toLocaleString()}
        </p>
      )}
    </div>
  );
}

