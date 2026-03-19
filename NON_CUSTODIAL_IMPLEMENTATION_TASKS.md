# FinTechAPI Non-Custodial Implementation Tasks

Дата: 2026-03-19
Статус: рабочий backlog в Markdown
Основа: Stripe-first, non-custodial модель

## 1. Цель

Этот файл заменяет Jira backlog для текущей инициативы.

Используем его для:

- декомпозиции задач на backend и frontend;
- фиксации объема изменений;
- определения порядка реализации;
- отслеживания migration от internal balance semantics к Stripe-backed model.

---

## 2. Главный принцип

- Stripe = source of funds movement and displayed balance.
- FinTechAPI = source of business truth, fraud decisions, audit, reconciliation.
- Internal `Account.balance` больше не является основным денежным источником истины.

---

## 3. Backend Tasks

## BE-01. Зафиксировать non-custodial архитектуру в коде и документации

Приоритет: P0
Статус: Completed

Задачи:

- [x] Зафиксировать в документации, что Stripe является источником баланса и движения средств.
- [x] Зафиксировать, что внутренние `Transaction` и `Payment` не являются хранилищем средств.
- [x] Пометить `Account.balance` как legacy-semantics в техническом плане.

Файлы:

- [STRIPE_TRANSACTIONS_FRAUD_BLUEPRINT.md](c:/Users/Daniel/RiderProjects/FinTechAPI/STRIPE_TRANSACTIONS_FRAUD_BLUEPRINT.md)
- [PROJECT_MASTER_PLAN.md](c:/Users/Daniel/RiderProjects/FinTechAPI/PROJECT_MASTER_PLAN.md)

---

## BE-02. Убрать внутреннюю денежную семантику из TransactionService

Приоритет: P0
Статус: Completed

Задачи:

- [x] Удалить изменение `Account.balance` из `CreateTransactionAsync`.
- [x] Удалить изменение `Account.balance` из `UpdateTransactionAsync`.
- [x] Удалить изменение `Account.balance` из `UpdateTransactionStatusAsync`.
- [x] Удалить изменение `Account.balance` из `DeleteTransactionAsync`.
- [x] Удалить или перевести в legacy helper `GetBalanceDelta`.
- [x] Обновить комментарии и поведение сервиса под business-state only модель.

Файлы:

- [src/FinTechAPI.Infrastructure/Services/TransactionService.cs](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Infrastructure/Services/TransactionService.cs)

Definition of Done:

- [x] Ни одна операция над transaction больше не меняет внутренний balance.
- [x] Сервис работает только со status lifecycle и metadata.

---

## BE-03. Перевести Account в logical/reporting сущность

Приоритет: P0
Статус: In Progress

Задачи:

- [x] Перестать использовать `Account.balance` как основной денежный атрибут.
- [x] Определить новую семантику `Account`: profile / reporting bucket / logical grouping.
- [ ] Решить, оставляем ли поле `balance` физически как legacy на переходный период.
- [x] Подготовить migration note по отказу от wallet semantics.

Файлы:

- [src/FinTechAPI.Infrastructure/Firebase/Documents/AccountDocument.cs](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Infrastructure/Firebase/Documents/AccountDocument.cs)
- [src/FinTechAPI.Infrastructure/Services/AccountService.cs](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Infrastructure/Services/AccountService.cs)
- [src/FinTechAPI.Application/DTOs/AccountDto.cs](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Application/DTOs/AccountDto.cs)

Definition of Done:

- [ ] В модели и сервисах account больше не трактуется как внутренний кошелек.

---

## BE-04. Добавить Stripe-backed balance endpoint

Приоритет: P0
Статус: Completed

Задачи:

- [x] Добавить endpoint `GET /api/platform/balance`.
- [x] Определить DTO ответа: `available`, `pending`, `currency`, `source`, `syncedAt`.
- [x] Реализовать сервис получения Stripe platform balance.
- [x] Обработать ошибки Stripe и деградацию UI-friendly сообщениями.
- [x] Добавить логирование и correlation id.

Новые/изменяемые файлы:

