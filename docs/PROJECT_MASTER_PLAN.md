# FinTechAPI: Master Plan

Дата создания: 2026-03-19
Последнее обновление: 2026-03-31
Статус: рабочий документ

## 0. Архитектурные решения

- FinTechAPI — non-custodial модель. Платформа не хранит средства пользователей.
- Stripe — единственный источник движения средств и platform balance.
- `Account.balance` — legacy-поле, не source of truth.
- ML.NET + ONNX Runtime — гибридная fraud detection (rules + ML).

---

## 1. Архитектура

- **Backend:** .NET 9, слоистая архитектура (API / Application / Domain / Infrastructure).
- **ML Trainer:** .NET 9 Console App (`FinTechAPI.MlTrainer`) — ML.NET FastTree + RandomizedPCA → ONNX.
- **Хранилище и auth:** Firebase + Firestore.
- **Платежи:** Stripe (payment intents + webhook + reconciliation).
- **Frontend:** React + TypeScript + Vite + Tailwind/Radix.
- **Тесты:** xUnit + Moq (99 unit + 18 integration).

### Структура репозитория

- `src/FinTechAPI.API` — контроллеры, DI, auth, middleware.
- `src/FinTechAPI.Application` — DTO, интерфейсы, mappings, exceptions.
- `src/FinTechAPI.Domain` — доменные модели и enum.
- `src/FinTechAPI.Infrastructure` — сервисы, Firestore, Stripe, ONNX Runtime inference.
- `src/FinTechAPI.MlTrainer` — ML pipeline (Kaggle CSV → train → ONNX export).
- `src/FinTechAPI.Client` — production-ready веб-клиент.
- `src/Design` — UI-стенд/прототип (частично расходится с Client).
- `tests/FinTechAPI.Tests` — unit тесты.
- `tests/FinTechAPI.IntegrationTests` — integration тесты.

---

## 2. Реализованный функционал

### 2.1 Backend API

| Модуль | Функционал | Статус |
|--------|-----------|--------|
| **Auth** | Register, Login, Forgot/Reset Password, Verify Email, Send Verification Email | ✅ |
| **Accounts** | CRUD (list, get, create, update, delete) с ownership check | ✅ |
| **Transactions** | CRUD + status transitions + фильтрация по accountId | ✅ |
| **Payments** | Payment intent (идемпотентность), webhook, status timeline, manual reconcile | ✅ |
| **Payouts** | Создание, список, reconciliation | ✅ |
| **Fraud** | FraudRuleEngine (5 deterministic rules + ML scoring), fraud cases CRUD, evaluate/approve/reject/escalate/assign | ✅ |
| **ML Fraud** | IFraudMlService, OnnxFraudScoringService (singleton InferenceSession), feature flag, graceful degradation | ✅ |
| **Profile** | GET/PATCH /api/users/me/profile (Firestore + Firebase Auth sync) | ✅ |
| **Settings** | GET/PATCH /api/users/me/settings + admin policy locks (15+ settings) | ✅ |
| **Reports** | Отчёт по типу транзакции | ✅ |
| **Users/Roles** | Admin: list/get/delete/disable users, set/remove roles via custom claims | ✅ |
| **Platform** | Balance, summary, API latency (via Stripe) | ✅ |
| **Reconciliation** | Background service (каждые 5 мин), проверка Stripe на divergence | ✅ |
| **Dev tools** | Topup, set-balance, seed, clear, quick-register | ✅ |

### 2.2 Frontend Client (всё подключено к реальному API)

| Экран | Функционал | Статус |
|-------|-----------|--------|
| **Login / Register** | Firebase auth, JWT, protected routes | ✅ |
| **Forgot / Reset / Verify** | Реальные backend flows через Firebase REST API | ✅ |
| **Dashboard** | Баланс (Stripe), транзакции, графики (Recharts), latency | ✅ |
| **Transactions** | Таблица, multi-field фильтры, поиск, detail modal | ✅ |
| **Create Payment** | Транзакция + payment intent + status update | ✅ |
| **Payment Details** | Provider timeline, webhook event, correlation ID, reconcile | ✅ |
| **Payouts** | Создание с idempotency, список, reconcile | ✅ |
| **Profile** | GET/PATCH через API, edit/save/cancel flow | ✅ |
| **Settings** | GET/PATCH через API, policy locks, admin section | ✅ |
| **Accounts** | Полный CRUD (10 типов, выбор валюты) | ✅ |
| **Fraud Dashboard** | KPI (total/open/review/approved/rejected), avg score, risk counts | ✅ |
| **Fraud Cases** | Список с пагинацией, status filter, rules triggered | ✅ |
| **Fraud Case Details** | Overview, triggered rules, evaluation, approve/reject/escalate/assign | ✅ |
| **Help** | Статический FAQ + email (заглушка) | ⚠️ |

