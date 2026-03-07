import { useEffect, useMemo, useState } from "react";
import {
  Search,
  X,
  ArrowUpCircle,
  ArrowDownCircle,
  Repeat,
  Landmark,
  CalendarClock,
  FileText,
} from "lucide-react";
import {
  getAccounts,
  getCurrencyLabel,
  getTransactions,
  getTransactionTypeLabel,
} from "../api/client";
import type { ApiAccount, ApiTransaction } from "../api/client";

function formatMoney(amount: number, currencyCode: string): string {
  return `${currencyCode} ${amount.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

function formatDateTime(dateIso: string): string {
  const date = new Date(dateIso);
  return date.toLocaleString("en-US");
}

function typeBadge(type: number | string) {
  const typeValue = getTransactionTypeLabel(type).toLowerCase();

  if (typeValue === "income") {
    return {
      label: "Income",
      className: "bg-success/10 text-success border-success/20",
      icon: <ArrowUpCircle className="w-4 h-4 text-success" />,
      key: "income",
    };
  }

  if (typeValue === "expense") {
    return {
      label: "Expense",
      className: "bg-warning/10 text-warning border-warning/20",
      icon: <ArrowDownCircle className="w-4 h-4 text-warning" />,
      key: "expense",
    };
  }

  return {
    label: "Transfer",
    className: "bg-accent/10 text-accent border-accent/20",
    icon: <Repeat className="w-4 h-4 text-accent" />,
    key: "transfer",
  };
}

export function Transactions() {
  const [selectedTransaction, setSelectedTransaction] = useState<ApiTransaction | null>(null);
  const [typeFilter, setTypeFilter] = useState<string>("all");
  const [amountFilter, setAmountFilter] = useState<string>("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [transactions, setTransactions] = useState<ApiTransaction[]>([]);
  const [accounts, setAccounts] = useState<ApiAccount[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadData() {
      try {
        setLoading(true);
        setError(null);

        const [transactionsData, accountsData] = await Promise.all([getTransactions(), getAccounts()]);
        setTransactions(transactionsData);
        setAccounts(accountsData);
      } catch (requestError) {
        const message = requestError instanceof Error ? requestError.message : "Failed to load transactions";
        setError(message);
      } finally {
        setLoading(false);
      }
    }

    loadData();
  }, []);

  const accountNameById = useMemo(() => {
    const map = new Map<number, string>();
    accounts.forEach((account) => map.set(account.id, account.name));
    return map;
  }, [accounts]);

  const filteredTransactions = useMemo(() => {
    return transactions.filter((transaction) => {
      const badge = typeBadge(transaction.type);
      const typeMatches = typeFilter === "all" || badge.key === typeFilter;

      const amountMatches =
        amountFilter === "all" ||
        (amountFilter === "small" && transaction.amount < 1000) ||
        (amountFilter === "medium" && transaction.amount >= 1000 && transaction.amount < 10000) ||
        (amountFilter === "large" && transaction.amount >= 10000);

      const accountName = accountNameById.get(transaction.accountId) || "";
      const search = searchQuery.toLowerCase();
      const searchMatches =
        search.length === 0 ||
        String(transaction.id).includes(search) ||
        (transaction.description || "").toLowerCase().includes(search) ||
        accountName.toLowerCase().includes(search);

      return typeMatches && amountMatches && searchMatches;
    });
  }, [transactions, typeFilter, amountFilter, searchQuery, accountNameById]);

  if (loading) {
    return <div className="text-muted-foreground">Loading transactions...</div>;
  }

  if (error) {
    return (
      <div className="space-y-3">
        <h1 className="text-3xl">Transactions</h1>
        <div className="p-4 rounded-lg border border-destructive/30 bg-destructive/10 text-destructive text-sm">
          {error}. Check authorization (JWT token) and API availability.
        </div>
      </div>
    );
  }

  return (
    <div className="flex gap-6 h-full">
      <div className={`flex-1 space-y-6 ${selectedTransaction ? "mr-[400px]" : ""} transition-all duration-300`}>
        <div>
          <h1 className="text-3xl mb-2">Transactions</h1>
          <p className="text-muted-foreground">Full operation history from FinTech API</p>
        </div>

        <div className="bg-card p-4 rounded-xl border border-border">
          <div className="flex flex-wrap gap-4">
            <div className="flex-1 min-w-[250px]">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-muted-foreground" />
                <input
                  type="text"
                  placeholder="Search by ID, description, account..."
                  className="w-full pl-10 pr-4 py-2 bg-input-background border border-input rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                  value={searchQuery}
                  onChange={(event) => setSearchQuery(event.target.value)}
                />
              </div>
            </div>

            <div className="flex gap-2">
              <select
                className="px-4 py-2 bg-input-background border border-input rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                value={typeFilter}
                onChange={(event) => setTypeFilter(event.target.value)}
              >
                <option value="all">All types</option>
                <option value="income">Income</option>
                <option value="expense">Expense</option>
                <option value="transfer">Transfer</option>
              </select>

              <select
                className="px-4 py-2 bg-input-background border border-input rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                value={amountFilter}
                onChange={(event) => setAmountFilter(event.target.value)}
              >
                <option value="all">Any amount</option>
                <option value="small">Up to 1,000</option>
                <option value="medium">1,000 – 10,000</option>
                <option value="large">Over 10,000</option>
              </select>
            </div>
          </div>
        </div>

        <div className="bg-card rounded-xl border border-border overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-secondary/50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">ID</th>
                  <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">Date / Time</th>
                  <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">Account</th>
                  <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">Amount</th>
                  <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">Type</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {filteredTransactions.map((transaction) => {
                  const badge = typeBadge(transaction.type);
                  return (
                    <tr
                      key={transaction.id}
                      className="hover:bg-secondary/30 transition-colors cursor-pointer"
                      onClick={() => setSelectedTransaction(transaction)}
                    >
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-2">
                          {badge.icon}
                          <span className="text-sm font-mono">#{transaction.id}</span>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <span className="text-sm text-muted-foreground">{formatDateTime(transaction.transactionDate)}</span>
                      </td>
                      <td className="px-6 py-4">
                        <span className="text-sm">{accountNameById.get(transaction.accountId) || `Account ${transaction.accountId}`}</span>
                      </td>
                      <td className="px-6 py-4">
                        <span className="text-sm">{formatMoney(transaction.amount, getCurrencyLabel(transaction.currency))}</span>
                      </td>
                      <td className="px-6 py-4">
                        <span className={`px-2 py-1 rounded-md border text-xs ${badge.className}`}>{badge.label}</span>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {filteredTransactions.length === 0 && <div className="p-12 text-center text-muted-foreground">No transactions found</div>}
        </div>
      </div>

      {selectedTransaction && (
        <div className="fixed right-0 top-16 bottom-0 w-[400px] bg-card border-l border-border overflow-y-auto shadow-xl">
          <div className="sticky top-0 bg-card border-b border-border p-4 flex items-center justify-between z-10">
            <h3>Transaction details</h3>
            <button onClick={() => setSelectedTransaction(null)} className="p-2 rounded-lg hover:bg-secondary transition-colors">
              <X className="w-5 h-5" />
            </button>
          </div>

          <div className="p-6 space-y-6">
            <div>
              <div className="flex items-center gap-2 mb-3">
                {typeBadge(selectedTransaction.type).icon}
                <span className="font-mono text-sm">#{selectedTransaction.id}</span>
              </div>

              <div className="space-y-3">
                <div>
                  <p className="text-xs text-muted-foreground mb-1">Amount</p>
                  <p className="text-2xl">{formatMoney(selectedTransaction.amount, getCurrencyLabel(selectedTransaction.currency))}</p>
                </div>

                <div>
                  <p className="text-xs text-muted-foreground mb-1">Operation type</p>
                  <span className={`px-2 py-1 rounded-md border text-xs ${typeBadge(selectedTransaction.type).className}`}>
                    {typeBadge(selectedTransaction.type).label}
                  </span>
                </div>

                <div>
                  <p className="text-xs text-muted-foreground mb-1">Account</p>
                  <p>{accountNameById.get(selectedTransaction.accountId) || `Account ${selectedTransaction.accountId}`}</p>
                </div>

                <div>
                  <p className="text-xs text-muted-foreground mb-1">Description</p>
                  <p className="text-sm">{selectedTransaction.description || "No description provided"}</p>
                </div>
              </div>
            </div>

            <div className="border-t border-border pt-6">
              <div className="flex items-center gap-2 mb-4">
                <CalendarClock className="w-5 h-5 text-accent" />
                <h4>Timestamps</h4>
              </div>

              <div className="space-y-2">
                <div className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg text-sm">
                  <span className="text-muted-foreground">Transaction date</span>
                  <span>{formatDateTime(selectedTransaction.transactionDate)}</span>
                </div>
                <div className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg text-sm">
                  <span className="text-muted-foreground">Created</span>
                  <span>{formatDateTime(selectedTransaction.createdAt)}</span>
                </div>
                <div className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg text-sm">
                  <span className="text-muted-foreground">Updated</span>
                  <span>{formatDateTime(selectedTransaction.updatedAt)}</span>
                </div>
              </div>
            </div>

            <div className="border-t border-border pt-6 space-y-2">
              <div className="flex items-center gap-2 mb-2">
                <Landmark className="w-5 h-5 text-accent" />
                <h4>Service data</h4>
              </div>
              <div className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg text-sm">
                <span className="text-muted-foreground">Account ID</span>
                <code>{selectedTransaction.accountId}</code>
              </div>
              <div className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg text-sm">
                <span className="text-muted-foreground">User ID</span>
                <code className="truncate max-w-[180px]">{selectedTransaction.userId}</code>
              </div>
              <div className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg text-sm">
                <span className="text-muted-foreground">Transaction ID</span>
                <code>{selectedTransaction.id}</code>
              </div>
            </div>

            <div className="border-t border-border pt-6">
              <div className="flex items-center gap-2 mb-2">
                <FileText className="w-5 h-5 text-accent" />
                <h4>API status</h4>
              </div>
              <p className="text-sm text-muted-foreground">Data is loaded from endpoint `GET /api/transactions`.</p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
