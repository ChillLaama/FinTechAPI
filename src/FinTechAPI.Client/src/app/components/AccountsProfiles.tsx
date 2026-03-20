import { useEffect, useState } from "react";
import { FolderOpen, Landmark, Layers, Loader2 } from "lucide-react";
import { getAccounts } from "../api/client";
import type { ApiAccount } from "../api/client";

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
      <div>
        <h1 className="text-3xl mb-2">Account profiles</h1>
        <p className="text-muted-foreground">
          Logical grouping and reporting profiles. Monetary balance is not
          stored here.
        </p>
      </div>

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
