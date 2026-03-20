import { useEffect, useMemo, useState } from "react";
import {
  createIdempotencyKey,
  createPayout,
  getPayouts,
  reconcilePayout,
} from "../api/client";
import type { ApiPayout } from "../api/client";

function formatMoney(amount: number, currencyCode = "USD"): string {
  return `${currencyCode} ${amount.toLocaleString("en-US", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}`;
}

function formatDateTime(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleString("en-US", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function statusTone(status: string): string {
  const normalized = status.toLowerCase();

  if (normalized === "paid" || normalized === "succeeded") {
    return "bg-success/10 text-success border-success/20";
  }

  if (normalized === "failed" || normalized === "canceled") {
    return "bg-destructive/10 text-destructive border-destructive/20";
  }

  return "bg-warning/10 text-warning border-warning/20";
}

export function Payouts() {
  const [payouts, setPayouts] = useState<ApiPayout[]>([]);
  const [amount, setAmount] = useState("500");
  const [currency, setCurrency] = useState("usd");
  const [description, setDescription] = useState("Merchant withdrawal");
  const [stripeAccountId, setStripeAccountId] = useState("");
  const [externalReference, setExternalReference] = useState("");
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  async function loadPayouts() {
    try {
      setLoading(true);
      setError(null);
      const data = await getPayouts();
      setPayouts(
        [...data].sort(
          (first, second) =>
            new Date(second.createdAt).getTime() -
            new Date(first.createdAt).getTime(),
        ),
      );
    } catch (requestError) {
      const message =
        requestError instanceof Error
          ? requestError.message
          : "Failed to load payouts";
      setError(message);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadPayouts();
  }, []);

  const reserveOverview = useMemo(() => {
    const totals = {
      reserved: 0,
      consumed: 0,
      released: 0,
    };

    payouts.forEach((item) => {
      const reserveStatus = item.reserveStatus.toLowerCase();
      if (reserveStatus === "consumed") {
        totals.consumed += item.amount;
      } else if (reserveStatus === "released") {
        totals.released += item.amount;
      } else {
        totals.reserved += item.amount;
      }
    });

    return totals;
  }, [payouts]);

  async function handleCreatePayout(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSuccessMessage(null);
    setError(null);

    const parsedAmount = Number(amount);
    if (!Number.isFinite(parsedAmount) || parsedAmount <= 0) {
      setError("Amount must be a positive number.");
      return;
    }

    try {
      setBusy(true);
      const payout = await createPayout(
        {
          amount: parsedAmount,
          currency: currency.trim().toLowerCase(),
          description: description.trim() || undefined,
          stripeAccountId: stripeAccountId.trim() || undefined,
          externalReference: externalReference.trim() || undefined,
        },
        createIdempotencyKey(),
      );

      setPayouts((current) => [payout, ...current]);
      setSuccessMessage(
        `Payout ${payout.id} created with status ${payout.status}.`,
      );
    } catch (requestError) {
      const message =
        requestError instanceof Error
          ? requestError.message
          : "Failed to create payout";
      setError(message);
    } finally {
      setBusy(false);
    }
  }

  async function handleReconcile(payoutId: string) {
    setError(null);
    setSuccessMessage(null);

    try {
      setBusy(true);
      const updated = await reconcilePayout(payoutId);
      setPayouts((current) =>
        current.map((item) => (item.id === updated.id ? updated : item)),
      );
      setSuccessMessage(`Payout ${updated.id} reconciled: ${updated.status}.`);
    } catch (requestError) {
      const message =
        requestError instanceof Error
          ? requestError.message
          : "Failed to reconcile payout";
      setError(message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl mb-2">Payouts</h1>
        <p className="text-muted-foreground">
          Create withdrawals and monitor reserve consumption.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="rounded-xl border border-border bg-card p-4">
          <p className="text-xs uppercase tracking-wide text-muted-foreground">
            Reserved
          </p>
          <p className="text-2xl mt-2">
            {formatMoney(reserveOverview.reserved, currency.toUpperCase())}
          </p>
        </div>
        <div className="rounded-xl border border-border bg-card p-4">
          <p className="text-xs uppercase tracking-wide text-muted-foreground">
            Consumed
          </p>
          <p className="text-2xl mt-2 text-success">
            {formatMoney(reserveOverview.consumed, currency.toUpperCase())}
          </p>
        </div>
        <div className="rounded-xl border border-border bg-card p-4">
          <p className="text-xs uppercase tracking-wide text-muted-foreground">
            Released
          </p>
          <p className="text-2xl mt-2 text-warning">
            {formatMoney(reserveOverview.released, currency.toUpperCase())}
          </p>
        </div>
      </div>

      <form
        onSubmit={handleCreatePayout}
        className="rounded-xl border border-border bg-card p-5 space-y-4"
      >
        <h2 className="text-xl">Create payout</h2>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <label className="space-y-2">
            <span className="text-sm text-muted-foreground">Amount</span>
            <input
              type="number"
              min="0.01"
              step="0.01"
              className="w-full px-3 py-2 bg-input-background border border-input rounded-lg text-sm"
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              required
            />
          </label>

          <label className="space-y-2">
            <span className="text-sm text-muted-foreground">Currency</span>
            <input
              type="text"
              className="w-full px-3 py-2 bg-input-background border border-input rounded-lg text-sm"
              value={currency}
              onChange={(event) => setCurrency(event.target.value)}
              required
            />
          </label>

          <label className="space-y-2">
            <span className="text-sm text-muted-foreground">Description</span>
            <input
              type="text"
              className="w-full px-3 py-2 bg-input-background border border-input rounded-lg text-sm"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              placeholder="Merchant settlement batch"
            />
          </label>

          <label className="space-y-2">
            <span className="text-sm text-muted-foreground">
              Stripe account ID (optional)
            </span>
            <input
              type="text"
              className="w-full px-3 py-2 bg-input-background border border-input rounded-lg text-sm"
              value={stripeAccountId}
              onChange={(event) => setStripeAccountId(event.target.value)}
              placeholder="acct_..."
            />
          </label>

          <label className="space-y-2 md:col-span-2">
            <span className="text-sm text-muted-foreground">
              External reference (optional)
            </span>
            <input
              type="text"
              className="w-full px-3 py-2 bg-input-background border border-input rounded-lg text-sm"
              value={externalReference}
              onChange={(event) => setExternalReference(event.target.value)}
              placeholder="invoice-batch-2026-03-20"
            />
          </label>
        </div>

        <button
          type="submit"
          className="px-5 py-2 rounded-lg bg-primary text-primary-foreground hover:opacity-90 disabled:opacity-60"
          disabled={busy}
        >
          {busy ? "Processing..." : "Create payout"}
        </button>
      </form>

      {error ? (
        <div className="p-3 rounded-lg border border-destructive/30 bg-destructive/10 text-destructive text-sm">
          {error}
        </div>
      ) : null}

      {successMessage ? (
        <div className="p-3 rounded-lg border border-success/30 bg-success/10 text-success text-sm">
          {successMessage}
        </div>
      ) : null}

      <div className="rounded-xl border border-border bg-card overflow-hidden">
        <div className="px-5 py-4 border-b border-border">
          <h2 className="text-xl">Payout history</h2>
        </div>

        {loading ? (
          <div className="p-5 text-muted-foreground">Loading payouts...</div>
        ) : payouts.length === 0 ? (
          <div className="p-5 text-muted-foreground">No payouts yet.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[940px]">
              <thead className="bg-secondary/40">
                <tr>
                  <th className="px-4 py-3 text-left text-xs uppercase tracking-wide text-muted-foreground">
                    ID
                  </th>
                  <th className="px-4 py-3 text-left text-xs uppercase tracking-wide text-muted-foreground">
                    Amount
                  </th>
                  <th className="px-4 py-3 text-left text-xs uppercase tracking-wide text-muted-foreground">
                    Status
                  </th>
                  <th className="px-4 py-3 text-left text-xs uppercase tracking-wide text-muted-foreground">
                    Reserve
                  </th>
                  <th className="px-4 py-3 text-left text-xs uppercase tracking-wide text-muted-foreground">
                    Stripe payout
                  </th>
                  <th className="px-4 py-3 text-left text-xs uppercase tracking-wide text-muted-foreground">
                    Updated
                  </th>
                  <th className="px-4 py-3 text-left text-xs uppercase tracking-wide text-muted-foreground">
                    Action
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {payouts.map((item) => (
                  <tr key={item.id} className="hover:bg-secondary/20">
                    <td className="px-4 py-3 font-mono text-xs">{item.id}</td>
                    <td className="px-4 py-3">
                      {formatMoney(item.amount, item.currency.toUpperCase())}
                    </td>
                    <td className="px-4 py-3">
                      <span
                        className={`inline-flex px-2 py-1 rounded-md border text-xs ${statusTone(item.status)}`}
                      >
                        {item.status}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <span
                        className={`inline-flex px-2 py-1 rounded-md border text-xs ${statusTone(item.reserveStatus)}`}
                      >
                        {item.reserveStatus}
                      </span>
                    </td>
                    <td className="px-4 py-3 font-mono text-xs">
                      {item.stripePayoutId}
                    </td>
                    <td className="px-4 py-3 text-sm text-muted-foreground">
                      {formatDateTime(item.updatedAt)}
                    </td>
                    <td className="px-4 py-3">
                      <button
                        type="button"
                        className="px-3 py-1 text-xs rounded-md border border-border hover:bg-secondary/50 disabled:opacity-50"
                        onClick={() => handleReconcile(item.id)}
                        disabled={busy}
                      >
                        Reconcile
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}