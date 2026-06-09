import { useEffect, useState } from "react";
import { Link } from "react-router";
import {
  AlertTriangle,
  ArrowRight,
  Bell,
  CheckCircle,
  CreditCard,
  RefreshCw,
  Shield,
  Users,
  ClipboardList,
  Zap,
} from "lucide-react";
import {
  getAdminOverview,
  seedDemoScenario,
  type ApiAdminOverview,
} from "../api/client";

function StatCard({
  title,
  value,
  icon: Icon,
  variant = "default",
  href,
}: {
  title: string;
  value: number | string;
  icon: React.ElementType;
  variant?: "default" | "warning" | "danger" | "success";
  href?: string;
}) {
  const variantClass = {
    default: "text-accent",
    warning: "text-warning",
    danger: "text-destructive",
    success: "text-success",
  }[variant];

  const bgClass = {
    default: "bg-accent/10",
    warning: "bg-yellow-500/10",
    danger: "bg-destructive/10",
    success: "bg-success/10",
  }[variant];

  const card = (
    <div className="bg-card border border-border rounded-lg p-5 flex items-center gap-4 hover:border-accent/40 transition-colors">
      <div className={`${bgClass} p-3 rounded-lg flex-shrink-0`}>
        <Icon className={`w-6 h-6 ${variantClass}`} />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-xs text-muted-foreground uppercase tracking-wide mb-0.5">
          {title}
        </p>
        <p className={`text-2xl font-semibold ${variantClass}`}>{value}</p>
      </div>
      {href && <ArrowRight className="w-4 h-4 text-muted-foreground flex-shrink-0" />}
    </div>
  );

  return href ? <Link to={href}>{card}</Link> : card;
}

function QuickAction({
  label,
  description,
  icon: Icon,
  href,
  variant = "default",
}: {
  label: string;
  description: string;
  icon: React.ElementType;
  href: string;
  variant?: "default" | "warning" | "danger";
}) {
  const accent = {
    default: "text-accent",
    warning: "text-yellow-500",
    danger: "text-destructive",
  }[variant];

  return (
    <Link
      to={href}
      className="flex items-center gap-4 p-4 bg-card border border-border rounded-lg hover:border-accent/40 transition-colors group"
    >
      <Icon className={`w-5 h-5 ${accent} flex-shrink-0`} />
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium text-foreground">{label}</p>
        <p className="text-xs text-muted-foreground">{description}</p>
      </div>
      <ArrowRight className="w-4 h-4 text-muted-foreground group-hover:text-foreground transition-colors" />
    </Link>
  );
}

