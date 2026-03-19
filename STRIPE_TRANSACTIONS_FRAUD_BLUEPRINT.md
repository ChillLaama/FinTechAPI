# FinTechAPI Plan 3.0: Stripe-First, Non-Custodial, Fraud, Frontend

Дата: 2026-03-19
Статус: основной рабочий план
Модель: non-custodial, Stripe-first
Горизонт: 8 спринтов по 1 неделе

## 1) Описание проекта

FinTechAPI не хранит средства пользователей и не ведет собственный денежный кошелек клиента.

Целевая модель проекта:

- Stripe является источником движения средств и источником отображаемого баланса платформы;
- FinTechAPI является слоем orchestration, antifraud, audit, reconciliation и продуктовой логики;
- внутренние Transaction и Payment нужны не для хранения денег, а для бизнес-учета, трассировки состояний, расследований и UI;
- отображаемый баланс на фронте должен приходить со Stripe platform side, а не из внутреннего account balance.

Это означает, что проект строится как платежная платформа над Stripe, а не как собственный wallet/custody продукт.

---

## 2) Принципы архитектуры

### 2.1 Что является source of truth

Stripe:

- движение денег;
- provider payment lifecycle;
- platform balance;
- settlement-related состояния.

FinTechAPI:

- внутренняя business transaction;
- fraud decision;
- audit trail;
- review queue;
- reconciliation state;
- пользовательский и операционный UX.

### 2.2 Что мы не делаем

- не храним клиентские средства;
- не ведем собственный wallet balance как денежное обязательство;
- не показываем внутренний account balance как официальный денежный остаток платформы.

### 2.3 Что мы делаем

- создаем и сопровождаем платежный flow через Stripe;
- принимаем antifraud-решение до и после платежных событий;
- храним у себя бизнес-события и статусы;
- синхронизируем состояния через webhook и reconciliation;
- показываем Stripe balance и производные аналитические показатели в UI.

---

## 3) Текущее состояние проекта

### 3.1 Что уже есть

- Backend на .NET 9 со слоями API / Application / Domain / Infrastructure.
- Stripe PaymentIntent creation.
- Stripe webhook обработка.
- Внутренние Transaction со статусами и типами.
- Frontend с базовыми экранами Dashboard, Transactions, Create Payment, Auth.
- Базовые backend тесты.

### 3.2 Что нужно переосмыслить под новую модель

- текущий `Account.balance` не должен оставаться главным источником баланса;
- Dashboard не должен рассчитывать основной баланс как сумму внутренних account balances;
- account model должна быть переосмыслена как logical account / payment profile / reporting bucket, а не денежный кошелек;
- transaction service не должен трактовать внутренний баланс как источник реального движения средств.

### 3.3 Что уже становится legacy

Legacy-зона:

- внутренний `Account.balance` в его текущем виде;
- логика обновления account balance при смене статусов transaction;
- UX, где внутренний баланс выглядит как реальные деньги клиента.

---

## 4) Целевая доменная модель

### 4.1 Transaction

Назначение: внутреннее бизнес-событие, отражающее намерение, статус и контекст операции.

Transaction хранит:

- `id`
- `userId`
- `accountId` или logical profile id
- `amount`
- `currency`
- `type`
- `status`
- `category`
- `description`
- `transactionDate`
- `createdAt`
- `updatedAt`
- `provider` = Stripe
- `providerReference` = Stripe PaymentIntent ID / related object

Важно:

- Transaction не является местом хранения денег;
- Transaction является внутренним слоем бизнес-учета.

### 4.2 Payment

Назначение: техническая сущность, связывающая внутреннюю операцию со Stripe.

Payment хранит:

- внутренний `paymentId`
- `transactionId`
- `stripePaymentIntentId`
- `status`
- `lastWebhookEvent`
- `lastStripeEventId`
- `amountMinorUnits`
- `currency`
- `createdAt`
- `updatedAt`

### 4.3 FraudEvaluation

Назначение: хранение результата автоматической антифрод-проверки.

FraudEvaluation хранит:

- `transactionId`
- `paymentId` optional
- `fraudScore`
- `riskLevel`
- `decision`
- `reasons[]`
- `rulesTriggered[]`
- `modelVersion` или `rulesVersion`
- `evaluatedAt`
- `correlationId`

### 4.4 FraudCase

Назначение: ручная очередь проверки сомнительных кейсов.

FraudCase хранит:

- `caseId`
- `transactionId`
- `paymentId`
- `status` (`open`, `in_review`, `approved`, `rejected`, `expired`)
- `assignee`
- `decisionReason`
- `createdAt`
- `updatedAt`

### 4.5 Account

Целевая роль Account:

- logical grouping entity;
- пользовательский контекст операций;
- reporting bucket;
- сегмент для фильтрации и представления операций.

