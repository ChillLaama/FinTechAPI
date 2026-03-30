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
} from "lucide-react";
import { getFraudCases } from "../api/client";
import type { ApiFraudCase } from "../api/client";

interface FraudStats {
  total: number;
  open: number;
  inReview: number;
  approved: number;
  rejected: number;
  avgScore: number;
  criticalCount: number;
  highCount: number;
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
  };

  let scoreSum = 0;
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
  }

  stats.avgScore = cases.length > 0 ? Math.round(scoreSum / cases.length) : 0;
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
