import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router";
import {
  Shield,
  AlertTriangle,
  CheckCircle,
  XCircle,
  BarChart3,
  Loader2,
  RefreshCw,
  Brain,
} from "lucide-react";
import { getFraudCases } from "../api/client";
import type { ApiFraudCase } from "../api/client";
import { useAuth } from "../auth/AuthContext";

interface FraudStats {
  total: number;
  open: number;
  inReview: number;
  approved: number;
  rejected: number;
  avgScore: number;
  criticalCount: number;
  highCount: number;
  mlFlaggedCount: number;
  avgMlScore: number;
  mlModelVersion: string | null;
}

function computeStats(cases: ApiFraudCase[]): FraudStats {
  const stats: FraudStats = {
    total: cases.length,
    open: 0,
    inReview: 0,
    approved: 0,
    rejected: 0,
    avgScore: 0,
    criticalCount: 0,
    highCount: 0,
    mlFlaggedCount: 0,
    avgMlScore: 0,
    mlModelVersion: null,
  };

  let scoreSum = 0;
  let mlScoreSum = 0;
  let mlScoredCount = 0;
  for (const c of cases) {
    scoreSum += c.fraudScore;
    switch (c.status.toLowerCase()) {
      case "open":
        stats.open++;
        break;
      case "inreview":
        stats.inReview++;
        break;
      case "approved":
        stats.approved++;
        break;
      case "rejected":
        stats.rejected++;
        break;
    }
    if (c.riskLevel.toLowerCase() === "critical") stats.criticalCount++;
    if (c.riskLevel.toLowerCase() === "high") stats.highCount++;
    if (c.mlAnomalyScore != null) {
      mlScoredCount++;
      mlScoreSum += c.mlAnomalyScore;
      if (c.mlAnomalyScore >= 0.6) stats.mlFlaggedCount++;
      if (c.mlModelVersion && !stats.mlModelVersion) {
        stats.mlModelVersion = c.mlModelVersion;
      }
    }
  }

  stats.avgScore = cases.length > 0 ? Math.round(scoreSum / cases.length) : 0;
  stats.avgMlScore =
    mlScoredCount > 0
      ? Math.round((mlScoreSum / mlScoredCount) * 100) / 100
      : 0;
  return stats;
}

function StatCard({
  label,
  value,
  icon: Icon,
  color,
}: {
  label: string;
  value: number | string;
  icon: React.FC<{ className?: string }>;
  color: string;
}) {
  return (
    <div className="bg-card p-5 rounded-xl border border-border">
      <div className="flex items-center gap-3 mb-2">
        <Icon className={`w-5 h-5 ${color}`} />
        <span className="text-sm text-muted-foreground">{label}</span>
      </div>
      <p className="text-2xl font-semibold">{value}</p>
    </div>
  );
}