export function AdminPanel() {
  const [overview, setOverview] = useState<ApiAdminOverview | null>(null);
  const [loading, setLoading] = useState(true);
  const [seeding, setSeeding] = useState(false);
  const [seedResult, setSeedResult] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fetchOverview = async () => {
    try {
      setLoading(true);
      const data = await getAdminOverview();
      setOverview(data);
      setError(null);
    } catch {
      setError("Failed to load admin overview.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchOverview();
  }, []);

  const handleSeedDemo = async () => {
    setSeeding(true);
    setSeedResult(null);
    try {
      const result = await seedDemoScenario();
      setSeedResult(
        `✓ Demo seeded: ${result.accounts} accounts, ${result.transactions} transactions, ${result.fraudCases} fraud cases, ${result.pendingPayments} pending payments.`,
      );
      await fetchOverview();
    } catch {
      setSeedResult("✗ Demo seeding failed.");
    } finally {
      setSeeding(false);
    }
  };

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-foreground">
            Admin Panel
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            Platform operations, fraud monitoring and audit tools.
          </p>
        </div>
        <button
          onClick={fetchOverview}
          disabled={loading}
          className="flex items-center gap-2 px-4 py-2 text-sm border border-border rounded-lg hover:bg-secondary transition-colors disabled:opacity-50"
        >
          <RefreshCw className={`w-4 h-4 ${loading ? "animate-spin" : ""}`} />
          Refresh
        </button>
      </div>

      {error && (
        <div className="bg-destructive/10 border border-destructive/30 text-destructive text-sm rounded-lg px-4 py-3">
          {error}
        </div>
      )}

      {/* Stats grid */}
      <section>
        <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide mb-3">
          Platform Status
        </h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
          <StatCard
            title="Active Alerts"
            value={loading ? "…" : overview?.activeAlertsCount ?? 0}
            icon={Bell}
            variant={
              (overview?.criticalAlertsCount ?? 0) > 0 ? "danger" : "warning"
            }
            href="/admin/alerts"
          />
          <StatCard
            title="Critical Alerts"
            value={loading ? "…" : overview?.criticalAlertsCount ?? 0}
            icon={AlertTriangle}
            variant={
              (overview?.criticalAlertsCount ?? 0) > 0 ? "danger" : "success"
            }
            href="/admin/alerts"
          />
          <StatCard
            title="Pending Payments"
            value={loading ? "…" : overview?.pendingPaymentsCount ?? 0}
            icon={CreditCard}
            variant={
              (overview?.stuckPaymentsCount ?? 0) > 0 ? "warning" : "default"
            }
            href="/admin/reconciliation"
          />
          <StatCard
            title="Stuck Payments (>30 min)"
            value={loading ? "…" : overview?.stuckPaymentsCount ?? 0}
            icon={RefreshCw}
            variant={
              (overview?.stuckPaymentsCount ?? 0) > 0 ? "danger" : "success"
            }
            href="/admin/reconciliation"
          />
          <StatCard
            title="Open Fraud Cases"
            value={loading ? "…" : overview?.openFraudCasesCount ?? 0}
            icon={Shield}
            variant={
              (overview?.openFraudCasesCount ?? 0) > 0 ? "warning" : "success"
            }
            href="/fraud-cases"
          />
          <StatCard
            title="Last Refreshed"
            value={
              overview
                ? new Date(overview.generatedAt).toLocaleTimeString()
                : "—"
            }
            icon={CheckCircle}
            variant="default"
          />
        </div>
      </section>

      {/* Quick Actions */}
      <section>
        <h2 className="text-sm font-medium text-muted-foreground uppercase tracking-wide mb-3">
          Quick Actions
        </h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <QuickAction
            label="Audit Trail"
            description="Browse all system activity logs"
            icon={ClipboardList}
            href="/admin/audit-log"
          />
          <QuickAction
            label="Reconciliation Center"
            description="View and fix stuck or pending payments"
            icon={RefreshCw}
            href="/admin/reconciliation"
            variant="warning"
          />
          <QuickAction
            label="System Alerts"
            description="Review and dismiss operational alerts"
            icon={Bell}
            href="/admin/alerts"
            variant={
              (overview?.criticalAlertsCount ?? 0) > 0 ? "danger" : "default"
            }
          />
          <QuickAction
            label="Fraud Cases"
            description="Review, approve or reject flagged transactions"
            icon={Shield}
            href="/fraud-cases"
            variant="warning"
          />
          <QuickAction
            label="Fraud Monitoring"
            description="KPI dashboard and risk trends"
            icon={Shield}
            href="/fraud-dashboard"
          />
          <QuickAction
            label="User Management"
            description="Manage user roles and access"
            icon={Users}
            href="/user-management"
          />
        </div>
      </section>

      {/* Demo scenario seeder (dev convenience) */}
      <section className="border border-dashed border-border rounded-lg p-5">
        <div className="flex items-start gap-3">
          <Zap className="w-5 h-5 text-accent flex-shrink-0 mt-0.5" />
          <div className="flex-1">
            <p className="text-sm font-medium text-foreground">
              Demo Scenario Seeder
            </p>
            <p className="text-xs text-muted-foreground mt-0.5">
              Creates demo accounts, transactions, stuck payments and fraud
              cases to showcase the platform. Use in development / staging only.
            </p>
            {seedResult && (
              <p
                className={`text-xs mt-2 ${seedResult.startsWith("✓") ? "text-success" : "text-destructive"}`}
              >
                {seedResult}
              </p>
            )}
          </div>
          <button
            onClick={handleSeedDemo}
            disabled={seeding}
            className="flex items-center gap-2 px-4 py-2 text-sm bg-accent text-accent-foreground rounded-lg hover:bg-accent/80 transition-colors disabled:opacity-50 flex-shrink-0"
          >
            {seeding ? (
              <RefreshCw className="w-3.5 h-3.5 animate-spin" />
            ) : (
              <Zap className="w-3.5 h-3.5" />
            )}
            Seed Demo
          </button>
        </div>
      </section>
    </div>
  );
}