### 2.3 ML.NET Pipeline

| Компонент | Статус |
|-----------|--------|
| FinTechAPI.MlTrainer (Console App, ML.NET 3.0.1) | ✅ Код готов |
| Feature engineering (OneHot, computed columns, normalize) | ✅ |
| FastTree (supervised, 100 trees/20 leaves) | ✅ |
| RandomizedPCA (unsupervised anomaly, rank=5) | ✅ |
| ONNX export + evaluation report | ✅ |
| OnnxFraudScoringService (Runtime 1.17.3, singleton) | ✅ |
| Hybrid integration: ML score → 6-е правило в FraudRuleEngine | ✅ |
| Feature flag (FraudMl:Enabled) | ✅ |
| **Kaggle CSV скачан и модель натренирована** | ❌ Ожидает |
| **ONNX файл скопирован в Infrastructure/ML/Models/** | ❌ Ожидает |

### 2.4 Тестирование

| Набор | Кол-во | Статус |
|-------|--------|--------|
| Unit тесты (xUnit + Moq) | 99 | ✅ Все проходят |
| Integration тесты (WebApplicationFactory) | 18 | ✅ Все проходят |
| E2E тесты (Playwright/Cypress) | 0 | ❌ Не реализовано |
| Frontend тесты | 0 | ❌ Не реализовано |

---

## 3. Открытые задачи

### P0 (критично для thesis)

| # | Задача | Детали |
|---|--------|--------|
| 1 | **Тренировка ML модели** | Скачать Kaggle CSV → `dotnet run --project src/FinTechAPI.MlTrainer` → скопировать ONNX в Infrastructure |
| 2 | **Fraud UI — ML поля** | Отобразить `mlAnomalyScore`, `mlModelVersion` в FraudDashboard, FraudCases, FraudCaseDetails |

### P1 (высокий приоритет)

| # | Задача | Детали |
|---|--------|--------|
| 3 | **Унификация ошибок API** | Внедрить RFC 7807 Problem Details middleware. Сейчас контроллеры возвращают разные форматы |
| 4 | **Design-стенд синхронизация** | Design/* расходится с Client — settings/profile только моки. Решение: Design = только UI showcase, Client = production |

### P2 (средний)

| # | Задача | Детали |
|---|--------|--------|
| 5 | **E2E тесты** | Playwright для критических flows (auth, payment, fraud) |
| 6 | **Help page** | Заменить статический FAQ на реальный контент или убрать |
| 7 | **i18n** | Нет фреймворка локализации, только hardcoded en-US |
| 8 | **Выделенная админ-панель** | Admin-функции встроены в Settings, нет отдельного UI |

---

## 4. Definition of Done

- Реализовано по спецификации.
- Обработка edge-cases и ошибок.
- Тесты (unit/integration — по уровню задачи).
- Нет регрессии (99 unit + 18 integration pass).

---

## 5. Правила ведения этого файла

- Любое изменение scope фиксируем с датой.
- После завершения задачи: перемещаем из §3 в §2.
- Устаревшие разделы удаляем, не накапливаем.
- Не удаляем исторические решения, а переносим в раздел "Архив решений".

---

## 11. Блокеры и риски (обновлять регулярно)

- [ ] Расхождение между Design и основным Client.
- [ ] Недостаточная покрытость e2e.
- [ ] Возможные задержки из-за auth/security интеграций.
- [ ] Риск изменения приоритетов без фиксации в документе.

---

## 12. Следующий шаг (прямо сейчас)

На следующей итерации из этого файла делаем:

1. финальный список эпиков;
2. задачи уровня sprint-ready;
3. оценку трудозатрат (S/M/L или story points);
4. матрицу зависимостей.
