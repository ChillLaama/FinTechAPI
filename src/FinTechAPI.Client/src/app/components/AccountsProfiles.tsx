import { useEffect, useState } from "react";
import { FolderOpen, Landmark, Layers, Loader2, Plus } from "lucide-react";
import { getAccounts, createAccount, currencyLabels } from "../api/client";
import type { ApiAccount } from "../api/client";
import { Button } from "./ui/button";
import { Input } from "./ui/input";
import { Label } from "./ui/label";

const accountTypeLabels: Record<number, string> = {
  0: "Checking",
  1: "Savings",
  2: "Credit",
  3: "Investment",
  4: "Loan",
  5: "Business",
  6: "Joint",
  7: "Cash",
  8: "Emergency Fund",
  9: "Retirement",
};

function profileLabel(accountName: string): string {
  if (!accountName) {
    return "General profile";
  }

  return `${accountName} profile`;
}

export function AccountsProfiles() {
  const [accounts, setAccounts] = useState<ApiAccount[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [formName, setFormName] = useState("");
  const [formType, setFormType] = useState(0);
  const [formCurrency, setFormCurrency] = useState(1); // EUR
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  useEffect(() => {
    async function loadAccounts() {
      try {
        setLoading(true);
        setError(null);
        const data = await getAccounts();
        setAccounts(data);
      } catch (requestError) {
        const message =
          requestError instanceof Error
            ? requestError.message
            : "Failed to load account profiles";
        setError(message);
      } finally {
        setLoading(false);
      }
    }

    loadAccounts();
  }, []);

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setCreating(true);
    setCreateError(null);
    try {
      const created = await createAccount({
        name: formName.trim(),
        accountType: formType,
        currency: formCurrency,
      });
      setAccounts((prev) => [...prev, created]);
      setFormName("");
      setShowForm(false);
    } catch (err) {
      setCreateError(
        err instanceof Error ? err.message : "Failed to create account",
      );
    } finally {
      setCreating(false);
    }
  }

  if (loading) {
    return (
      <div className="flex items-center gap-2 text-muted-foreground">
        <Loader2 className="w-4 h-4 animate-spin" />
        Loading account profiles...
      </div>
    );
  }

  if (error) {
    return (
      <div className="space-y-3">
        <h1 className="text-3xl">Account profiles</h1>
        <div className="p-4 rounded-lg border border-destructive/30 bg-destructive/10 text-destructive text-sm">
          {error}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl mb-2">Account profiles</h1>
          <p className="text-muted-foreground">
            Logical grouping and reporting profiles. Monetary balance is not
            stored here.
          </p>
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => setShowForm((v) => !v)}
        >
          <Plus className="w-4 h-4 mr-1" />
          New account
        </Button>
      </div>

      {showForm && (
        <form
          onSubmit={handleCreate}
          className="bg-card border border-border rounded-xl p-5 space-y-4"
        >
          {createError && (
            <div className="p-3 rounded-lg border border-destructive/30 bg-destructive/10 text-destructive text-sm">
              {createError}
            </div>
          )}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="space-y-1">
              <Label htmlFor="acc-name">Name</Label>
              <Input
                id="acc-name"
                value={formName}
                onChange={(e) => setFormName(e.target.value)}
                placeholder="My savings"
                required
              />
            </div>
            <div className="space-y-1">
              <Label htmlFor="acc-type">Type</Label>
              <select
                id="acc-type"
                className="flex h-10 w-full rounded-md border border-input bg-input-background px-3 py-2 text-sm"
                value={formType}
                onChange={(e) => setFormType(Number(e.target.value))}
              >
                {Object.entries(accountTypeLabels).map(([val, label]) => (
                  <option key={val} value={val}>
                    {label}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-1">
              <Label htmlFor="acc-currency">Currency</Label>
              <select
                id="acc-currency"
                className="flex h-10 w-full rounded-md border border-input bg-input-background px-3 py-2 text-sm"
                value={formCurrency}
                onChange={(e) => setFormCurrency(Number(e.target.value))}
              >
                {Object.entries(currencyLabels).map(([val, label]) => (
                  <option key={val} value={val}>
                    {label}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div className="flex gap-2">
            <Button type="submit" disabled={creating || !formName.trim()}>
              {creating ? (
                <Loader2 className="w-4 h-4 animate-spin mr-1" />
              ) : null}
              Create
            </Button>
            <Button
              type="button"
              variant="ghost"
              onClick={() => setShowForm(false)}
            >
              Cancel
            </Button>
          </div>
        </form>
      )}

      {accounts.length === 0 ? (
        <div className="bg-card border border-border rounded-xl p-8 text-center text-muted-foreground">
          No account profiles available.
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {accounts.map((account) => (
            <div
              key={account.id}
              className="bg-card border border-border rounded-xl p-5 space-y-4"
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <Landmark className="w-5 h-5 text-accent" />
                  <h2>{account.name}</h2>
                </div>
                <span className="text-xs text-muted-foreground">
                  ID: {account.id}
                </span>
              </div>

              <div className="space-y-2">
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <FolderOpen className="w-4 h-4" />
                  {profileLabel(account.name)}
                </div>
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Layers className="w-4 h-4" />
                  Used for transaction grouping and reporting views.
                </div>
              </div>

              <div className="rounded-lg bg-secondary/40 p-3 text-xs text-muted-foreground">
                This entity is informational and does not represent stored
                funds.
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
