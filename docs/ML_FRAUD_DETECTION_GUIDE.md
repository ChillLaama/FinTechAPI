# ML Fraud Detection — Руководство

## Архитектура

```
CreatePaymentIntent (HTTP POST)
        │
        ▼
  PaymentService
        │
        ▼
  FraudRuleEngine.EvaluateAsync()
        │
        ├── Rule 1: Velocity (≤5 txn / 10 мин)
        ├── Rule 2: Amount anomaly (выше среднего × 3)
        ├── Rule 3: Repeated failures (≥3 за 30 мин)
        ├── Rule 4: High amount (>$5K / >$10K)
        └── Rule 5: ML FastTree ◄── MlNetFraudScoringService
                                        │
                                    fraud_fasttree.zip
                                    (ML.NET PredictionEngine)
```

Все 5 правил возвращают баллы, которые суммируются (cap = 100):

| Общий балл | Уровень риска | Решение |
|------------|---------------|---------|
| < 40       | Low / Medium  | **Allow** — платёж проходит |
| 40–74      | Medium / High | **Review** — создаётся FraudCase |
| ≥ 75       | Critical      | **Block** → shadow mode → **Review** |

ML-модуль добавляет до 30 баллов:

| ML Probability | Баллы | Тег |
|----------------|-------|-----|
| ≥ 0.8          | +30   | `ml_high_risk` |
| ≥ 0.6          | +20   | `ml_medium_risk` |
| ≥ 0.4          | +10   | `ml_low_risk` |
| < 0.4          | 0     | — |

---

## Конфигурация

`appsettings.json`:
```json
{
  "FraudMl": {
    "ModelPath": "ML/Models/fraud_fasttree.zip",
    "Enabled": true
  }
}
```

- `Enabled: false` — ML-скоринг отключён, остаётся только rule-based движок
- `ModelPath` — относительный (от `AppContext.BaseDirectory`) или абсолютный путь к `.zip`

---

## Быстрый старт: проверка ML

### 1. Убедиться, что модель на месте

```
src/FinTechAPI.Infrastructure/ML/Models/fraud_fasttree.zip  (~80 KB)
```

Файл автоматически копируется в `bin/` при сборке (настроено в `.csproj`).

### 2. Запустить API

```powershell
dotnet run --project src/FinTechAPI.API
```

В логах при старте должно быть:
```
Fraud model loaded successfully. Path=...\ML\Models\fraud_fasttree.zip, Version=fasttree-v20260331
```

Если вместо этого `Fraud model not found` — модель не скопировалась в `bin/`.

### 3. Создать тестовый платёж

**Swagger UI:** http://localhost:5000 (в Development окружении)

Или через `curl` / HTTP-клиент:

```http
POST http://localhost:5000/api/payments/intents
Content-Type: application/json
Authorization: Bearer <JWT_TOKEN>
Idempotency-Key: test-ml-001

{
  "amount": 150.00,
  "currency": "usd",
  "description": "ML test - normal payment"
}
```

Ответ:
```json
{
  "paymentId": "...",
  "status": "...",
  "fraudDecision": "Allow",
  "fraudScore": 0,
  "fraudEvaluationId": "..."
}
```

### 4. Посмотреть детали fraud-оценки

```http
GET http://localhost:5000/api/fraud-cases/{caseId}/evaluation
Authorization: Bearer <ADMIN_JWT_TOKEN>
```

В ответе будут ML-поля:
```json
{
  "fraudScore": 10,
  "riskLevel": "Low",
  "decision": "Allow",
  "rulesTriggered": ["ml_low_risk"],
  "mlAnomalyScore": 0.4231,
  "mlModelVersion": "fasttree-v20260331"
}
```

### 5. Спровоцировать высокий ML-скор

Отправить крупный платёж (>$5,000) — это активирует и Rule 4 (high amount), и модель получит высокий `Amount`:

```http
POST http://localhost:5000/api/payments/intents
Content-Type: application/json
Authorization: Bearer <JWT_TOKEN>
Idempotency-Key: test-ml-high-001

{
  "amount": 15000.00,
  "currency": "usd",
  "description": "ML test - high risk payment"
}
```

Ожидаемый результат: `fraudDecision: "Review"`, `fraudScore ≥ 40`, в `rulesTriggered` будет `ml_high_risk` или `ml_medium_risk`.

---

## Юнит-тесты

```powershell
# Все тесты (99 unit + 18 integration)
dotnet test FinTechAPI.sln

# Только ML-сервис
dotnet test tests/FinTechAPI.Tests --filter "FullyQualifiedName~MlNetFraudScoringService"

# Только fraud rule engine
dotnet test tests/FinTechAPI.Tests --filter "FullyQualifiedName~FraudRuleEngine"
```

---

## Переобучение модели

```powershell
# 1. Положить Fraud.csv (~494 MB) в data/
#    Скачать: https://www.kaggle.com/datasets/chitwanmanchanda/fraudulent-transactions-data

# 2. Запустить тренер
dotnet run --project src/FinTechAPI.MlTrainer

# 3. Скопировать модель
Copy-Item models/fraud_fasttree.zip src/FinTechAPI.Infrastructure/ML/Models/fraud_fasttree.zip -Force

# 4. Пересобрать API
dotnet build src/FinTechAPI.API
```

Тренер выводит метрики:
```
  Accuracy:    0.9997
  AUC-ROC:     0.9976
  F1 Score:    0.8844
  Precision:   0.9468
  Recall:      0.8298
```

---

## Файловая структура ML

```
src/
├── FinTechAPI.MlTrainer/           # Console App для обучения
│   ├── Program.cs                  # Пайплайн: загрузка CSV → features → FastTree + PCA
│   ├── FraudTransactionData.cs     # Схема CSV-данных
│   └── models/                     # Выход тренера (не в git)
│       ├── fraud_fasttree.zip
│       ├── fraud_pca.zip
│       └── evaluation_report.txt
│
├── FinTechAPI.Application/
│   ├── Interfaces/IFraudMlService.cs       # Контракт: ScoreAsync + IsModelLoaded
│   └── DTOs/FraudMlDtos.cs                 # FraudMlFeaturesDto, FraudMlScoreDto
│
├── FinTechAPI.Infrastructure/
│   ├── ML/
│   │   ├── FraudMlSettings.cs              # Конфиг: ModelPath, Enabled
│   │   └── Models/fraud_fasttree.zip       # Продакшен-модель
│   └── Services/
│       ├── OnnxFraudScoringService.cs      # MlNetFraudScoringService (PredictionEngine)
│       └── FraudRuleEngine.cs              # 5 правил, вызывает IFraudMlService
```

## Graceful Degradation

Если модель не найдена / повреждена / выключена — ML-скоринг возвращает `{ anomalyScore: 0, isAnomaly: false }`, остальные 4 правила работают без изменений.
