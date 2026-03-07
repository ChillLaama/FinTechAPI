import { useState } from "react";
import { useNavigate } from "react-router";
import { 
  DollarSign, 
  Send, 
  Shield, 
  CheckCircle, 
  AlertTriangle,
  XCircle,
  Loader2,
  ArrowLeft
} from "lucide-react";

type PaymentStep = "form" | "processing" | "result";

interface PaymentResult {
  success: boolean;
  transactionId: string;
  mlCheckTime: number;
  stripeStatus: string;
  riskWarning?: string;
  fraudScore: number;
}

export function CreatePayment() {
  const navigate = useNavigate();
  const [step, setStep] = useState<PaymentStep>("form");
  const [processing, setProcessing] = useState(false);
  const [result, setResult] = useState<PaymentResult | null>(null);

  const [formData, setFormData] = useState({
    amount: "",
    currency: "RUB",
    recipient: "",
    description: "",
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setProcessing(true);
    setStep("processing");

    // Симуляция обработки платежа
    await new Promise(resolve => setTimeout(resolve, 2000));

    // Симуляция ML-проверки
    const mlCheckTime = Math.random() * 3 + 1; // 1-4ms
    const fraudScore = Math.random();
    const isHighRisk = fraudScore > 0.6;

    const mockResult: PaymentResult = {
      success: !isHighRisk,
      transactionId: `TXN-2024-${Math.floor(Math.random() * 1000).toString().padStart(3, '0')}`,
      mlCheckTime: mlCheckTime,
      stripeStatus: isHighRisk ? "payment.failed" : "payment.succeeded",
      fraudScore: fraudScore,
      riskWarning: isHighRisk ? "Транзакция отклонена из-за высокого уровня риска" : undefined,
    };

    setResult(mockResult);
    setStep("result");
    setProcessing(false);
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
  };

  const navigateToTransactions = () => {
    navigate("/transactions");
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
          <h1 className="text-3xl mb-2">Создать платёж</h1>
          <p className="text-muted-foreground">Отправка платежа с ML-проверкой безопасности</p>
        </div>
      </div>

      {step === "form" && (
        <div className="bg-card p-8 rounded-xl border border-border">
          <form onSubmit={handleSubmit} className="space-y-6">
            <div>
              <label className="block mb-2 text-sm">Сумма</label>
              <div className="relative">
                <DollarSign className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-muted-foreground" />
                <input
                  type="number"
                  step="0.01"
                  required
                  placeholder="0.00"
                  className="w-full pl-12 pr-4 py-3 bg-input-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring"
                  value={formData.amount}
                  onChange={(e) => setFormData({ ...formData, amount: e.target.value })}
                />
              </div>
            </div>

            <div>
              <label className="block mb-2 text-sm">Валюта</label>
              <select
                className="w-full px-4 py-3 bg-input-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring"
                value={formData.currency}
                onChange={(e) => setFormData({ ...formData, currency: e.target.value })}
              >
                <option value="RUB">RUB - Российский рубль</option>
                <option value="USD">USD - Доллар США</option>
                <option value="EUR">EUR - Евро</option>
              </select>
            </div>

            <div>
              <label className="block mb-2 text-sm">Получатель</label>
              <input
                type="text"
                required
                placeholder="Введите имя получателя или компании"
                className="w-full px-4 py-3 bg-input-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring"
                value={formData.recipient}
                onChange={(e) => setFormData({ ...formData, recipient: e.target.value })}
              />
            </div>

            <div>
              <label className="block mb-2 text-sm">Назначение платежа</label>
              <textarea
                required
                rows={3}
                placeholder="Опишите назначение платежа"
                className="w-full px-4 py-3 bg-input-background border border-input rounded-lg focus:outline-none focus:ring-2 focus:ring-ring resize-none"
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              />
            </div>

            <div className="bg-secondary/30 p-4 rounded-lg flex items-start gap-3">
              <Shield className="w-5 h-5 text-accent flex-shrink-0 mt-0.5" />
              <div className="text-sm">
                <p className="mb-1">Защита платежа</p>
                <p className="text-muted-foreground text-xs">
                  Каждая транзакция проходит ML-проверку на предмет мошенничества. Среднее время проверки {'<'}5мс.
                </p>
              </div>
            </div>

            <button
              type="submit"
              className="w-full px-6 py-3 bg-accent text-accent-foreground rounded-lg hover:bg-accent/90 transition-colors flex items-center justify-center gap-2"
            >
              <Send className="w-5 h-5" />
              Отправить платёж
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
              <h2 className="text-2xl mb-2">Обработка платежа</h2>
              <p className="text-muted-foreground">Выполняется ML-проверка безопасности...</p>
            </div>

            <div className="max-w-md mx-auto space-y-3">
              <div className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg">
                <span className="text-sm">Проверка получателя</span>
                <Loader2 className="w-4 h-4 animate-spin text-accent" />
              </div>
              <div className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg">
                <span className="text-sm">Анализ рисков</span>
                <Loader2 className="w-4 h-4 animate-spin text-accent" />
              </div>
              <div className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg">
                <span className="text-sm">Обработка в Stripe</span>
                <Loader2 className="w-4 h-4 animate-spin text-accent" />
              </div>
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
              <h2 className="text-2xl mb-2">
                {result.success ? "Платёж успешно отправлен" : "Платёж отклонён"}
              </h2>
              <p className="text-muted-foreground">
                {result.success 
                  ? "Транзакция прошла все проверки безопасности" 
                  : "Транзакция не прошла проверку безопасности"}
              </p>
            </div>

            <div className="max-w-md mx-auto space-y-4">
              <div className="bg-secondary/30 p-4 rounded-lg space-y-3">
                <div className="flex justify-between items-center">
                  <span className="text-sm text-muted-foreground">ID транзакции</span>
                  <code className="text-sm font-mono">{result.transactionId}</code>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm text-muted-foreground">Сумма</span>
                  <span className="text-sm">₽ {parseFloat(formData.amount).toLocaleString('ru-RU', { minimumFractionDigits: 2 })}</span>
                </div>
                <div className="flex justify-between items-center">
                  <span className="text-sm text-muted-foreground">Получатель</span>
                  <span className="text-sm">{formData.recipient}</span>
                </div>
              </div>

              <div className="border-t border-border pt-4">
                <div className="flex items-center gap-2 mb-3">
                  <Shield className="w-5 h-5 text-accent" />
                  <h4>ML-проверка</h4>
                </div>

                <div className="space-y-2">
                  <div className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg">
                    <span className="text-sm">Время проверки</span>
                    <span className="text-accent">{result.mlCheckTime.toFixed(2)}ms</span>
                  </div>
                  <div className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg">
                    <span className="text-sm">Fraud Score</span>
                    <div className="flex items-center gap-2">
                      <span className="text-sm">{result.fraudScore.toFixed(2)}</span>
                      {result.fraudScore < 0.3 ? (
                        <span className="px-2 py-1 rounded-md bg-success/10 text-success text-xs">Низкий</span>
                      ) : result.fraudScore < 0.6 ? (
                        <span className="px-2 py-1 rounded-md bg-warning/10 text-warning text-xs">Средний</span>
                      ) : (
                        <span className="px-2 py-1 rounded-md bg-destructive/10 text-destructive text-xs">Высокий</span>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center justify-between p-3 bg-secondary/30 rounded-lg">
                    <span className="text-sm">Stripe Status</span>
                    <code className="text-xs">{result.stripeStatus}</code>
                  </div>
                </div>
              </div>

              {result.riskWarning && (
                <div className="flex items-start gap-3 p-4 bg-destructive/10 border border-destructive/20 rounded-lg">
                  <AlertTriangle className="w-5 h-5 text-destructive flex-shrink-0 mt-0.5" />
                  <div>
                    <p className="text-sm text-destructive mb-1">Предупреждение безопасности</p>
                    <p className="text-xs text-muted-foreground">{result.riskWarning}</p>
                  </div>
                </div>
              )}
            </div>

            <div className="flex gap-3 pt-4">
              {result.success && (
                <button
                  onClick={navigateToTransactions}
                  className="flex-1 px-6 py-3 bg-primary text-primary-foreground rounded-lg hover:bg-primary/90 transition-colors"
                >
                  Посмотреть транзакции
                </button>
              )}
              <button
                onClick={resetForm}
                className="flex-1 px-6 py-3 bg-secondary text-secondary-foreground rounded-lg hover:bg-secondary/80 transition-colors"
              >
                {result.success ? "Создать новый платёж" : "Попробовать снова"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