- [src/FinTechAPI.API/Controllers](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.API/Controllers)
- [src/FinTechAPI.Application/DTOs](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Application/DTOs)
- [src/FinTechAPI.Application/Interfaces](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Application/Interfaces)
- [src/FinTechAPI.Infrastructure/Services](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Infrastructure/Services)

Definition of Done:

- [x] Баланс для UI можно получить только из Stripe-backed endpoint.

---

## BE-05. Добавить platform summary endpoint

Приоритет: P1
Статус: Not Started

Задачи:

- [ ] Добавить endpoint `GET /api/platform/summary`.
- [ ] Вернуть aggregated метрики: processed volume, successful payments, failed payments, pending review count, fraud blocked count.
- [ ] Согласовать, какие метрики считаются из Stripe, а какие из внутренних transactions/payments.

Definition of Done:

- [ ] Dashboard можно строить без обращения к внутреннему account balance.

---

## BE-06. Разделить provider status и business status

Приоритет: P0
Статус: Not Started

Задачи:

- [ ] Зафиксировать отдельные статусы для internal transaction и Stripe payment.
- [ ] Добавить централизованный mapper Stripe status -> internal transaction status.
- [ ] Убрать неоднозначность между `Transaction.status` и `Payment.status`.
- [ ] Убедиться, что frontend получает оба состояния явно.

Файлы:

- [src/FinTechAPI.Infrastructure/Services/PaymentService.cs](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Infrastructure/Services/PaymentService.cs)
- [src/FinTechAPI.Infrastructure/Services/TransactionService.cs](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Infrastructure/Services/TransactionService.cs)
- [src/FinTechAPI.Application/DTOs](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Application/DTOs)

Definition of Done:

- [ ] В API и UI provider status и business status не смешиваются.

---

## BE-07. Усилить webhook reconciliation

Приоритет: P0
Статус: In Progress

Что уже есть:

- идемпотентность обработки событий;
- state transition guard;
- логирование webhook lifecycle.

Задачи:

- [ ] Добавить явный reconciliation процесс для потерянных webhook.
- [ ] Добавить алерты по рассинхрону `Payment.status` и `Transaction.status`.
- [ ] Добавить endpoint/джоб для ручной повторной синхронизации.
- [ ] Документировать recovery сценарии.

Файлы:

- [src/FinTechAPI.Infrastructure/Services/PaymentService.cs](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Infrastructure/Services/PaymentService.cs)

Definition of Done:

- [ ] Есть способ восстановить консистентность между Stripe и внутренней системой.

---

## BE-08. Реализовать FraudEvaluation storage

Приоритет: P0
Статус: Not Started

Задачи:

- [ ] Создать модель `FraudEvaluation`.
- [ ] Сохранять `fraudScore`, `riskLevel`, `decision`, `reasons`, `rulesTriggered`, `rulesVersion`, `correlationId`.
- [ ] Связать evaluation с `transactionId` и `paymentId`.

Definition of Done:

- [ ] Каждое fraud-решение сохраняется и может быть показано в UI.

---

## BE-09. Реализовать Fraud Rule Engine MVP

Приоритет: P0
Статус: Not Started

Задачи:

- [ ] Реализовать velocity rules.
- [ ] Реализовать amount anomaly rules.
- [ ] Реализовать repeated failure rules.
- [ ] Реализовать new recipient rules.
- [ ] Реализовать geo/device anomaly rules.
- [ ] Вернуть `allow/review/block`.
- [ ] Добавить shadow-mode возможность для правил высокого риска.

Definition of Done:

- [ ] Create payment pipeline получает fraud decision до Stripe intent creation.

---

## BE-10. Реализовать FraudCase lifecycle и review queue backend

Приоритет: P0
Статус: Not Started

Задачи:

- [ ] Создать модель `FraudCase`.
- [ ] Реализовать статусы: `open`, `in_review`, `approved`, `rejected`, `expired`.
- [ ] Добавить API списка кейсов.
- [ ] Добавить approve/reject/escalate endpoints.
- [ ] Добавить audit trail analyst actions.

Definition of Done:

- [ ] Review queue полностью доступна backend-side.

---

## BE-11. Добавить audit/event log для критичных действий

Приоритет: P0
Статус: Not Started

Задачи:

