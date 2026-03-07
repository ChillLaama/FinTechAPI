import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router";
import {
  DollarSign,
  Send,
  Shield,
  CheckCircle,
  AlertTriangle,
  XCircle,
  Loader2,
  ArrowLeft,
} from "lucide-react";
import {
  createTransaction,
  getAccounts,
  getCurrencyLabel,
  toCurrencyValue,
  transactionTypeValues,
} from "../api/client";
import type { ApiAccount, ApiTransaction } from "../api/client";

type PaymentStep = "form" | "processing" | "result";

interface PaymentResult {
  success: boolean;
  transaction?: ApiTransaction;
  message: string;
}

function formatMoney(amount: number, currencyCode: string): string {
  return `${currencyCode} ${amount.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

export function CreatePayment() {
  const navigate = useNavigate();
  const [step, setStep] = useState<PaymentStep>("form");
  const [result, setResult] = useState<PaymentResult | null>(null);
  const [accounts, setAccounts] = useState<ApiAccount[]>([]);
  const [loadingAccounts, setLoadingAccounts] = useState(true);

  const [formData, setFormData] = useState({
    amount: "",
    currency: "RUB",
    recipient: "",
    description: "",
  });

  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    async function loadAccounts() {
      try {
        setLoadingAccounts(true);
        const accountsData = await getAccounts();
        setAccounts(accountsData);
      } catch (requestError) {
        const message = requestError instanceof Error ? requestError.message : "Failed to load accounts";
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

    setFormError(null);
    setStep("processing");

    try {
      const amountValue = Number.parseFloat(formData.amount);

      const transaction = await createTransaction({
        amount: amountValue,
        currency: toCurrencyValue(formData.currency),
        type: transactionTypeValues.expense,
        description: `${formData.recipient}: ${formData.description}`,
        transactionDate: new Date().toISOString(),
        accountId: selectedAccount.id,
      });

      setResult({
        success: true,
        transaction,
        message: "Payment was successfully created via FinTech API.",
      });
    } catch (requestError) {
      const message = requestError instanceof Error ? requestError.message : "Payment was not created";
      setResult({
        success: false,
        message,
      });
    } finally {
      setStep("result");
    }
  };

  const resetForm = () => {
    setFormData({
      amount: "",
      currency: "RUB",
      recipient: "",
      description: "",
    });
    setStep("form");
    setResult(null);
    setFormError(null);
  };

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div className="flex items-center gap-4">
        <button onClick={() => navigate("/")} className="p-2 rounded-lg hover:bg-secondary transition-colors">
          <ArrowLeft className="w-5 h-5" />
        </button>
        <div>
          <h1 className="text-3xl mb-2">Create payment</h1>
          <p className="text-muted-foreground">Create an expense transaction via FinTech API</p>
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
                  onChange={(event) => setFormData({ ...formData, amount: event.target.value })}
                />
              </div>
            </div>

            <div>
              <label className="block mb-2 text-sm">Currency</label>
              <select
                className="w-full px-4 py-3 bg-input-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring"
                value={formData.currency}
                onChange={(event) => setFormData({ ...formData, currency: event.target.value })}
              >
                <option value="RUB">RUB - Russian Ruble</option>
                <option value="USD">USD - US Dollar</option>
                <option value="EUR">EUR - Euro</option>
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
                onChange={(event) => setFormData({ ...formData, recipient: event.target.value })}
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
                onChange={(event) => setFormData({ ...formData, description: event.target.value })}
              />
            </div>

            <div className="bg-secondary/30 p-4 rounded-lg flex items-start gap-3">
              <Shield className="w-5 h-5 text-accent flex-shrink-0 mt-0.5" />
              <div className="text-sm">
                <p className="mb-1">Debit account</p>
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
              Send payment
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
              <p className="text-muted-foreground">Sending transaction to API...</p>
            </div>
          </div>
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
              <h2 className="text-2xl mb-2">{result.success ? "Payment sent successfully" : "Payment failed"}</h2>
              <p className="text-muted-foreground">{result.message}</p>
            </div>

            {result.transaction && (
              <div className="max-w-md mx-auto space-y-4">
                <div className="bg-secondary/30 p-4 rounded-lg space-y-3 text-left">
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-muted-foreground">Transaction ID</span>
                    <code className="text-sm font-mono">#{result.transaction.id}</code>
                  </div>
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-muted-foreground">Amount</span>
                    <span className="text-sm">{formatMoney(result.transaction.amount, getCurrencyLabel(result.transaction.currency))}</span>
                  </div>
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-muted-foreground">Account</span>
                    <span className="text-sm">{selectedAccount?.name || result.transaction.accountId}</span>
                  </div>
                </div>
              </div>
            )}

            <div className="flex gap-3 pt-4">
              {result.success && (
                <button
                  onClick={() => navigate("/transactions")}
                  className="flex-1 px-6 py-3 bg-primary text-primary-foreground rounded-lg hover:bg-primary/90 transition-colors"
                >
                  View transactions
                </button>
              )}
              <button
                onClick={resetForm}
                className="flex-1 px-6 py-3 bg-secondary text-secondary-foreground rounded-lg hover:bg-secondary/80 transition-colors"
              >
                {result.success ? "Create another payment" : "Try again"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
