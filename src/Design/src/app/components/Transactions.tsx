import { useState } from "react";
import { 
  Filter, 
  Search, 
  X,
  CheckCircle,
  XCircle,
  AlertTriangle,
  Shield,
  Clock,
  ExternalLink
} from "lucide-react";

interface Transaction {
  id: string;
  amount: number;
  status: "success" | "warning" | "rejected";
  fraudScore: number;
  time: string;
  date: string;
  recipient: string;
  stripeId: string;
  description: string;
  mlAnalysis: {
    riskLevel: string;
    flags: string[];
    confidence: number;
  };
  webhookEvents: string[];
}

const mockTransactions: Transaction[] = [
  {
    id: "TXN-2024-001",
    amount: 1250.00,
    status: "success",
    fraudScore: 0.12,
    time: "10:34:22",
    date: "04.03.2026",
    recipient: "ООО Техносервис",
    stripeId: "pi_3N8QZ82eZvKYlo2C0z1n8Y9x",
    description: "Оплата услуг техподдержки",
    mlAnalysis: {
      riskLevel: "Низкий",
      flags: [],
      confidence: 0.94
    },
    webhookEvents: ["payment.created", "payment.succeeded"]
  },
  {
    id: "TXN-2024-002",
    amount: 5600.00,
    status: "success",
    fraudScore: 0.08,
    time: "10:28:15",
    date: "04.03.2026",
    recipient: "ИП Иванов А.С.",
    stripeId: "pi_3N8QY72eZvKYlo2C0a2b9Z0y",
    description: "Консультационные услуги",
    mlAnalysis: {
      riskLevel: "Низкий",
      flags: [],
      confidence: 0.97
    },
    webhookEvents: ["payment.created", "payment.succeeded"]
  },
  {
    id: "TXN-2024-003",
    amount: 980.00,
    status: "warning",
    fraudScore: 0.64,
    time: "10:22:41",
    date: "04.03.2026",
    recipient: "ООО Комплекс",
    stripeId: "pi_3N8QX62eZvKYlo2C0b3c0A1z",
    description: "Закупка материалов",
    mlAnalysis: {
      riskLevel: "Средний",
      flags: ["Новый получатель", "Необычная сумма"],
      confidence: 0.68
    },
    webhookEvents: ["payment.created", "payment.requires_action"]
  },
  {
    id: "TXN-2024-004",
    amount: 12500.00,
    status: "rejected",
    fraudScore: 0.89,
    time: "10:15:33",
    date: "04.03.2026",
    recipient: "Неизвестный получатель",
    stripeId: "pi_3N8QW52eZvKYlo2C0c4d1B2a",
    description: "Подозрительная транзакция",
    mlAnalysis: {
      riskLevel: "Высокий",
      flags: ["Неверифицированный получатель", "Аномальная сумма", "Подозрительный IP"],
      confidence: 0.92
    },
    webhookEvents: ["payment.created", "payment.failed"]
  },
  {
    id: "TXN-2024-005",
    amount: 3200.00,
    status: "success",
    fraudScore: 0.15,
    time: "10:08:19",
    date: "04.03.2026",
    recipient: "ООО ТоргПром",
    stripeId: "pi_3N8QV42eZvKYlo2C0d5e2C3b",
    description: "Регулярный платёж",
    mlAnalysis: {
      riskLevel: "Низкий",
      flags: [],
      confidence: 0.96
    },
    webhookEvents: ["payment.created", "payment.succeeded"]
  },
  {
    id: "TXN-2024-006",
    amount: 7800.00,
    status: "success",
    fraudScore: 0.21,
    time: "09:54:12",
    date: "04.03.2026",
    recipient: "ООО МегаСтрой",
    stripeId: "pi_3N8QU32eZvKYlo2C0e6f3D4c",
    description: "Поставка оборудования",
    mlAnalysis: {
      riskLevel: "Низкий",
      flags: [],
      confidence: 0.91
    },
    webhookEvents: ["payment.created", "payment.succeeded"]
  },
  {
    id: "TXN-2024-007",
    amount: 450.00,
    status: "warning",
    fraudScore: 0.58,
    time: "09:45:38",
    date: "04.03.2026",
    recipient: "ИП Петров В.И.",
    stripeId: "pi_3N8QT22eZvKYlo2C0f7g4E5d",
    description: "Разовая услуга",
    mlAnalysis: {
      riskLevel: "Средний",
      flags: ["Нетипичное время операции"],
      confidence: 0.73
    },
    webhookEvents: ["payment.created", "payment.requires_action"]
  },
  {
    id: "TXN-2024-008",
    amount: 15200.00,
    status: "success",
    fraudScore: 0.09,
    time: "09:32:45",
    date: "04.03.2026",
    recipient: "ООО ТехноПарк",
    stripeId: "pi_3N8QS12eZvKYlo2C0g8h5F6e",
    description: "Годовое обслуживание",
    mlAnalysis: {
      riskLevel: "Низкий",
      flags: [],
      confidence: 0.98
    },
    webhookEvents: ["payment.created", "payment.succeeded"]
  },
];