- [ ] Логировать create payment flow.
- [ ] Логировать fraud decisions.
- [ ] Логировать webhook reconciliation.
- [ ] Логировать analyst approvals/rejections.
- [ ] Ввести correlation id через весь pipeline.

Definition of Done:

- [ ] Любой спорный кейс можно восстановить по audit trail.

---

## BE-12. Обновить тесты под новую non-custodial модель

Приоритет: P0
Статус: Not Started

Задачи:

- [ ] Тест: create transaction не меняет internal balance.
- [ ] Тест: update transaction status не меняет internal balance.
- [ ] Тест: delete transaction не меняет internal balance.
- [ ] Тест: Stripe-backed balance endpoint возвращает корректный DTO.
- [ ] Тест: webhook reconciliation обновляет state без internal wallet logic.
- [ ] Тест: fraud decision записывается и влияет на create payment flow.

Файлы:

- [tests/FinTechAPI.Tests](c:/Users/Daniel/RiderProjects/FinTechAPI/tests/FinTechAPI.Tests)

---

## 4. Frontend Tasks

## FE-01. Перевести Dashboard на Stripe-backed balance

Приоритет: P0
Статус: Not Started

Задачи:

- [ ] Убрать вычисление общего баланса из суммы `account.balance`.
- [ ] Подключить новый endpoint `GET /api/platform/balance`.
- [ ] Переименовать UI-блоки: `Platform balance`, `Available`, `Pending`, `Settlement summary`.
- [ ] Добавить `last synced` и fallback/error state.

Файлы:

- [src/FinTechAPI.Client/src/app/components/Dashboard.tsx](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Client/src/app/components/Dashboard.tsx)
- [src/FinTechAPI.Client/src/app/api/client.ts](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Client/src/app/api/client.ts)

Definition of Done:

- [ ] Dashboard больше не использует внутренние balances как главный источник денег.

---

## FE-02. Обновить dashboard metrics под новую модель

Приоритет: P1
Статус: Not Started

Задачи:

- [ ] Показать processed volume.
- [ ] Показать successful/failed payments.
- [ ] Показать pending review count.
- [ ] Показать fraud blocked volume/count.
- [ ] Добавить provider/API health summary.

---

## FE-03. Разделить provider status и business status в Transactions List

Приоритет: P0
Статус: Not Started

Задачи:

- [ ] Показать отдельно business transaction status.
- [ ] Показать отдельно provider payment status.
- [ ] Показать risk level / fraud decision.
- [ ] Добавить фильтры по provider status и fraud status.

Файлы:

- [src/FinTechAPI.Client/src/app/components/Transactions.tsx](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Client/src/app/components/Transactions.tsx)
- [src/FinTechAPI.Client/src/app/api/client.ts](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Client/src/app/api/client.ts)

Definition of Done:

- [ ] На списке больше нет смешения внутренних и Stripe-статусов.

---

## FE-04. Переписать Transaction Details под новую модель

Приоритет: P0
Статус: Not Started

Задачи:

- [ ] Добавить timeline бизнес-статуса.
- [ ] Добавить timeline provider-статуса.
- [ ] Добавить fraud score / reasons / risk level.
- [ ] Добавить webhook events.
- [ ] Добавить correlation id / technical details.

Definition of Done:

- [ ] Transaction Details объясняет, что произошло в бизнес-слое, провайдере и antifraud.

---

## FE-05. Переписать Create Payment под provider-first UX

Приоритет: P0
Статус: In Progress

Что уже есть:

- transaction creation;
- payment intent creation;
- idempotency key;
- update transaction status.

Что нужно сделать:

- [ ] Добавить явное отображение fraud pre-check состояния.
- [ ] Отдельно показать `allow / review / block`.
- [ ] При `review` показать понятный waiting state.
- [ ] При `block` показать причину и next steps.
- [ ] Убрать wallet-style формулировки про внутренний баланс счета.

Файлы:

- [src/FinTechAPI.Client/src/app/components/CreatePayment.tsx](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Client/src/app/components/CreatePayment.tsx)

Definition of Done:

- [ ] Пользователь видит разницу между бизнес-проверкой, fraud-проверкой и Stripe provider state.

---

## FE-06. Добавить Payment Details экран