Account не должен означать:

- wallet balance;
- money storage;
- обязательство платформы перед пользователем.

---

## 5) Баланс: новая модель

### 5.1 Что показываем пользователю

На фронте должен показываться:

- Stripe platform balance;
- available / pending balance, если это соответствует продукту и правам доступа;
- derived analytics: оборот, успешные платежи, отклоненные операции, тренды.

### 5.2 Что не показываем как основной баланс

Не показываем как главный баланс:

- сумму внутренних `Account.balance`;
- внутренние derived-числа как будто это реальные деньги в системе.

### 5.3 Что можно оставить как аналитику

Можно оставить:

- turnover by period;
- gross processed volume;
- succeeded payment volume;
- pending payment volume;
- fraud-blocked amount;
- payout/settlement summary.

---

## 6) Целевой платежный pipeline

### 6.1 Create Payment

1. Пользователь заполняет форму платежа.
2. Frontend отправляет запрос на создание внутренней Transaction со статусом `Pending`.
3. Backend выполняет Fraud pre-check.
4. Если `decision = block`:

- Stripe intent не создается;
- Transaction помечается как `Failed`;
- сохраняется FraudEvaluation;
- пользователь получает объяснимую причину отказа.

5. Если `decision = review`:

- создается FraudCase;
- Transaction остается в промежуточном статусе review/pending;
- пользователь получает статус ожидания решения.

6. Если `decision = allow`:

- создается Stripe PaymentIntent с idempotency key;
- создается Payment document;
- Transaction связывается с provider reference.

### 6.2 Webhook Reconciliation

1. Stripe присылает webhook.
2. Backend валидирует подпись.
3. По `stripePaymentIntentId` находит внутренний Payment.
4. Применяет state transition guard.
5. Обновляет Payment status.
6. Обновляет внутренний Transaction status по централизованному mapper.
7. Записывает audit event.

### 6.3 Reconciliation

Нужен отдельный процесс reconciliation:

- поиск потерянных webhook;
- проверка расхождений между Stripe и внутренними статусами;
- повторная синхронизация;
- alerting по рассинхрону.

---

## 7) Fraud Detection система

### 7.1 Цель

Собственная fraud system нужна даже в Stripe-first модели, потому что это ваша логика риска, explainability и операционного контроля.

### 7.2 MVP

Rule Engine:

- velocity rules;
- atypical amount rules;
- repeated failure rules;
- new recipient rules;
- geo/device anomaly rules;
- high-risk pattern rules.

Decision model:

- `allow`
- `review`
- `block`

### 7.3 Phase 2

- ML scoring;
- calibration thresholds;
- analyst feedback loop;
- false-positive optimization.

### 7.4 Что обязательно хранить

- fraud score;
- reasons;
- triggered rules;
- decision;
- rule version;
- analyst decision trail.

---

## 8) Frontend: новая целевая модель экранов

### 8.1 Пользовательские экраны

1. Login
2. Register
3. Forgot Password
4. Reset Password
5. Verify Email
6. Dashboard
7. Transactions List
8. Transaction Details
9. Create Payment
10. Payment Details
11. Accounts / Profiles Management
12. Profile
13. Settings
14. Notifications Center

### 8.2 Fraud operations экраны

15. Fraud Monitoring Dashboard
16. Fraud Cases Queue
17. Fraud Case Details
18. Fraud Rules Management
19. Fraud Events Journal
20. Roles & Access

---

## 9) Что должен показывать каждый ключевой экран

### 9.1 Dashboard

Должен показывать:

- Stripe platform balance;
- available / pending summary;
- processed volume;
- fraud KPIs;
- latest transactions;
- latest payment incidents;
- API and webhook health summary.

Не должен показывать:

- внутренний account balance как деньги пользователя.

### 9.2 Transactions List

Должен показывать:

- business transaction status;
- provider payment status;
- fraud decision / risk level;
- amount, currency, date, recipient, category;
- связь с PaymentIntent.

### 9.3 Transaction Details

Должен показывать:

- transaction timeline;
- provider status timeline;
- fraud score и reasons;
- correlation id;
- webhook events;
- audit trail.

### 9.4 Create Payment

Должен показывать:

- форму платежа;
- risk pre-check state;
- allow/review/block result;
- idempotency reference;
- provider payment creation result.

### 9.5 Payment Details

Должен показывать:

- Stripe lifecycle;
- current provider state;
- retries / failures;
- reconciliation state;
- related internal transaction.

### 9.6 Accounts / Profiles

Новая трактовка:

- не кошелек;
- не место хранения денег;
- а logical container для операций, фильтров, сегментов и reporting.

---

## 10) Изменения в backend плане

