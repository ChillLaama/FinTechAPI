import { useCallback, useEffect, useMemo, useState } from "react";
import { type ReactNode } from "react";
import { Link, useParams } from "react-router";
import {
  ArrowLeft,
  CalendarClock,
  CreditCard,
  FileText,
  Shield,
} from "lucide-react";
import { getPaymentById, reconcilePayment } from "../api/client";
import type { ApiPayment } from "../api/client";
import { useAuth } from "../auth/AuthContext";

function formatMoney(amount: number, currency: string): string {
  return `${currency.toUpperCase()} ${amount.toLocaleString("en-US", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
}

function formatDateTime(dateIso: string): string {
  const date = new Date(dateIso);
  if (Number.isNaN(date.getTime())) {
    return "-";
  }

  return date.toLocaleString("en-US");
}

function getProviderHealth(status: string): string {
  const s = status.toLowerCase();
  if (s === "succeeded") return "Healthy";
  if (s === "processing" || s === "requires_action") return "In progress";
  return "Attention";
}

function InfoRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="p-3 bg-secondary/30 rounded-lg flex justify-between text-sm">
      <span className="text-muted-foreground">{label}</span>
      {children}
    </div>
  );
}

function InfoCell({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="p-3 bg-secondary/30 rounded-lg">
      <p className="text-xs text-muted-foreground mb-1">{label}</p>
      {children}
    </div>
  );
}

export function PaymentDetails() {
  const { paymentId = "" } = useParams();
  const { user } = useAuth();
  const isAdmin = useMemo(
    () => user?.role.toLowerCase() === "admin",
    [user?.role],
  );
  const [payment, setPayment] = useState<ApiPayment | null>(null);
  const [loading, setLoading] = useState(true);
  const [reconciling, setReconciling] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [reconcileError, setReconcileError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function loadPayment() {
      try {
        setLoading(true);
        setError(null);

        const paymentData = await getPaymentById(paymentId);
        if (!cancelled) {
          setPayment(paymentData);
        }
      } catch (requestError) {
        if (!cancelled) {
          const message =
            requestError instanceof Error
              ? requestError.message
              : "Failed to load payment details";
          setError(message);
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    loadPayment();

    return () => {
      cancelled = true;
    };
  }, [paymentId]);

  const handleManualReconcile = useCallback(async () => {
    if (!payment) return;

    try {
      setReconciling(true);
      setReconcileError(null);
      const updated = await reconcilePayment(payment.id);
      if (updated == null) {
        setReconcileError(
          "Reconciliation returned no data. The payment may no longer be accessible.",
        );
        return;
      }
      setPayment(updated);
    } catch (requestError) {
      const message =
        requestError instanceof Error
          ? requestError.message
          : "Failed to run manual reconciliation";
      setReconcileError(message);
    } finally {
      setReconciling(false);
    }
  }, [payment]);

  if (loading) {
    return (
      <div className="text-muted-foreground">Loading payment details...</div>
    );
  }

  if (error) {
    return (
      <div className="space-y-3">
        <h1 className="text-3xl">Payment details</h1>
        <div className="p-4 rounded-lg border border-destructive/30 bg-destructive/10 text-destructive text-sm">
          {error}
        </div>
      </div>
    );
  }

  if (!payment) {
    return <div className="text-muted-foreground">Payment not found.</div>;
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl mb-2">Payment details</h1>
          {isAdmin && (
            <p className="text-muted-foreground">
              Provider lifecycle and reconciliation data
            </p>
          )}
        </div>
        <div className="flex items-center gap-2">
          {isAdmin && (
            <button
              onClick={handleManualReconcile}
              disabled={reconciling}
              aria-busy={reconciling}
              aria-label="Run manual reconciliation for this payment"
              className="px-4 py-2 rounded-lg bg-primary text-primary-foreground hover:bg-primary/90 transition-colors disabled:opacity-60"
            >
              {reconciling ? "Reconciling..." : "Run reconciliation"}
            </button>
          )}
          <Link
            to="/transactions"
            className="px-4 py-2 rounded-lg border border-border hover:bg-secondary transition-colors inline-flex items-center gap-2"
          >
            <ArrowLeft className="w-4 h-4" />
            Back to transactions
          </Link>
        </div>
      </div>

      {reconcileError && (
        <div className="p-4 rounded-lg border border-destructive/30 bg-destructive/10 text-destructive text-sm">
          {reconcileError}
        </div>
      )}

      <div className="bg-card border border-border rounded-xl p-6 space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-xs text-muted-foreground">Payment ID</p>
            <code className="text-sm">{payment.id}</code>
          </div>
          <div className="text-right">
            <p className="text-xs text-muted-foreground">Amount</p>
            <p className="text-xl">
              {formatMoney(payment.amount, payment.currency)}
            </p>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <InfoCell label="Status">
            <p>{payment.status}</p>
          </InfoCell>
          {isAdmin && (
            <InfoCell label="Reconciliation state">
              <p>
                {payment.lastWebhookEvent
                  ? "Webhook received"
                  : "Awaiting webhook"}
              </p>
            </InfoCell>
          )}
          {isAdmin && (
            <InfoCell label="Provider reference">
              <code className="text-xs break-all">
                {payment.stripePaymentIntentId}
              </code>
            </InfoCell>
          )}
          <InfoCell label="Linked transaction">
            <p className="text-sm">{payment.transactionId ?? "-"}</p>
          </InfoCell>
        </div>
      </div>

      <div className="bg-card border border-border rounded-xl p-6 space-y-4">
        <div className="flex items-center gap-2">
          <CalendarClock className="w-5 h-5 text-accent" />
          <h2>Timeline</h2>
        </div>

        <div className="space-y-3">
          <InfoRow label="Created at">
            <span>{formatDateTime(payment.createdAt)}</span>
          </InfoRow>
          <InfoRow label="Last updated">
            <span>{formatDateTime(payment.updatedAt)}</span>
          </InfoRow>
          {isAdmin && (
            <>
              <InfoRow label="Webhook event">
                <span>{payment.lastWebhookEvent || "Not received"}</span>
              </InfoRow>
              <InfoRow label="Correlation ID">
                <code className="text-xs break-all">
                  {payment.lastStripeEventId || "-"}
                </code>
              </InfoRow>
            </>
          )}
        </div>
      </div>

      {isAdmin && (
        <>
          <div className="bg-card border border-border rounded-xl p-6 space-y-4">
            <div className="flex items-center gap-2">
              <Shield className="w-5 h-5 text-accent" />
              <h2>Operational summary</h2>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <InfoCell label="Provider health">
                <p>{getProviderHealth(payment.status)}</p>
              </InfoCell>
              <InfoCell label="Data source">
                <p>GET /api/payments/{paymentId}</p>
              </InfoCell>
            </div>
          </div>

          <div className="bg-card border border-border rounded-xl p-6 space-y-4">
            <div className="flex items-center gap-2">
              <CreditCard className="w-5 h-5 text-accent" />
              <h2>Error and retry context</h2>
            </div>
            <p className="text-sm text-muted-foreground">
              If provider status is not terminal, monitor webhook updates and
              verify correlation ID linkage.
            </p>
            <div className="flex items-center gap-2 text-xs text-muted-foreground">
              <FileText className="w-4 h-4" />
              Keep payment ID and correlation ID for support investigations.
            </div>
          </div>
        </>
      )}
    </div>
  );
}