Приоритет: P1
Статус: Not Started

Задачи:

- [ ] Отдельный экран/route для payment lifecycle.
- [ ] Показ `stripePaymentIntentId`, provider status, reconciliation state.
- [ ] Показ связи с transaction.
- [ ] Показ ошибок provider и retry context.

Definition of Done:

- [ ] Есть отдельная точка просмотра Stripe-side состояния платежа.

---

## FE-07. Пересобрать Accounts UI под logical profiles

Приоритет: P1
Статус: Not Started

Задачи:

- [ ] Убрать восприятие accounts как wallet/accounts with money.
- [ ] Переименовать/переформулировать UI при необходимости.
- [ ] Использовать accounts как grouping/reporting/payment profiles.
- [ ] Убрать отображение misleading balance values.

Definition of Done:

- [ ] Accounts UI не создает семантику хранения средств в FinTechAPI.

---

## FE-08. Добавить Fraud Monitoring Dashboard

Приоритет: P1
Статус: Not Started

Задачи:

- [ ] Показ KPI по fraud.
- [ ] Показ blocked/reviewed trends.
- [ ] Показ false positive review outcomes в будущем.
- [ ] Показ operational alerts.

---

## FE-09. Добавить Fraud Cases Queue UI

Приоритет: P0
Статус: Not Started

Задачи:

- [ ] Список кейсов review.
- [ ] Фильтры по status, risk level, assignee, age.
- [ ] Быстрые действия open/assign.

Definition of Done:

- [ ] Аналитик может открыть и обработать сомнительный кейс.

---

## FE-10. Добавить Fraud Case Details UI

Приоритет: P0
Статус: Not Started

Задачи:

- [ ] Показ полного контекста transaction/payment/fraud evaluation.
- [ ] Показ triggered rules.
- [ ] Approve / reject / escalate actions.
- [ ] Audit trail analyst actions.

Definition of Done:

- [ ] Risk analyst может принять решение без выхода из системы.

---

## FE-11. Обновить API client под новую модель данных

Приоритет: P0
Статус: Not Started

Задачи:

- [ ] Добавить метод `getPlatformBalance()`.
- [ ] Добавить метод `getPlatformSummary()`.
- [ ] Добавить DTO с provider status + business status + fraud fields.
- [ ] Добавить методы для fraud cases endpoints.

Файлы:

- [src/FinTechAPI.Client/src/app/api/client.ts](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Client/src/app/api/client.ts)

---

## FE-12. Обновить продуктовую терминологию по всему фронту

Приоритет: P0
Статус: Not Started

Задачи:

- [ ] Убрать wallet semantics.
- [ ] Убрать misleading wording про internal balance.
- [ ] Везде разделить `platform balance`, `payment status`, `transaction status`, `fraud decision`.

Definition of Done:

- [ ] В интерфейсе нет двусмысленности, что FinTechAPI хранит деньги пользователя.

---

## 5. Порядок реализации

### Шаг 1

- [ ] BE-04 Stripe-backed balance endpoint
- [ ] FE-01 Dashboard balance migration
- [ ] FE-12 Terminology cleanup

### Шаг 2

- [ ] BE-02 Remove internal balance mutation
- [ ] BE-06 Status split and mapping
- [ ] FE-03 Transactions status split

### Шаг 3

- [ ] BE-08 FraudEvaluation storage
- [ ] BE-09 Fraud Rule Engine MVP
- [ ] FE-05 Create Payment fraud UX

### Шаг 4

- [ ] BE-10 FraudCase backend
- [ ] FE-09 Cases Queue
- [ ] FE-10 Case Details

### Шаг 5

- [ ] BE-07 Reconciliation hardening
- [ ] BE-12 Tests
- [ ] FE-04 Transaction Details
- [ ] FE-06 Payment Details

---

## 6. Migration Notes

- На первом этапе поле `Account.balance` можно не удалять физически.
- На первом этапе нужно перестать использовать его как денежный источник истины.
- После перевода dashboard и backend logic в новую модель можно делать cleanup схемы.

---

## 7. Следующий документ

Следующий артефакт после этого файла:

- детальный sprint-by-sprint backlog
- weekly execution checklist
- технический migration checklist по каждому файлу
