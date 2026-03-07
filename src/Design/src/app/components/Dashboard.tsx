import { useState } from "react";
import { Link } from "react-router";
import { 
  TrendingUp, 
  TrendingDown, 
  DollarSign, 
  Shield, 
  Clock,
  AlertTriangle,
  CheckCircle,
  XCircle
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
  ResponsiveContainer 
} from "recharts";

// Mock data
const chartData = [
  { date: "01.03", amount: 24500 },
  { date: "02.03", amount: 28300 },
  { date: "03.03", amount: 26100 },
  { date: "04.03", amount: 31200 },
  { date: "05.03", amount: 29800 },
  { date: "06.03", amount: 34500 },
  { date: "07.03", amount: 38200 },
];

const latencyData = [
  { time: "10:00", ms: 2.3 },
  { time: "10:15", ms: 1.8 },
  { time: "10:30", ms: 2.1 },
  { time: "10:45", ms: 1.9 },
  { time: "11:00", ms: 2.4 },
  { time: "11:15", ms: 2.0 },
  { time: "11:30", ms: 1.7 },
];

const recentTransactions = [
  {
    id: "TXN-2024-001",
    amount: 1250.00,
    status: "success",
    fraudScore: 0.12,
    time: "10:34:22",
    recipient: "ООО Техносервис"
  },
  {
    id: "TXN-2024-002",
    amount: 5600.00,
    status: "success",
    fraudScore: 0.08,
    time: "10:28:15",
    recipient: "ИП Иванов А.С."
  },
  {
    id: "TXN-2024-003",
    amount: 980.00,
    status: "warning",
    fraudScore: 0.64,
    time: "10:22:41",
    recipient: "ООО Комплекс"
  },
  {
    id: "TXN-2024-004",
    amount: 12500.00,
    status: "rejected",
    fraudScore: 0.89,
    time: "10:15:33",
    recipient: "Неизвестный получатель"
  },
  {
    id: "TXN-2024-005",
    amount: 3200.00,
    status: "success",
    fraudScore: 0.15,
    time: "10:08:19",
    recipient: "ООО ТоргПром"
  },
];