export function Transactions() {
  const [selectedTransaction, setSelectedTransaction] = useState<Transaction | null>(null);
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [riskFilter, setRiskFilter] = useState<string>("all");
  const [searchQuery, setSearchQuery] = useState("");

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

  const filteredTransactions = mockTransactions.filter(transaction => {
    const matchesStatus = statusFilter === "all" || transaction.status === statusFilter;
    const matchesRisk = riskFilter === "all" || 
      (riskFilter === "low" && transaction.fraudScore < 0.3) ||
      (riskFilter === "medium" && transaction.fraudScore >= 0.3 && transaction.fraudScore < 0.6) ||
      (riskFilter === "high" && transaction.fraudScore >= 0.6);
    const matchesSearch = searchQuery === "" || 
      transaction.id.toLowerCase().includes(searchQuery.toLowerCase()) ||
      transaction.recipient.toLowerCase().includes(searchQuery.toLowerCase()) ||
      transaction.stripeId.toLowerCase().includes(searchQuery.toLowerCase());
    
    return matchesStatus && matchesRisk && matchesSearch;
  });

  return (
    <div className="flex gap-6 h-full">
      {/* Main Content */}
      <div className={`flex-1 space-y-6 ${selectedTransaction ? 'mr-[400px]' : ''} transition-all duration-300`}>
        <div>
          <h1 className="text-3xl mb-2">Транзакции</h1>
          <p className="text-muted-foreground">Полная история платежей и операций</p>
        </div>

        {/* Filters */}
        <div className="bg-card p-4 rounded-xl border border-border">
          <div className="flex flex-wrap gap-4">
            <div className="flex-1 min-w-[250px]">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-muted-foreground" />
                <input
                  type="text"
                  placeholder="Поиск по ID, получателю, Stripe ID..."
                  className="w-full pl-10 pr-4 py-2 bg-input-background border border-input rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                />
              </div>
            </div>

            <div className="flex gap-2">
              <div>
                <select
                  className="px-4 py-2 bg-input-background border border-input rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                  value={statusFilter}
                  onChange={(e) => setStatusFilter(e.target.value)}
                >
                  <option value="all">Все статусы</option>
                  <option value="success">Успешные</option>
                  <option value="warning">Проверка</option>
                  <option value="rejected">Отклонённые</option>
                </select>
              </div>

              <div>
                <select
                  className="px-4 py-2 bg-input-background border border-input rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                  value={riskFilter}
                  onChange={(e) => setRiskFilter(e.target.value)}
                >
                  <option value="all">Все риски</option>
                  <option value="low">Низкий риск</option>
                  <option value="medium">Средний риск</option>
                  <option value="high">Высокий риск</option>
                </select>
              </div>
            </div>
          </div>
        </div>

        {/* Transactions Table */}
        <div className="bg-card rounded-xl border border-border overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-secondary/50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">
                    ID
                  </th>
                  <th className="px-6 py-3 text-left text-xs text-muted-foreground uppercase tracking-wider">
                    Дата / Время
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
                    Риск
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {filteredTransactions.map((transaction) => (
                  <tr
                    key={transaction.id}
                    className="hover:bg-secondary/30 transition-colors cursor-pointer"
                    onClick={() => setSelectedTransaction(transaction)}
                  >
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-2">
                        {getStatusIcon(transaction.status)}
                        <span className="text-sm font-mono">{transaction.id}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <div>
                        <div className="text-sm">{transaction.date}</div>
                        <div className="text-xs text-muted-foreground">{transaction.time}</div>
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
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {filteredTransactions.length === 0 && (
            <div className="p-12 text-center text-muted-foreground">
              Транзакции не найдены
            </div>
          )}
        </div>
      </div>

      {/* Side Panel */}
      {selectedTransaction && (
        <div className="fixed right-0 top-16 bottom-0 w-[400px] bg-card border-l border-border overflow-y-auto shadow-xl">
          <div className="sticky top-0 bg-card border-b border-border p-4 flex items-center justify-between z-10">
            <h3>Детали транзакции</h3>
            <button
              onClick={() => setSelectedTransaction(null)}
              className="p-2 rounded-lg hover:bg-secondary transition-colors"
            >
              <X className="w-5 h-5" />
            </button>
          </div>

          <div className="p-6 space-y-6">
            {/* Transaction Info */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                {getStatusIcon(selectedTransaction.status)}
                <span className="font-mono text-sm">{selectedTransaction.id}</span>
              </div>
              
              <div className="space-y-3">
                <div>
                  <p className="text-xs text-muted-foreground mb-1">Сумма</p>
                  <p className="text-2xl">₽ {selectedTransaction.amount.toLocaleString('ru-RU', { minimumFractionDigits: 2 })}</p>
                </div>

                <div>
                  <p className="text-xs text-muted-foreground mb-1">Получатель</p>
                  <p>{selectedTransaction.recipient}</p>
                </div>

                <div>
                  <p className="text-xs text-muted-foreground mb-1">Описание</p>
                  <p className="text-sm">{selectedTransaction.description}</p>
                </div>

                <div>
                  <p className="text-xs text-muted-foreground mb-1">Дата и время</p>
                  <p className="text-sm">{selectedTransaction.date} в {selectedTransaction.time}</p>
                </div>

                <div>
                  <p className="text-xs text-muted-foreground mb-1">Stripe Payment ID</p>
                  <div className="flex items-center gap-2">
                    <code className="text-xs bg-secondary px-2 py-1 rounded">{selectedTransaction.stripeId}</code>
                    <ExternalLink className="w-3 h-3 text-muted-foreground" />
                  </div>
                </div>
              </div>
            </div>

            {/* ML Analysis */}
            <div className="border-t border-border pt-6">
              <div className="flex items-center gap-2 mb-4">
                <Shield className="w-5 h-5 text-accent" />
                <h4>ML-анализ рисков</h4>
              </div>

              <div className="space-y-4">
                <div className="bg-secondary/30 p-4 rounded-lg">
                  <div className="flex justify-between items-center mb-2">
                    <span className="text-sm text-muted-foreground">Уровень риска</span>
                    <span className={`text-sm ${
                      selectedTransaction.mlAnalysis.riskLevel === "Низкий" ? "text-success" :
                      selectedTransaction.mlAnalysis.riskLevel === "Средний" ? "text-warning" :
                      "text-destructive"
                    }`}>
                      {selectedTransaction.mlAnalysis.riskLevel}
                    </span>
                  </div>
                  <div className="flex justify-between items-center mb-2">
                    <span className="text-sm text-muted-foreground">Fraud Score</span>
                    <span className="text-sm">{selectedTransaction.fraudScore.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-muted-foreground">Уверенность модели</span>
                    <span className="text-sm">{(selectedTransaction.mlAnalysis.confidence * 100).toFixed(0)}%</span>
                  </div>
                </div>

                {selectedTransaction.mlAnalysis.flags.length > 0 && (
                  <div>
                    <p className="text-sm text-muted-foreground mb-2">Причины флага</p>
                    <div className="space-y-2">
                      {selectedTransaction.mlAnalysis.flags.map((flag, index) => (
                        <div key={index} className="flex items-start gap-2 text-sm">
                          <AlertTriangle className="w-4 h-4 text-warning mt-0.5 flex-shrink-0" />
                          <span>{flag}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </div>

            {/* Webhook Events */}
            <div className="border-t border-border pt-6">
              <div className="flex items-center gap-2 mb-4">
                <Clock className="w-5 h-5 text-accent" />
                <h4>Webhook-события</h4>
              </div>

              <div className="space-y-2">
                {selectedTransaction.webhookEvents.map((event, index) => (
                  <div key={index} className="flex items-center gap-2 p-2 bg-secondary/30 rounded-lg">
                    <CheckCircle className="w-4 h-4 text-success flex-shrink-0" />
                    <code className="text-xs">{event}</code>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
