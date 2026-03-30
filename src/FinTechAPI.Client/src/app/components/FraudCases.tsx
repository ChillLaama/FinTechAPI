import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router";
import {
  Shield,
  AlertTriangle,
  Search,
  ChevronRight,
  Loader2,
  RefreshCw,
} from "lucide-react";
import { getFraudCases } from "../api/client";
import type { ApiFraudCase, ApiFraudCasePage } from "../api/client";

const statusOptions = [
  { value: "", label: "All statuses" },
  { value: "Open", label: "Open" },
  { value: "InReview", label: "In review" },
  { value: "Approved", label: "Approved" },
  { value: "Rejected", label: "Rejected" },
  { value: "Expired", label: "Expired" },
];

function statusColor(status: string): string {
  switch (status.toLowerCase()) {
    case "open":
      return "text-yellow-500 bg-yellow-500/10";
    case "inreview":
      return "text-blue-500 bg-blue-500/10";
    case "approved":
      return "text-green-500 bg-green-500/10";
    case "rejected":
      return "text-red-500 bg-red-500/10";
    case "expired":
      return "text-muted-foreground bg-secondary";
    default:
      return "text-muted-foreground bg-secondary";
  }
}

function riskColor(level: string): string {
  switch (level.toLowerCase()) {
    case "critical":
      return "text-red-600";
    case "high":
      return "text-red-500";
    case "medium":
      return "text-yellow-500";
    case "low":
      return "text-green-500";
    default:
      return "text-muted-foreground";
  }
}

function formatAmount(minorUnits: number, currency: string): string {
  const major = minorUnits / 100;
  return `${currency.toUpperCase()} ${major.toLocaleString("en-US", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
}

export function FraudCases() {
  const navigate = useNavigate();
  const [page, setPage] = useState<ApiFraudCasePage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState("");

  const loadCases = useCallback(
    async (startAfter?: string) => {
      try {
        setLoading(true);
        setError(null);
        const result = await getFraudCases({
          status: statusFilter || undefined,
          limit: 20,
          startAfter,
        });
        setPage(result);
      } catch (err) {
        setError(
          err instanceof Error ? err.message : "Failed to load fraud cases",
        );
      } finally {
        setLoading(false);
      }
    },
    [statusFilter],
  );

  useEffect(() => {
    loadCases();
  }, [loadCases]);

  const handleLoadMore = () => {
    if (page?.items.length) {
      const lastItem = page.items[page.items.length - 1];
      loadCases(lastItem.id);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Shield className="w-8 h-8 text-accent" />
          <div>
            <h1 className="text-3xl mb-1">Fraud cases</h1>
            <p className="text-muted-foreground">
              Review and manage flagged transactions
              {page && ` · ${page.totalCount} total`}
            </p>
          </div>
        </div>
        <button
          onClick={() => loadCases()}
          disabled={loading}
          className="p-2 rounded-lg hover:bg-secondary transition-colors"
          title="Refresh"
        >
          <RefreshCw className={`w-5 h-5 ${loading ? "animate-spin" : ""}`} />
        </button>
      </div>

      {/* Filters */}
      <div className="flex items-center gap-4">
        <div className="relative flex-1 max-w-xs">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
          <select
            className="w-full pl-10 pr-4 py-2 bg-input-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring text-sm"
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
          >
            {statusOptions.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {error && (
        <div className="flex items-start gap-3 p-4 bg-destructive/10 border border-destructive/20 rounded-lg">
          <AlertTriangle className="w-5 h-5 text-destructive flex-shrink-0 mt-0.5" />
          <p className="text-sm text-muted-foreground">{error}</p>
        </div>
      )}

      {loading && !page && (
        <div className="flex justify-center py-12">
          <Loader2 className="w-8 h-8 animate-spin text-accent" />
        </div>
      )}

      {page && page.items.length === 0 && (
        <div className="text-center py-12 text-muted-foreground">
          <Shield className="w-12 h-12 mx-auto mb-3 opacity-40" />
          <p>No fraud cases found</p>
        </div>
      )}

      {page && page.items.length > 0 && (
        <div className="space-y-2">
          {page.items.map((c: ApiFraudCase) => (
            <button
              key={c.id}
              onClick={() => navigate(`/fraud-cases/${c.id}`)}
              className="w-full bg-card p-4 rounded-lg border border-border hover:border-accent/40 transition-colors text-left flex items-center gap-4"
            >
              <div className="flex-1 min-w-0 space-y-1">
                <div className="flex items-center gap-2">
                  <span
                    className={`text-xs px-2 py-0.5 rounded-full font-medium ${statusColor(c.status)}`}
                  >
                    {c.status}
                  </span>
                  <span
                    className={`text-xs font-medium ${riskColor(c.riskLevel)}`}
                  >
                    {c.riskLevel}
                  </span>
                  <span className="text-xs text-muted-foreground">
                    Score: {c.fraudScore}
                  </span>
                </div>
                <div className="flex items-center gap-3 text-sm">
                  <span className="font-mono text-xs truncate max-w-[200px]">
                    {c.id}
                  </span>
                  <span className="text-muted-foreground">·</span>
                  <span>{formatAmount(c.amountMinorUnits, c.currency)}</span>
                  {c.assignee && (
                    <>
                      <span className="text-muted-foreground">·</span>
                      <span className="text-xs text-muted-foreground truncate max-w-[150px]">
                        {c.assignee}
                      </span>
                    </>
                  )}
                </div>
                {c.rulesTriggered.length > 0 && (
                  <div className="flex gap-1 flex-wrap">
                    {c.rulesTriggered.map((rule) => (
                      <span
                        key={rule}
                        className="text-[10px] px-1.5 py-0.5 bg-secondary rounded text-muted-foreground"
                      >
                        {rule}
                      </span>
                    ))}
                  </div>
                )}
              </div>

              <div className="text-right flex-shrink-0 flex items-center gap-2">
                <span className="text-xs text-muted-foreground">
                  {new Date(c.createdAt).toLocaleDateString()}
                </span>
                <ChevronRight className="w-4 h-4 text-muted-foreground" />
              </div>
            </button>
          ))}

          {page.items.length < page.totalCount && (
            <button
              onClick={handleLoadMore}
              disabled={loading}
              className="w-full py-3 text-sm text-accent hover:underline"
            >
              {loading ? "Loading..." : "Load more"}
            </button>
          )}
        </div>
      )}
    </div>
  );
}