### 10.1 Что нужно изменить в коде

1. Отвязать денежную семантику от `Account.balance`.
2. Перевести account balance в legacy/derived режим.
3. Ввести Stripe-backed balance retrieval endpoint.
4. Ввести explicit mapping Stripe status -> internal transaction status.
5. Добавить reconciliation job.
6. Добавить fraud storage и review queue.

### 10.2 Что нужно изменить в API

Нужны endpoints:

- `GET /api/platform/balance`
- `GET /api/payments/{paymentId}`
- `GET /api/transactions/{id}`
- `GET /api/fraud/evaluations/{transactionId}`
- `GET /api/fraud/cases`
- `POST /api/fraud/cases/{id}/approve`
- `POST /api/fraud/cases/{id}/reject`

### 10.3 Что считать legacy

Legacy:

- внутренняя логика пересчета real balance через transaction service;
- dashboard total balance как сумма внутренних аккаунтов;
- account semantics как internal wallet.

---

## 11) План по спринтам

### Sprint 1: Non-Custodial Domain Alignment

- Зафиксировать Stripe-first архитектуру.
- Утвердить, что Stripe balance является source of truth.
- Пометить `Account.balance` как legacy.
- Утвердить новый status/event contract.

Результат:

- новая архитектурная база без wallet semantics.

### Sprint 2: Platform Balance and Provider Truth

- Добавить backend endpoint для Stripe platform balance.
- Подключить Dashboard к Stripe-backed balance.
- Перевести UI-терминологию с wallet/balance на platform balance/settlement summary.

Результат:

- баланс в UI приходит из Stripe.

### Sprint 3: Fraud MVP Core

- Реализовать Rule Engine MVP.
- Добавить FraudEvaluation storage.
- Интегрировать fraud decision в create payment pipeline.

Результат:

- antifraud встроен до Stripe intent creation.

### Sprint 4: Review Queue Backend

- Реализовать FraudCase lifecycle.
- Добавить audit trail.
- Реализовать analyst actions.

Результат:

- ручной risk review workflow.

### Sprint 5: User Payment UX

- Переписать Create Payment под provider-first модель.
- Улучшить Transaction Details и Payment Details.
- Показать provider state + business state + fraud state отдельно.

Результат:

- прозрачный пользовательский UX.

### Sprint 6: Fraud Operations Frontend

- Cases Queue UI.
- Case Details UI.
- Fraud Monitoring Dashboard.

Результат:

- operational risk UI готов.

### Sprint 7: Reconciliation and Reliability

- Реализовать reconciliation process.
- Добавить integration tests и e2e.
- Проверить webhook replay/order scenarios.

Результат:

- устойчивая и проверяемая provider sync модель.

### Sprint 8: Release Readiness

- Security hardening.
- Observability and alerts.
- Release dry-run.

Результат:

- готовность к rollout.

---

## 12) Приоритеты

### P0

- Stripe-backed balance endpoint and UI.
- Legacy removal from wallet semantics.
- Fraud pre-check in create payment.
- Review queue backend.
- Status mapping and webhook reconciliation.

### P1

- Fraud analyst frontend.
- Payment Details / Transaction Details full timeline.
- Profile / Settings / Auth recovery completion.

### P2

- ML scoring.
- Rule management UI advanced flows.
- advanced analytics and playbooks.

---

## 13) KPI

Продуктовые:

- баланс в UI всегда соответствует Stripe source;
- успешный create payment flow >= 97%;
- снижение support tickets по статусам платежей.

Операционные:

- p95 fraud check <= 100 ms;
- failed reconciliation < 0.5%;
- mean time to decision по review кейсам <= 15 мин.

Бизнес:

- снижение fraud loss rate;
- снижение chargeback rate.

---

## 14) Definition of Done

Задача считается завершенной, если:

1. нет wallet/custody ambiguity в коде и UI;
2. provider truth и business truth четко разделены;
3. есть тесты требуемого уровня;
4. есть logging, metrics, audit trail;
5. документация и UX обновлены;
6. нет регрессии в критических flows.

---

## 15) Риски

Риск: часть текущего backend все еще пересчитывает внутренний баланс.

- Мера: перевести это в legacy и вынести в migration backlog.

Риск: фронт продолжит показывать derived internal balances как реальные деньги.

- Мера: заменить dashboard model на Stripe-backed data source.

Риск: смешение business status и provider status.

- Мера: два явных статуса в API и UI, плюс централизованный mapper.

Риск: false positive на старте fraud rules.

- Мера: shadow mode и conservative thresholds.

---

## 16) Следующий артефакт

Следующий документ после этого плана:

- Sprint Backlog
- Epic -> Feature -> Task
- оценки (SP)
- зависимости
- migration plan по отказу от internal balance semantics
