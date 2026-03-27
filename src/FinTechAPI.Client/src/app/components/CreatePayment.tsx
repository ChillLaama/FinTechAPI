import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router";
import { Elements, PaymentElement, useElements, useStripe } from "@stripe/react-stripe-js";
import { loadStripe } from "@stripe/stripe-js";
import {
  DollarSign,
  Send,
  Shield,
  CheckCircle,
  AlertTriangle,
  XCircle,
  Loader2,
  ArrowLeft,
  CreditCard,
} from "lucide-react";
import {
  createTransaction,
  createIdempotencyKey,
  createPaymentIntent,
  updateTransactionStatus,
  currencyLabels,
  getAccounts,
  toCurrencyValue,
  transactionTypeValues,
  reconcilePayment,
} from "../api/client";
import type { ApiAccount, ApiTransaction } from "../api/client";

type PaymentStep = "form" | "processing" | "checkout" | "result";

interface PaymentResult {
  success: boolean;
  transaction?: ApiTransaction;
  paymentId?: string;
  stripePaymentIntentId?: string;
  idempotencyKey?: string;
  providerStatus?: string;
  fraudDecision?: string | null;
  fraudScore?: number | null;
  message: string;
}

interface CheckoutState {
  clientSecret: string;
  paymentId: string;
  stripePaymentIntentId: string;
  transactionId: string;
  idempotencyKey: string;
  amount: number;
  currency: string;
  fraudDecision?: string | null;
  fraudScore?: number | null;
}

interface CheckoutFormProps {
  onConfirmed: (status: string) => Promise<void>;
  onFailed: (message: string) => Promise<void>;
  onPending: (status: string) => Promise<void>;
}

const stripePublishableKey =
  (import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY as string | undefined)?.trim() ??
  "";
const stripePromise = stripePublishableKey ? loadStripe(stripePublishableKey) : null;