export function FraudDashboard() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [allCases, setAllCases] = useState<ApiFraudCase[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await getFraudCases({ limit: 100 });
      setAllCases(result.items);
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to load fraud data",
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  if (user?.role.toLowerCase() !== "admin") {
    return (
      <div className="p-4 rounded-lg border border-destructive/30 bg-destructive/10 text-destructive text-sm">
        Access denied. Admin role required.
      </div>
    );
  }

  const stats = computeStats(allCases);

  // Recent critical/high cases
  const recentHighRisk = allCases
    .filter(
      (c) =>
        c.riskLevel.toLowerCase() === "critical" ||
        c.riskLevel.toLowerCase() === "high",
    )
    .slice(0, 5);

  // False positive rate
  const resolvedCases = allCases.filter(
    (c) =>
      c.status.toLowerCase() === "approved" ||
      c.status.toLowerCase() === "rejected",
  );
  const falsePositiveRate =
    resolvedCases.length > 0
      ? Math.round(
          (resolvedCases.filter((c) => c.status.toLowerCase() === "approved")
            .length /
            resolvedCases.length) *
            100,
        )
      : 0;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <BarChart3 className="w-8 h-8 text-accent" />
          <div>
            <h1 className="text-3xl mb-1">Fraud monitoring</h1>
            <p className="text-muted-foreground">
              KPIs, trends, and operational alerts
            </p>
          </div>
        </div>
        <button
          onClick={load}
          disabled={loading}
          className="p-2 rounded-lg hover:bg-secondary transition-colors"
          title="Refresh"
        >
          <RefreshCw className={`w-5 h-5 ${loading ? "animate-spin" : ""}`} />
        </button>
      </div>

      {error && (
        <div className="flex items-start gap-3 p-4 bg-destructive/10 border border-destructive/20 rounded-lg">
          <AlertTriangle className="w-5 h-5 text-destructive flex-shrink-0 mt-0.5" />
          <p className="text-sm">{error}</p>
        </div>
      )}

      {loading && allCases.length === 0 ? (
        <div className="flex justify-center py-12">
          <Loader2 className="w-8 h-8 animate-spin text-accent" />
        </div>
      ) : (
        <>
          {/* KPI grid */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <StatCard
              label="Total cases"
              value={stats.total}
              icon={Shield}
              color="text-accent"
            />
            <StatCard
              label="Open"
              value={stats.open}
              icon={AlertTriangle}
              color="text-yellow-500"
            />
            <StatCard
              label="In review"
              value={stats.inReview}
              icon={Shield}
              color="text-blue-500"
            />
            <StatCard
              label="Rejected (blocked)"
              value={stats.rejected}
              icon={XCircle}
              color="text-red-500"
            />
          </div>

          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <StatCard
              label="Approved"
              value={stats.approved}
              icon={CheckCircle}
              color="text-green-500"
            />
            <StatCard
              label="Avg. score"
              value={stats.avgScore}
              icon={BarChart3}
              color="text-accent"
            />
            <StatCard
              label="Critical risk"
              value={stats.criticalCount}
              icon={AlertTriangle}
              color="text-red-600"
            />
            <StatCard
              label="False positive rate"
              value={`${falsePositiveRate}%`}
              icon={CheckCircle}
              color="text-muted-foreground"
            />
          </div>

          {/* ML model stats */}
          <div className="bg-card p-6 rounded-xl border border-border space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-medium flex items-center gap-2">
                <Brain className="w-5 h-5 text-purple-500" />
                ML model insights
              </h2>
              {stats.mlModelVersion ? (
                <span className="text-xs px-2 py-1 bg-purple-500/10 text-purple-500 rounded-full">
                  {stats.mlModelVersion}
                </span>
              ) : (
                <span className="text-xs px-2 py-1 bg-secondary text-muted-foreground rounded-full">
                  Model not loaded
                </span>
              )}
            </div>

            {/* Runtime stats */}
            <div className="grid grid-cols-3 gap-4">
              <div>
                <span className="text-sm text-muted-foreground">
                  Avg. ML score
                </span>
                <p className="text-2xl font-semibold">
                  {stats.avgMlScore > 0 ? stats.avgMlScore.toFixed(2) : "—"}
                </p>
              </div>
              <div>
                <span className="text-sm text-muted-foreground">
                  ML flagged (≥0.6)
                </span>
                <p className="text-2xl font-semibold text-orange-500">
                  {stats.mlFlaggedCount}
                </p>
              </div>
              <div>
                <span className="text-sm text-muted-foreground">
                  ML coverage
                </span>
                <p className="text-2xl font-semibold">
                  {stats.total > 0
                    ? `${Math.round(
                        ((stats.total -
                          allCases.filter((c) => c.mlAnomalyScore == null)
                            .length) /
                          stats.total) *
                          100,
                      )}%`
                    : "—"}
                </p>
              </div>
            </div>

            {/* Training evaluation metrics */}
            <div className="border-t border-border pt-4">
              <p className="text-xs text-muted-foreground mb-3">
                Training evaluation — FastTree Binary Classification ·{" "}
                <span className="font-medium">284,807</span> samples (Kaggle
                Credit Card Fraud Dataset) · Generated 2026-03-31
              </p>
              <div className="grid grid-cols-3 md:grid-cols-6 gap-3">
                {(
                  [
                    { label: "Accuracy", value: "99.97%" },
                    { label: "AUC-ROC", value: "0.9976" },
                    { label: "AUC-PR", value: "0.8513" },
                    { label: "F1 Score", value: "0.8844" },
                    { label: "Precision", value: "94.68%" },
                    { label: "Recall", value: "82.98%" },
                  ] as const
                ).map(({ label, value }) => (
                  <div
                    key={label}
                    className="bg-secondary/40 rounded-lg p-3 text-center"
                  >
                    <p className="text-xs text-muted-foreground">{label}</p>
                    <p className="text-sm font-semibold text-purple-400 mt-0.5">
                      {value}
                    </p>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* High risk cases */}
          {recentHighRisk.length > 0 && (
            <div className="bg-card p-6 rounded-xl border border-border space-y-3">
              <h2 className="text-lg font-medium flex items-center gap-2">
                <AlertTriangle className="w-5 h-5 text-red-500" />
                High risk cases
              </h2>
              <div className="space-y-2">
                {recentHighRisk.map((c) => (
                  <button
                    key={c.id}
                    onClick={() => navigate(`/fraud-cases/${c.id}`)}
                    className="w-full flex items-center justify-between p-3 rounded-lg bg-secondary/30 hover:bg-secondary/50 transition-colors text-left text-sm"
                  >
                    <div className="flex items-center gap-3">
                      <span
                        className={`text-xs font-medium ${
                          c.riskLevel.toLowerCase() === "critical"
                            ? "text-red-600"
                            : "text-red-500"
                        }`}
                      >
                        {c.riskLevel}
                      </span>
                      <span className="font-mono text-xs">
                        {c.id.slice(0, 12)}...
                      </span>
                      <span>Score: {c.fraudScore}</span>
                      {c.mlAnomalyScore != null && (
                        <span
                          className={`text-xs px-1.5 py-0.5 rounded ${
                            c.mlAnomalyScore >= 0.8
                              ? "bg-red-500/10 text-red-500"
                              : c.mlAnomalyScore >= 0.6
                                ? "bg-orange-500/10 text-orange-500"
                                : "bg-purple-500/10 text-purple-500"
                          }`}
                        >
                          ML: {c.mlAnomalyScore.toFixed(2)}
                        </span>
                      )}
                    </div>
                    <span className="text-xs text-muted-foreground">
                      {c.status}
                    </span>
                  </button>
                ))}
              </div>
              <button
                onClick={() => navigate("/fraud-cases")}
                className="text-sm text-accent hover:underline"
              >
                View all cases →
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
