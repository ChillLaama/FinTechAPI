import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router";
import { ArrowLeft, CalendarClock, CreditCard, FileText, Shield } from "lucide-react";
import { getPaymentById, reconcilePayment } from "../api/client";
import type { ApiPayment } from "../api/client";

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

export function PaymentDetails() {
  const { paymentId = "" } = useParams();
  const [payment, setPayment] = useState<ApiPayment | null>(null);
  const [loading, setLoading] = useState(true);
  const [reconciling, setReconciling] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadPayment() {
      try {
        setLoading(true);
        setError(null);

        if (!paymentId) {
          setError("Payment ID is missing in URL.");
          return;
        }

        const paymentData = await getPaymentById(paymentId);
        setPayment(paymentData);
      } catch (requestError) {
        const message =
          requestError instanceof Error
            ? requestError.message
            : "Failed to load payment details";
        setError(message);
      } finally {
        setLoading(false);
      }
    }

    loadPayment();
  }, [paymentId]);

  const providerHealth = useMemo(() => {
    if (!payment) {
      return "Unknown";
    }

    const status = payment.status.toLowerCase();
    if (status === "succeeded") {
      return "Healthy";
    }

    if (status === "processing" || status === "requires_action") {
      return "In progress";
    }

    return "Attention";
  }, [payment]);

  if (loading) {
    return <div className="text-muted-foreground">Loading payment details...</div>;
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

  async function handleManualReconcile() {
    if (!payment) {
      return;
    }

    try {
      setReconciling(true);
      const updated = await reconcilePayment(payment.id);
      setPayment(updated);
    } catch (requestError) {
      const message =
        requestError instanceof Error
          ? requestError.message
          : "Failed to run manual reconciliation";
      setError(message);
    } finally {
      setReconciling(false);
    }
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl mb-2">Payment details</h1>
          <p className="text-muted-foreground">Provider lifecycle and reconciliation data</p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={handleManualReconcile}
            disabled={reconciling}
            className="px-4 py-2 rounded-lg bg-primary text-primary-foreground hover:bg-primary/90 transition-colors disabled:opacity-60"
          >
            {reconciling ? "Reconciling..." : "Run reconciliation"}
          </button>
          <Link
            to="/transactions"
            className="px-4 py-2 rounded-lg border border-border hover:bg-secondary transition-colors inline-flex items-center gap-2"
          >
            <ArrowLeft className="w-4 h-4" />
            Back to transactions
          </Link>
        </div>
      </div>

      <div className="bg-card border border-border rounded-xl p-6 space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-xs text-muted-foreground">Payment ID</p>
            <code className="text-sm">{payment.id}</code>
          </div>
          <div className="text-right">
            <p className="text-xs text-muted-foreground">Amount</p>
            <p className="text-xl">{formatMoney(payment.amount, payment.currency)}</p>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="p-3 bg-secondary/30 rounded-lg">
            <p className="text-xs text-muted-foreground mb-1">Provider status</p>
            <p>{payment.status}</p>
          </div>
          <div className="p-3 bg-secondary/30 rounded-lg">
            <p className="text-xs text-muted-foreground mb-1">Reconciliation state</p>
            <p>{payment.lastWebhookEvent ? "Webhook received" : "Awaiting webhook"}</p>
          </div>
          <div className="p-3 bg-secondary/30 rounded-lg">
            <p className="text-xs text-muted-foreground mb-1">Provider reference</p>
            <code className="text-xs break-all">{payment.stripePaymentIntentId}</code>
          </div>
          <div className="p-3 bg-secondary/30 rounded-lg">
            <p className="text-xs text-muted-foreground mb-1">Linked transaction</p>
            {payment.transactionId ? (
              <Link to="/transactions" className="text-accent hover:text-accent/80">
                {payment.transactionId}
              </Link>
            ) : (
              <p>-</p>
            )}
          </div>
        </div>
      </div>

      <div className="bg-card border border-border rounded-xl p-6 space-y-4">
        <div className="flex items-center gap-2">
          <CalendarClock className="w-5 h-5 text-accent" />
          <h2>Provider timeline</h2>
        </div>

        <div className="space-y-3">
          <div className="p-3 bg-secondary/30 rounded-lg flex justify-between text-sm">
            <span className="text-muted-foreground">Created at</span>
            <span>{formatDateTime(payment.createdAt)}</span>
          </div>
          <div className="p-3 bg-secondary/30 rounded-lg flex justify-between text-sm">
            <span className="text-muted-foreground">Last provider update</span>
            <span>{formatDateTime(payment.updatedAt)}</span>
          </div>
          <div className="p-3 bg-secondary/30 rounded-lg flex justify-between text-sm">
            <span className="text-muted-foreground">Webhook event</span>
            <span>{payment.lastWebhookEvent || "Not received"}</span>
          </div>
          <div className="p-3 bg-secondary/30 rounded-lg flex justify-between text-sm">
            <span className="text-muted-foreground">Correlation ID</span>
            <code className="text-xs break-all">{payment.lastStripeEventId || "-"}</code>
          </div>
        </div>
      </div>

      <div className="bg-card border border-border rounded-xl p-6 space-y-4">
        <div className="flex items-center gap-2">
          <Shield className="w-5 h-5 text-accent" />
          <h2>Operational summary</h2>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="p-3 bg-secondary/30 rounded-lg">
            <p className="text-xs text-muted-foreground mb-1">Provider health</p>
            <p>{providerHealth}</p>
          </div>
          <div className="p-3 bg-secondary/30 rounded-lg">
            <p className="text-xs text-muted-foreground mb-1">Data source</p>
            <p>GET /api/payments/{paymentId}</p>
          </div>
        </div>
      </div>

      <div className="bg-card border border-border rounded-xl p-6 space-y-4">
        <div className="flex items-center gap-2">
          <CreditCard className="w-5 h-5 text-accent" />
          <h2>Error and retry context</h2>
        </div>
        <p className="text-sm text-muted-foreground">
          If provider status is not terminal, monitor webhook updates and verify correlation ID linkage.
        </p>
        <div className="flex items-center gap-2 text-xs text-muted-foreground">
          <FileText className="w-4 h-4" />
          Keep payment ID and correlation ID for support investigations.
        </div>
      </div>
    </div>
  );
}