export function Dashboard() {
  const [period, setPeriod] = useState<"7d" | "30d">("7d");

  const getStatusIcon = (status: string) => {
    switch (status) {
      case "success":
        return <CheckCircle className="w-4 h-4 text-success" />;
      case "warning":
        return <AlertTriangle className="w-4 h-4 text-warning" />;
      case "rejected":
        return <XCircle className="w-4 h-4 text-destructive" />;
      default:
        return null;
    }
  };

  const getStatusBadge = (status: string) => {
    const classes = {
      success: "bg-success/10 text-success border-success/20",
      warning: "bg-warning/10 text-warning border-warning/20",
      rejected: "bg-destructive/10 text-destructive border-destructive/20",
    };

    const labels = {
      success: "Успешно",
      warning: "Проверка",
      rejected: "Отклонён",
    };

    return (
      <span className={`px-2 py-1 rounded-md border text-xs ${classes[status as keyof typeof classes]}`}>
        {labels[status as keyof typeof labels]}
      </span>
    );
  };

  const getFraudScoreBadge = (score: number) => {
    if (score < 0.3) {
      return <span className="px-2 py-1 rounded-md bg-success/10 text-success text-xs">Низкий</span>;
    } else if (score < 0.6) {
      return <span className="px-2 py-1 rounded-md bg-warning/10 text-warning text-xs">Средний</span>;
    } else {
      return <span className="px-2 py-1 rounded-md bg-destructive/10 text-destructive text-xs">Высокий</span>;
    }
  };

  return (
    <div className="space-y-6">
      {/* Header Section */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-3xl mb-2">Dashboard</h1>
          <p className="text-muted-foreground">Обзор финансовых операций и безопасности</p>
        </div>
        
        <Link
          to="/create-payment"
          className="px-6 py-3 bg-accent text-accent-foreground rounded-lg hover:bg-accent/90 transition-colors flex items-center gap-2"
        >
          <DollarSign className="w-5 h-5" />
          Создать платёж
        </Link>
      </div>

      {/* Balance Card */}
      <div className="bg-gradient-to-br from-primary to-accent p-8 rounded-xl shadow-lg">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-primary-foreground/80 mb-2">Баланс счета</p>
            <h2 className="text-5xl text-primary-foreground">₽ 2,847,650.00</h2>
            <div className="flex items-center gap-2 mt-4 text-primary-foreground/90">
              <TrendingUp className="w-4 h-4" />
              <span className="text-sm">+12.5% за последние 30 дней</span>
            </div>
          </div>
          
          <div className="flex flex-col gap-3">
            <div className="bg-white/10 backdrop-blur-sm px-4 py-3 rounded-lg">
              <div className="flex items-center gap-2">
                <Shield className="w-5 h-5 text-primary-foreground" />
                <div>
                  <p className="text-xs text-primary-foreground/80">Защита активна</p>
                  <p className="text-sm text-primary-foreground">ML Fraud Detection</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-card p-6 rounded-xl border border-border">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-muted-foreground">Обороты за {period === "7d" ? "7 дней" : "30 дней"}</h3>
            <div className="flex gap-2">
              <button
                onClick={() => setPeriod("7d")}
                className={`px-3 py-1 rounded-md text-xs transition-colors ${
                  period === "7d" 
                    ? "bg-primary text-primary-foreground" 
                    : "bg-secondary text-muted-foreground hover:bg-secondary/80"
                }`}
              >
                7д
              </button>
              <button
                onClick={() => setPeriod("30d")}
                className={`px-3 py-1 rounded-md text-xs transition-colors ${
                  period === "30d" 
                    ? "bg-primary text-primary-foreground" 
                    : "bg-secondary text-muted-foreground hover:bg-secondary/80"
                }`}
              >
                30д
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
                    backgroundColor: '#1e293b', 
                    border: '1px solid #334155',
                    borderRadius: '8px'
                  }} 
                />
                <Area 
                  type="monotone" 
                  dataKey="amount" 
                  stroke="#06b6d4" 
                  strokeWidth={2}
                  fill="url(#colorAmount)" 
                />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </div>

        <div className="bg-card p-6 rounded-xl border border-border">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-muted-foreground">Fraud Detection</h3>
            <AlertTriangle className="w-5 h-5 text-warning" />
          </div>
          <div className="space-y-4">
            <div>
              <div className="flex justify-between mb-2">
                <span className="text-sm">Отклонено транзакций</span>
                <span className="text-destructive">2.3%</span>
              </div>
              <div className="w-full bg-secondary rounded-full h-2">
                <div className="bg-destructive h-2 rounded-full" style={{ width: "2.3%" }} />
              </div>
            </div>
            
            <div>
              <div className="flex justify-between mb-2">
                <span className="text-sm">Проверяются</span>
                <span className="text-warning">5.8%</span>
              </div>
              <div className="w-full bg-secondary rounded-full h-2">
                <div className="bg-warning h-2 rounded-full" style={{ width: "5.8%" }} />
              </div>
            </div>
            
            <div>
              <div className="flex justify-between mb-2">
                <span className="text-sm">Успешных</span>
                <span className="text-success">91.9%</span>
              </div>
              <div className="w-full bg-secondary rounded-full h-2">
                <div className="bg-success h-2 rounded-full" style={{ width: "91.9%" }} />
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
                    backgroundColor: '#1e293b', 
                    border: '1px solid #334155',
                    borderRadius: '8px'
                  }} 
                />
                <Line 
                  type="monotone" 
                  dataKey="ms" 
                  stroke="#06b6d4" 
                  strokeWidth={2}
                  dot={{ fill: '#06b6d4', r: 3 }}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
          <div className="mt-4 text-center">
            <p className="text-2xl text-accent">1.9ms</p>
            <p className="text-xs text-muted-foreground">Средняя задержка</p>
          </div>
        </div>
      </div>

      {/* Recent Transactions */}
      <div className="bg-card rounded-xl border border-border overflow-hidden">
        <div className="p-6 border-b border-border flex items-center justify-between">
          <h2>Последние транзакции</h2>
          <Link 
            to="/transactions" 
            className="text-sm text-accent hover:text-accent/80 transition-colors"
          >
            Посмотреть все →
          </Link>
        </div>
        
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-secondary/50">
              <tr>
                <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">
                  ID
                </th>
                <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">
                  Получатель
                </th>
                <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">
                  Сумма
                </th>
                <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">
                  Статус
                </th>
                <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">
                  Fraud Score
                </th>
                <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">
                  Время
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {recentTransactions.map((transaction) => (
                <tr 
                  key={transaction.id} 
                  className="hover:bg-secondary/30 transition-colors cursor-pointer"
                >
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-2">
                      {getStatusIcon(transaction.status)}
                      <span className="text-sm font-mono">{transaction.id}</span>
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <span className="text-sm">{transaction.recipient}</span>
                  </td>
                  <td className="px-6 py-4">
                    <span className="text-sm">₽ {transaction.amount.toLocaleString('ru-RU', { minimumFractionDigits: 2 })}</span>
                  </td>
                  <td className="px-6 py-4">
                    {getStatusBadge(transaction.status)}
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-2">
                      <span className="text-sm">{transaction.fraudScore.toFixed(2)}</span>
                      {getFraudScoreBadge(transaction.fraudScore)}
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <span className="text-sm text-muted-foreground">{transaction.time}</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