function formatMoney(amount: number, currencyCode: string): string {
  return `${currencyCode} ${amount.toLocaleString("en-US", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
}

function CheckoutForm({ onConfirmed, onFailed, onPending }: CheckoutFormProps) {
  const stripe = useStripe();
  const elements = useElements();
  const [submitting, setSubmitting] = useState(false);
  const [localError, setLocalError] = useState<string | null>(null);

  const handleConfirm = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!stripe || !elements) {
      setLocalError("Stripe checkout is not ready. Try again in a moment.");
      return;
    }

    setSubmitting(true);
    setLocalError(null);

    const result = await stripe.confirmPayment({
      elements,
      redirect: "if_required",
    });

    if (result.error) {
      setSubmitting(false);
      await onFailed(result.error.message ?? "Payment confirmation failed.");
      return;
    }

    const status = result.paymentIntent?.status ?? "unknown";
    setSubmitting(false);

    if (status === "succeeded") {
      await onConfirmed(status);
      return;
    }

    if (
      status === "processing" ||
      status === "requires_capture" ||
      status === "requires_action"
    ) {
      await onPending(status);
      return;
    }

    await onFailed(`Payment confirmation returned status: ${status}`);
  };

  return (
    <form onSubmit={handleConfirm} className="space-y-4">
      {localError && (
        <div className="flex items-start gap-3 p-4 bg-destructive/10 border border-destructive/20 rounded-lg">
          <AlertTriangle className="w-5 h-5 text-destructive flex-shrink-0 mt-0.5" />
          <p className="text-xs text-muted-foreground">{localError}</p>
        </div>
      )}

      <div className="p-4 bg-secondary/30 rounded-lg border border-border">
        <PaymentElement
          options={{
            layout: "tabs",
          }}
        />
      </div>

      <button
        type="submit"
        disabled={submitting || !stripe || !elements}
        className="w-full px-6 py-3 bg-accent text-accent-foreground rounded-lg hover:bg-accent/90 transition-colors flex items-center justify-center gap-2 disabled:opacity-60 disabled:cursor-not-allowed"
      >
        <CreditCard className="w-5 h-5" />
        {submitting ? "Confirming..." : "Confirm card payment"}
      </button>
    </form>
  );
}

export function CreatePayment() {
  const navigate = useNavigate();
  const [step, setStep] = useState<PaymentStep>("form");
  const [result, setResult] = useState<PaymentResult | null>(null);
  const [accounts, setAccounts] = useState<ApiAccount[]>([]);
  const [loadingAccounts, setLoadingAccounts] = useState(true);
  const [checkoutState, setCheckoutState] = useState<CheckoutState | null>(null);

  const [formData, setFormData] = useState({
    amount: "",
    currency: "EUR",
    recipient: "",
    description: "",
  });

  const [formError, setFormError] = useState<string | null>(null);
  const currentAttemptKeyRef = useRef<string | null>(null);

  function getAttemptKey(): string {
    if (!currentAttemptKeyRef.current) {
      currentAttemptKeyRef.current = createIdempotencyKey();
    }

    return currentAttemptKeyRef.current;
  }

  useEffect(() => {
    async function loadAccounts() {
      try {
        setLoadingAccounts(true);
        const accountsData = await getAccounts();
        setAccounts(accountsData);
      } catch (requestError) {
        const message =
          requestError instanceof Error
            ? requestError.message
            : "Failed to load accounts";
        setFormError(message);
      } finally {
        setLoadingAccounts(false);
      }
    }

    loadAccounts();
  }, []);

  const selectedAccount = useMemo(() => accounts[0], [accounts]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!selectedAccount) {
      setFormError("No accounts available to create a payment.");
      return;
    }

    const amountValue = Number.parseFloat(formData.amount);
    if (Number.isNaN(amountValue) || amountValue <= 0) {
      setFormError("Amount must be a positive number.");
      return;
    }

    setFormError(null);
    setStep("processing");

    try {
      const idempotencyKey = getAttemptKey();

      const transaction = await createTransaction({
        amount: amountValue,
        currency: toCurrencyValue(formData.currency),
        type: transactionTypeValues.expense,
        status: 0,
        category: "Payment",
        description: `${formData.recipient}: ${formData.description}`,
        transactionDate: new Date().toISOString(),
        accountId: String(selectedAccount.id),
      });

      const paymentIntent = await createPaymentIntent(
        {
          amount: amountValue,
          currency: formData.currency.toLowerCase(),
          description: `${formData.recipient}: ${formData.description}`,
          transactionId: String(transaction.id),
        },
        idempotencyKey,
      );

      setCheckoutState({
        clientSecret: paymentIntent.clientSecret,
        paymentId: paymentIntent.paymentId,
        stripePaymentIntentId: paymentIntent.stripePaymentIntentId,
        transactionId: String(transaction.id),
        idempotencyKey,
        amount: amountValue,
        currency: formData.currency,
        fraudDecision: paymentIntent.fraudDecision,
        fraudScore: paymentIntent.fraudScore,
      });
      setStep("checkout");
    } catch (requestError) {
      const message =
        requestError instanceof Error
          ? requestError.message
          : "Payment intent was not created";
      setResult({
        success: false,
        idempotencyKey: currentAttemptKeyRef.current ?? undefined,
        message,
      });
      setStep("result");
    }
  };

  const handleCheckoutConfirmed = async (providerStatus: string) => {
    if (!checkoutState) {
      return;
    }

    setStep("processing");

    let finalizedTransaction: ApiTransaction | undefined;
    let syncedStatus = providerStatus;

    try {
      const reconciled = await reconcilePayment(checkoutState.paymentId);
      syncedStatus = reconciled.status;

      if (syncedStatus === "succeeded") {
        finalizedTransaction = await updateTransactionStatus(
          checkoutState.transactionId,
          1,
        );
      }
    } catch {
      // Keep provider status from Stripe SDK if reconciliation is temporarily unavailable.
    }

    setResult({
      success: syncedStatus === "succeeded",
      transaction: finalizedTransaction,
      paymentId: checkoutState.paymentId,
      stripePaymentIntentId: checkoutState.stripePaymentIntentId,
      idempotencyKey: checkoutState.idempotencyKey,
      providerStatus: syncedStatus,
      fraudDecision: checkoutState.fraudDecision,
      fraudScore: checkoutState.fraudScore,
      message:
        syncedStatus === "succeeded"
          ? "Card payment completed successfully."
          : `Payment confirmation completed with provider status: ${syncedStatus}.`,
    });

    currentAttemptKeyRef.current = null;
    setStep("result");
  };

  const handleCheckoutPending = async (providerStatus: string) => {
    if (!checkoutState) {
      return;
    }

    try {
      await reconcilePayment(checkoutState.paymentId);
    } catch {
      // The pending state will be retried by webhook reconciliation.
    }

    setResult({
      success: true,
      paymentId: checkoutState.paymentId,
      stripePaymentIntentId: checkoutState.stripePaymentIntentId,
      idempotencyKey: checkoutState.idempotencyKey,
      providerStatus,
      message: `Payment confirmation submitted. Current provider status: ${providerStatus}.`,
    });

    currentAttemptKeyRef.current = null;
    setStep("result");
  };

  const handleCheckoutFailed = async (message: string) => {
    if (!checkoutState) {
      return;
    }

    try {
      await updateTransactionStatus(checkoutState.transactionId, 2);
    } catch {
      // Keep failure context in UI even if status update fails.
    }

    setResult({
      success: false,
      paymentId: checkoutState.paymentId,
      stripePaymentIntentId: checkoutState.stripePaymentIntentId,
      idempotencyKey: checkoutState.idempotencyKey,
      message,
    });

    setStep("result");
  };

  const resetForm = () => {
    const shouldClearAttemptKey = result?.success ?? true;

    setFormData({
      amount: "",
      currency: "EUR",
      recipient: "",
      description: "",
    });
    setCheckoutState(null);
    setStep("form");
    setResult(null);
    setFormError(null);
    if (shouldClearAttemptKey) {
      currentAttemptKeyRef.current = null;
    }
  };

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div className="flex items-center gap-4">
        <button
          onClick={() => navigate("/")}
          className="p-2 rounded-lg hover:bg-secondary transition-colors"
        >
          <ArrowLeft className="w-5 h-5" />
        </button>
        <div>
          <h1 className="text-3xl mb-2">Create payment</h1>
          <p className="text-muted-foreground">
            Create a payment request, confirm card details, and track provider reconciliation
          </p>
        </div>
      </div>

      {step === "form" && (
        <div className="bg-card p-8 rounded-xl border border-border">
          <form onSubmit={handleSubmit} className="space-y-6">
            <div>
              <label className="block mb-2 text-sm">Amount</label>
              <div className="relative">
                <DollarSign className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-muted-foreground" />
                <input
                  type="number"
                  step="0.01"
                  required
                  placeholder="0.00"
                  className="w-full pl-12 pr-4 py-3 bg-input-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring"
                  value={formData.amount}
                  onChange={(event) =>
                    setFormData({ ...formData, amount: event.target.value })
                  }
                />
              </div>
            </div>

            <div>
              <label className="block mb-2 text-sm">Currency</label>
              <select
                className="w-full px-4 py-3 bg-input-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring"
                value={formData.currency}
                onChange={(event) =>
                  setFormData({ ...formData, currency: event.target.value })
                }
              >
                {Object.entries(currencyLabels).map(([value, label]) => (
                  <option key={value} value={label}>
                    {label}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="block mb-2 text-sm">Recipient</label>
              <input
                type="text"
                required
                placeholder="Enter recipient or company name"
                className="w-full px-4 py-3 bg-input-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring"
                value={formData.recipient}
                onChange={(event) =>
                  setFormData({ ...formData, recipient: event.target.value })
                }
              />
            </div>

            <div>
              <label className="block mb-2 text-sm">Payment purpose</label>
              <textarea
                required
                rows={3}
                placeholder="Describe the payment purpose"
                className="w-full px-4 py-3 bg-input-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring resize-none"
                value={formData.description}
                onChange={(event) =>
                  setFormData({ ...formData, description: event.target.value })
                }
              />
            </div>

            <div className="bg-secondary/30 p-4 rounded-lg flex items-start gap-3">
              <Shield className="w-5 h-5 text-accent flex-shrink-0 mt-0.5" />
              <div className="text-sm">
                <p className="mb-1">Operation profile</p>
                <p className="text-muted-foreground text-xs">
                  {loadingAccounts
                    ? "Loading accounts..."
                    : selectedAccount
                      ? `${selectedAccount.name} (ID: ${selectedAccount.id})`
                      : "No accounts available. Create an account before sending a payment."}
                </p>
              </div>
            </div>

            {formError && (
              <div className="flex items-start gap-3 p-4 bg-destructive/10 border border-destructive/20 rounded-lg">
                <AlertTriangle className="w-5 h-5 text-destructive flex-shrink-0 mt-0.5" />
                <p className="text-xs text-muted-foreground">{formError}</p>
              </div>
            )}

            <button
              type="submit"
              disabled={loadingAccounts || !selectedAccount}
              className="w-full px-6 py-3 bg-accent text-accent-foreground rounded-lg hover:bg-accent/90 transition-colors flex items-center justify-center gap-2 disabled:opacity-60 disabled:cursor-not-allowed"
            >
              <Send className="w-5 h-5" />
              Create payment intent
            </button>
          </form>
        </div>
      )}

      {step === "processing" && (
        <div className="bg-card p-12 rounded-xl border border-border">
          <div className="text-center space-y-6">
            <div className="flex justify-center">
              <div className="relative">
                <Loader2 className="w-16 h-16 text-accent animate-spin" />
                <Shield className="w-8 h-8 text-accent absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2" />
              </div>
            </div>

            <div>
              <h2 className="text-2xl mb-2">Processing payment</h2>
              <p className="text-muted-foreground">
                Preparing transaction and synchronizing provider state...
              </p>
            </div>
          </div>
        </div>
      )}

      {step === "checkout" && checkoutState && (
        <div className="bg-card p-8 rounded-xl border border-border space-y-6">
          <div className="space-y-2">
            <h2 className="text-2xl">Card checkout</h2>
            <p className="text-muted-foreground">
              Confirm payment for {formatMoney(checkoutState.amount, checkoutState.currency)}.
            </p>
          </div>

          <div className="bg-secondary/30 p-4 rounded-lg space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="text-muted-foreground">Payment ID</span>
              <code>{checkoutState.paymentId}</code>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Provider reference</span>
              <code>{checkoutState.stripePaymentIntentId}</code>
            </div>
            <div className="flex justify-between">
              <span className="text-muted-foreground">Idempotency key</span>
              <code className="text-xs break-all text-right max-w-[220px]">
                {checkoutState.idempotencyKey}
              </code>
            </div>
          </div>

          {checkoutState.fraudDecision?.toLowerCase() === "review" && (
            <div className="flex items-start gap-3 p-4 bg-yellow-500/10 border border-yellow-500/20 rounded-lg">
              <AlertTriangle className="w-5 h-5 text-yellow-500 flex-shrink-0 mt-0.5" />
              <div className="text-sm">
                <p className="font-medium text-yellow-600">Under review</p>
                <p className="text-muted-foreground text-xs mt-1">
                  This payment has been flagged for manual review (score: {checkoutState.fraudScore ?? "N/A"}).
                  You may proceed, but the transaction may be held for additional verification.
                </p>
              </div>
            </div>
          )}

          {!stripePromise && (
            <div className="flex items-start gap-3 p-4 bg-destructive/10 border border-destructive/20 rounded-lg">
              <AlertTriangle className="w-5 h-5 text-destructive flex-shrink-0 mt-0.5" />
              <p className="text-xs text-muted-foreground">
                Missing VITE_STRIPE_PUBLISHABLE_KEY. Configure it to enable card checkout.
              </p>
            </div>
          )}

          {stripePromise && (
            <Elements
              stripe={stripePromise}
              options={{
                clientSecret: checkoutState.clientSecret,
              }}
            >
              <CheckoutForm
                onConfirmed={handleCheckoutConfirmed}
                onPending={handleCheckoutPending}
                onFailed={handleCheckoutFailed}
              />
            </Elements>
          )}

          <button
            onClick={resetForm}
            className="w-full px-6 py-3 bg-secondary text-secondary-foreground rounded-lg hover:bg-secondary/80 transition-colors"
          >
            Cancel and start over
          </button>
        </div>
      )}

      {step === "result" && result && (
        <div className="bg-card p-8 rounded-xl border border-border">
          <div className="text-center space-y-6">
            <div className="flex justify-center">
              {result.success ? (
                <div className="w-20 h-20 rounded-full bg-success/10 flex items-center justify-center">
                  <CheckCircle className="w-12 h-12 text-success" />
                </div>
              ) : (
                <div className="w-20 h-20 rounded-full bg-destructive/10 flex items-center justify-center">
                  <XCircle className="w-12 h-12 text-destructive" />
                </div>
              )}
            </div>

            <div>
              <h2 className="text-2xl mb-2">
                {result.success ? "Checkout processed" : "Checkout failed"}
              </h2>
              <p className="text-muted-foreground">{result.message}</p>
            </div>

            <div className="max-w-md mx-auto space-y-4">
              <div className="bg-secondary/30 p-4 rounded-lg space-y-3 text-left">
                {result.transaction && (
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-muted-foreground">Transaction ID</span>
                    <code className="text-sm font-mono">#{result.transaction.id}</code>
                  </div>
                )}
                {result.paymentId && (
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-muted-foreground">Payment ID</span>
                    <code className="text-sm font-mono">{result.paymentId}</code>
                  </div>
                )}
                {result.stripePaymentIntentId && (
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-muted-foreground">Provider reference</span>
                    <code className="text-sm font-mono">{result.stripePaymentIntentId}</code>
                  </div>
                )}
                {result.providerStatus && (
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-muted-foreground">Provider status</span>
                    <span className="text-sm">{result.providerStatus}</span>
                  </div>
                )}
                {result.fraudDecision && (
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-muted-foreground">Fraud check</span>
                    <span className={`text-sm font-medium ${
                      result.fraudDecision.toLowerCase() === "allow" ? "text-green-500" :
                      result.fraudDecision.toLowerCase() === "review" ? "text-yellow-500" :
                      "text-destructive"
                    }`}>
                      {result.fraudDecision} {result.fraudScore != null && `(${result.fraudScore})`}
                    </span>
                  </div>
                )}
                {result.idempotencyKey && (
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-muted-foreground">Idempotency key</span>
                    <code className="text-xs font-mono break-all text-right max-w-[220px]">
                      {result.idempotencyKey}
                    </code>
                  </div>
                )}
              </div>
            </div>

            <div className="flex gap-3 pt-4">
              {result.paymentId && (
                <button
                  onClick={() => navigate(`/payments/${result.paymentId}`)}
                  className="flex-1 px-6 py-3 bg-primary text-primary-foreground rounded-lg hover:bg-primary/90 transition-colors"
                >
                  View payment details
                </button>
              )}
              <button
                onClick={() => navigate("/transactions")}
                className="flex-1 px-6 py-3 bg-secondary text-secondary-foreground rounded-lg hover:bg-secondary/80 transition-colors"
              >
                View transactions
              </button>
              <button
                onClick={resetForm}
                className="flex-1 px-6 py-3 bg-secondary text-secondary-foreground rounded-lg hover:bg-secondary/80 transition-colors"
              >
                Create another payment
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
