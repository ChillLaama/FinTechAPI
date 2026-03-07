import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router";
import {
  TrendingUp,
  DollarSign,
  Shield,
  Clock,
  ArrowUpCircle,
  ArrowDownCircle,
  Repeat,
} from "lucide-react";
import {
  LineChart,
  Line,
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";
import {
  getAccounts,
  getCurrencyLabel,
  getTransactions,
  getTransactionTypeLabel,
  measureApiLatency,
} from "../api/client";
import type { ApiAccount, ApiTransaction } from "../api/client";

function formatMoney(amount: number, currencyCode = "RUB"): string {
  return `${currencyCode} ${amount.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

function formatDateLabel(dateIso: string): string {
  const date = new Date(dateIso);
  return date.toLocaleDateString("en-US", { day: "2-digit", month: "2-digit" });
}

function mapTypeBadge(type: number | string) {
  const typeLabel = getTransactionTypeLabel(type).toLowerCase();

  if (typeLabel === "income") {
    return {
      label: "Income",
      icon: <ArrowUpCircle className="w-4 h-4 text-success" />,
      className: "bg-success/10 text-success border-success/20",
    };
  }

  if (typeLabel === "expense") {
    return {
      label: "Expense",
      icon: <ArrowDownCircle className="w-4 h-4 text-warning" />,
      className: "bg-warning/10 text-warning border-warning/20",
    };
  }

  return {
    label: "Transfer",
    icon: <Repeat className="w-4 h-4 text-accent" />,
    className: "bg-accent/10 text-accent border-accent/20",
  };
}

export function Dashboard() {
  const [period, setPeriod] = useState<"7d" | "30d">("7d");
  const [accounts, setAccounts] = useState<ApiAccount[]>([]);
  const [transactions, setTransactions] = useState<ApiTransaction[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [latencySamples, setLatencySamples] = useState<number[]>([]);

  useEffect(() => {
    async function loadData() {
      try {
        setLoading(true);
        setError(null);

        const [accountsData, transactionsData, latency] = await Promise.all([
          getAccounts(),
          getTransactions(),
          measureApiLatency(),
        ]);

        setAccounts(accountsData);
        setTransactions(transactionsData);
        setLatencySamples((previous) => [...previous.slice(-6), latency]);
      } catch (requestError) {
        const message = requestError instanceof Error ? requestError.message : "Failed to load data";
        setError(message);
      } finally {
        setLoading(false);
      }
    }

    loadData();
  }, []);

  const totalBalance = useMemo(() => accounts.reduce((sum, account) => sum + account.balance, 0), [accounts]);

  const daysToShow = period === "7d" ? 7 : 30;
  const chartData = useMemo(() => {
    const result: Array<{ date: string; amount: number }> = [];
    const now = new Date();

    for (let dayIndex = daysToShow - 1; dayIndex >= 0; dayIndex -= 1) {
      const date = new Date(now);
      date.setDate(now.getDate() - dayIndex);

      const dayStart = new Date(date);
      dayStart.setHours(0, 0, 0, 0);
      const dayEnd = new Date(date);
      dayEnd.setHours(23, 59, 59, 999);

      const dayTurnover = transactions
        .filter((transaction) => {
          const transactionDate = new Date(transaction.transactionDate);
          return transactionDate >= dayStart && transactionDate <= dayEnd;
        })
        .reduce((sum, transaction) => sum + Math.abs(transaction.amount), 0);

      result.push({
        date: date.toLocaleDateString("en-US", { day: "2-digit", month: "2-digit" }),
        amount: dayTurnover,
      });
    }

    return result;
  }, [daysToShow, transactions]);

  const recentTransactions = useMemo(
    () => [...transactions].sort((first, second) => new Date(second.transactionDate).getTime() - new Date(first.transactionDate).getTime()).slice(0, 5),
    [transactions],
  );

  const typeDistribution = useMemo(() => {
    if (transactions.length === 0) {
      return { income: 0, expense: 0, transfer: 0 };
    }

    const incomeCount = transactions.filter((transaction) => getTransactionTypeLabel(transaction.type).toLowerCase() === "income").length;
    const expenseCount = transactions.filter((transaction) => getTransactionTypeLabel(transaction.type).toLowerCase() === "expense").length;
    const transferCount = transactions.length - incomeCount - expenseCount;

    return {
      income: (incomeCount / transactions.length) * 100,
      expense: (expenseCount / transactions.length) * 100,
      transfer: (transferCount / transactions.length) * 100,
    };
  }, [transactions]);

  const latencyData = useMemo(
    () => latencySamples.map((value, index) => ({ time: `${index + 1}`, ms: Number(value.toFixed(2)) })),
    [latencySamples],
  );

  const averageLatency = useMemo(() => {
    if (latencySamples.length === 0) {
      return 0;
    }

    return latencySamples.reduce((sum, value) => sum + value, 0) / latencySamples.length;
  }, [latencySamples]);

  if (loading) {
    return <div className="text-muted-foreground">Loading dashboard data...</div>;
  }

  if (error) {
    return (
      <div className="space-y-3">
        <h1 className="text-3xl">Dashboard</h1>
        <div className="p-4 rounded-lg border border-destructive/30 bg-destructive/10 text-destructive text-sm">
          {error}. Check authorization (JWT token) and API availability.
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-3xl mb-2">Dashboard</h1>
          <p className="text-muted-foreground">Overview of financial operations from live API data</p>
        </div>

        <Link
          to="/create-payment"
          className="px-6 py-3 bg-accent text-accent-foreground rounded-lg hover:bg-accent/90 transition-colors flex items-center gap-2"
        >
          <DollarSign className="w-5 h-5" />
          Create payment
        </Link>
      </div>

      <div className="bg-gradient-to-br from-primary to-accent p-8 rounded-xl shadow-lg">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-primary-foreground/80 mb-2">Total account balance</p>
            <h2 className="text-5xl text-primary-foreground">{formatMoney(totalBalance)}</h2>
            <div className="flex items-center gap-2 mt-4 text-primary-foreground/90">
              <TrendingUp className="w-4 h-4" />
              <span className="text-sm">Accounts: {accounts.length} · Transactions: {transactions.length}</span>
            </div>
          </div>

          <div className="flex flex-col gap-3">
            <div className="bg-white/10 backdrop-blur-sm px-4 py-3 rounded-lg">
              <div className="flex items-center gap-2">
                <Shield className="w-5 h-5 text-primary-foreground" />
                <div>
                  <p className="text-xs text-primary-foreground/80">Data source</p>
                  <p className="text-sm text-primary-foreground">FinTech API</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-card p-6 rounded-xl border border-border">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-muted-foreground">Turnover for {period === "7d" ? "7 days" : "30 days"}</h3>
            <div className="flex gap-2">
              <button
                onClick={() => setPeriod("7d")}
                className={`px-3 py-1 rounded-md text-xs transition-colors ${
                  period === "7d" ? "bg-primary text-primary-foreground" : "bg-secondary text-muted-foreground hover:bg-secondary/80"
                }`}
              >
                7d
              </button>
              <button
                onClick={() => setPeriod("30d")}
                className={`px-3 py-1 rounded-md text-xs transition-colors ${
                  period === "30d" ? "bg-primary text-primary-foreground" : "bg-secondary text-muted-foreground hover:bg-secondary/80"
                }`}
              >
                30d
              </button>
            </div>
          </div>
          <div className="h-48">
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={chartData}>
                <defs>
                  <linearGradient id="colorAmount" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#06b6d4" stopOpacity={0.3} />
                    <stop offset="95%" stopColor="#06b6d4" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
                <XAxis dataKey="date" stroke="#94a3b8" fontSize={12} />
                <YAxis stroke="#94a3b8" fontSize={12} />
                <Tooltip
                  contentStyle={{
                    backgroundColor: "#1e293b",
                    border: "1px solid #334155",
                    borderRadius: "8px",
                  }}
                />
                <Area type="monotone" dataKey="amount" stroke="#06b6d4" strokeWidth={2} fill="url(#colorAmount)" />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </div>

        <div className="bg-card p-6 rounded-xl border border-border">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-muted-foreground">Operation distribution</h3>
            <ArrowDownCircle className="w-5 h-5 text-warning" />
          </div>
          <div className="space-y-4">
            <div>
              <div className="flex justify-between mb-2">
                <span className="text-sm">Income</span>
                <span className="text-success">{typeDistribution.income.toFixed(1)}%</span>
              </div>
              <div className="w-full bg-secondary rounded-full h-2">
                <div className="bg-success h-2 rounded-full" style={{ width: `${typeDistribution.income}%` }} />
              </div>
            </div>

            <div>
              <div className="flex justify-between mb-2">
                <span className="text-sm">Expense</span>
                <span className="text-warning">{typeDistribution.expense.toFixed(1)}%</span>
              </div>
              <div className="w-full bg-secondary rounded-full h-2">
                <div className="bg-warning h-2 rounded-full" style={{ width: `${typeDistribution.expense}%` }} />
              </div>
            </div>

            <div>
              <div className="flex justify-between mb-2">
                <span className="text-sm">Transfer</span>
                <span className="text-accent">{typeDistribution.transfer.toFixed(1)}%</span>
              </div>
              <div className="w-full bg-secondary rounded-full h-2">
                <div className="bg-accent h-2 rounded-full" style={{ width: `${typeDistribution.transfer}%` }} />
              </div>
            </div>
          </div>
        </div>

        <div className="bg-card p-6 rounded-xl border border-border">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-muted-foreground">API Latency</h3>
            <Clock className="w-5 h-5 text-accent" />
          </div>
          <div className="h-48">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={latencyData}>
                <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
                <XAxis dataKey="time" stroke="#94a3b8" fontSize={12} />
                <YAxis stroke="#94a3b8" fontSize={12} />
                <Tooltip
                  contentStyle={{
                    backgroundColor: "#1e293b",
                    border: "1px solid #334155",
                    borderRadius: "8px",
                  }}
                />
                <Line type="monotone" dataKey="ms" stroke="#06b6d4" strokeWidth={2} dot={{ fill: "#06b6d4", r: 3 }} />
              </LineChart>
            </ResponsiveContainer>
          </div>
          <div className="mt-4 text-center">
            <p className="text-2xl text-accent">{averageLatency.toFixed(2)}ms</p>
            <p className="text-xs text-muted-foreground">Average latency</p>
          </div>
        </div>
      </div>

      <div className="bg-card rounded-xl border border-border overflow-hidden">
        <div className="p-6 border-b border-border flex items-center justify-between">
          <h2>Recent transactions</h2>
          <Link to="/transactions" className="text-sm text-accent hover:text-accent/80 transition-colors">
            View all →
          </Link>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-secondary/50">
              <tr>
                <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">ID</th>
                <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">Description</th>
                <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">Amount</th>
                <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">Type</th>
                <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">Date</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {recentTransactions.map((transaction) => {
                const badge = mapTypeBadge(transaction.type);
                return (
                  <tr key={transaction.id} className="hover:bg-secondary/30 transition-colors">
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-2">
                        {badge.icon}
                        <span className="text-sm font-mono">#{transaction.id}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <span className="text-sm">{transaction.description || "—"}</span>
                    </td>
                    <td className="px-6 py-4">
                      <span className="text-sm">{formatMoney(transaction.amount, getCurrencyLabel(transaction.currency))}</span>
                    </td>
                    <td className="px-6 py-4">
                      <span className={`px-2 py-1 rounded-md border text-xs ${badge.className}`}>{badge.label}</span>
                    </td>
                    <td className="px-6 py-4">
                      <span className="text-sm text-muted-foreground">{formatDateLabel(transaction.transactionDate)}</span>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          {recentTransactions.length === 0 && (
            <div className="p-10 text-center text-muted-foreground">No transactions to display</div>
          )}
        </div>
      </div>
    </div>
  );
}
