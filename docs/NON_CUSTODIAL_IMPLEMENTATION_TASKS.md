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

Fraud roadmap вынесен в отдельный файл:

- [FRAUD_DEFERRED_TASKS.md](c:/Users/Daniel/RiderProjects/FinTechAPI/FRAUD_DEFERRED_TASKS.md)

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
Статус: Completed

Задачи:

- [x] Перестать использовать `Account.balance` как основной денежный атрибут.
- [x] Определить новую семантику `Account`: profile / reporting bucket / logical grouping.
- [x] Решить, оставляем ли поле `balance` физически как legacy на переходный период.
- [x] Подготовить migration note по отказу от wallet semantics.

Файлы:

- [src/FinTechAPI.Infrastructure/Firebase/Documents/AccountDocument.cs](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Infrastructure/Firebase/Documents/AccountDocument.cs)
- [src/FinTechAPI.Infrastructure/Services/AccountService.cs](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Infrastructure/Services/AccountService.cs)
- [src/FinTechAPI.Application/DTOs/AccountDto.cs](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Application/DTOs/AccountDto.cs)

Definition of Done:

- [x] В модели и сервисах account больше не трактуется как внутренний кошелек.

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
Статус: Completed

Задачи:

- [x] Добавить endpoint `GET /api/platform/summary`.
- [x] Вернуть aggregated метрики: processed volume, successful payments, failed payments, pending review count, fraud blocked count.
- [x] Согласовать, какие метрики считаются из Stripe, а какие из внутренних transactions/payments.

Definition of Done:

- [x] Dashboard можно строить без обращения к внутреннему account balance.

---

## BE-06. Разделить provider status и business status

Приоритет: P0
Статус: Completed

Задачи:

- [x] Зафиксировать отдельные статусы для internal transaction и Stripe payment.
- [x] Добавить централизованный mapper Stripe status -> internal transaction status.
- [x] Убрать неоднозначность между `Transaction.status` и `Payment.status`.
- [x] Убедиться, что frontend получает оба состояния явно.

Файлы:

- [src/FinTechAPI.Infrastructure/Services/PaymentService.cs](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Infrastructure/Services/PaymentService.cs)
- [src/FinTechAPI.Infrastructure/Services/TransactionService.cs](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Infrastructure/Services/TransactionService.cs)
- [src/FinTechAPI.Application/DTOs](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Application/DTOs)

Definition of Done:

- [x] В API и UI provider status и business status не смешиваются.

---

## BE-07. Усилить webhook reconciliation

Приоритет: P0
Статус: Completed

Что уже есть:

- идемпотентность обработки событий;
- state transition guard;
- логирование webhook lifecycle.

Задачи:

- [x] Добавить явный reconciliation процесс для потерянных webhook.
- [x] Добавить алерты по рассинхрону `Payment.status` и `Transaction.status`.
- [x] Добавить endpoint/джоб для ручной повторной синхронизации.
- [x] Документировать recovery сценарии.

Файлы:

- [src/FinTechAPI.Infrastructure/Services/PaymentService.cs](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Infrastructure/Services/PaymentService.cs)
- [src/FinTechAPI.API/Controllers/PaymentsController.cs](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.API/Controllers/PaymentsController.cs)
- [PAYMENT_RECONCILIATION_RUNBOOK.md](c:/Users/Daniel/RiderProjects/FinTechAPI/PAYMENT_RECONCILIATION_RUNBOOK.md)

Definition of Done:

- [x] Есть способ восстановить консистентность между Stripe и внутренней системой.

---

## BE-08. Реализовать FraudEvaluation storage

Приоритет: P0
Статус: Deferred (moved)

Задачи:

- [ ] Создать модель `FraudEvaluation`.
- [ ] Сохранять `fraudScore`, `riskLevel`, `decision`, `reasons`, `rulesTriggered`, `rulesVersion`, `correlationId`.
- [ ] Связать evaluation с `transactionId` и `paymentId`.

Definition of Done:

- [ ] Каждое fraud-решение сохраняется и может быть показано в UI.
- [ ] Выполняется по [FRAUD_DEFERRED_TASKS.md](c:/Users/Daniel/RiderProjects/FinTechAPI/FRAUD_DEFERRED_TASKS.md).

---

## BE-09. Реализовать Fraud Rule Engine MVP

Приоритет: P0
Статус: Deferred (moved)

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
Статус: Deferred (moved)

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
Статус: Completed (non-fraud scope)

Задачи:

- [x] Тест: create transaction не меняет internal balance.
- [x] Тест: update transaction status не меняет internal balance.
- [x] Тест: delete transaction не меняет internal balance.
- [x] Тест: Stripe-backed balance endpoint возвращает корректный DTO.
- [x] Тест: webhook reconciliation обновляет state без internal wallet logic.
- [ ] Тест: fraud decision записывается и влияет на create payment flow (deferred в fraud backlog).

Файлы:

- [tests/FinTechAPI.Tests](c:/Users/Daniel/RiderProjects/FinTechAPI/tests/FinTechAPI.Tests)

---

## 4. Frontend Tasks

## FE-01. Перевести Dashboard на Stripe-backed balance

Приоритет: P0
Статус: Completed

Задачи:

- [x] Убрать вычисление общего баланса из суммы `account.balance`.
- [x] Подключить новый endpoint `GET /api/platform/balance`.
- [x] Переименовать UI-блоки: `Platform balance`, `Available`, `Pending`, `Settlement summary`.
- [x] Добавить `last synced` и fallback/error state.

Файлы:

- [src/FinTechAPI.Client/src/app/components/Dashboard.tsx](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Client/src/app/components/Dashboard.tsx)
- [src/FinTechAPI.Client/src/app/api/client.ts](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Client/src/app/api/client.ts)

Definition of Done:

- [x] Dashboard больше не использует внутренние balances как главный источник денег.

---

## FE-02. Обновить dashboard metrics под новую модель

Приоритет: P1
Статус: Completed

Задачи:

- [x] Показать processed volume.
- [x] Показать successful/failed payments.
- [x] Показать pending review count.
- [x] Показать fraud blocked volume/count.
- [x] Добавить provider/API health summary.

---

## FE-03. Разделить provider status и business status в Transactions List

Приоритет: P0
Статус: Completed

Задачи:

- [x] Показать отдельно business transaction status.
- [x] Показать отдельно provider payment status.
- [x] Показать risk level / fraud decision.
- [x] Добавить фильтры по provider status и fraud status.

Файлы:

- [src/FinTechAPI.Client/src/app/components/Transactions.tsx](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Client/src/app/components/Transactions.tsx)
- [src/FinTechAPI.Client/src/app/api/client.ts](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Client/src/app/api/client.ts)

Definition of Done:

- [x] На списке больше нет смешения внутренних и Stripe-статусов.

---

## FE-04. Переписать Transaction Details под новую модель

Приоритет: P0
Статус: Completed

Задачи:

- [x] Добавить timeline бизнес-статуса.
- [x] Добавить timeline provider-статуса.
- [x] Добавить fraud score / reasons / risk level.
- [x] Добавить webhook events.
- [x] Добавить correlation id / technical details.

Definition of Done:

- [x] Transaction Details объясняет, что произошло в бизнес-слое, провайдере и antifraud.

---

## FE-05. Переписать Create Payment под provider-first UX

Приоритет: P0
Статус: Completed

Что уже есть:

- transaction creation;
- payment intent creation;
- idempotency key;
- update transaction status.

Что нужно сделать:

- [x] Убрать wallet-style формулировки про внутренний баланс счета.
- [x] Добавить provider-first терминологию и статусные сообщения в create payment flow.
- [x] Добавить переход к payment details после успешного создания.
- [ ] Fraud pre-check UX вынесен в [FRAUD_DEFERRED_TASKS.md](c:/Users/Daniel/RiderProjects/FinTechAPI/FRAUD_DEFERRED_TASKS.md).

Файлы:

- [src/FinTechAPI.Client/src/app/components/CreatePayment.tsx](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Client/src/app/components/CreatePayment.tsx)

Definition of Done:

- [x] Пользователь видит разницу между бизнес-проверкой и provider state.
- [ ] Fraud-state часть будет закрыта позже по fraud backlog.

---

## FE-06. Добавить Payment Details экран

Приоритет: P1
Статус: Completed

Задачи:

- [x] Отдельный экран/route для payment lifecycle.
- [x] Показ `stripePaymentIntentId`, provider status, reconciliation state.
- [x] Показ связи с transaction.
- [x] Показ ошибок provider и retry context.

Definition of Done:

- [x] Есть отдельная точка просмотра provider-side состояния платежа.

---

## FE-07. Пересобрать Accounts UI под logical profiles

Приоритет: P1
Статус: Completed

Задачи:

- [x] Убрать восприятие accounts как wallet/accounts with money.
- [x] Переименовать/переформулировать UI при необходимости.
- [x] Использовать accounts как grouping/reporting/payment profiles.
- [x] Убрать отображение misleading balance values.

Definition of Done:

- [x] Accounts UI не создает семантику хранения средств в FinTechAPI.

---

## FE-08. Добавить Fraud Monitoring Dashboard

Приоритет: P1
Статус: Deferred (moved)

Задачи:

- [ ] Показ KPI по fraud.
- [ ] Показ blocked/reviewed trends.
- [ ] Показ false positive review outcomes в будущем.
- [ ] Показ operational alerts.

---

## FE-09. Добавить Fraud Cases Queue UI

Приоритет: P0
Статус: Deferred (moved)

Задачи:

- [ ] Список кейсов review.
- [ ] Фильтры по status, risk level, assignee, age.
- [ ] Быстрые действия open/assign.

Definition of Done:

- [ ] Аналитик может открыть и обработать сомнительный кейс.

---

## FE-10. Добавить Fraud Case Details UI

Приоритет: P0
Статус: Deferred (moved)

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
Статус: Completed (non-fraud scope)

Задачи:

- [x] Добавить метод `getPlatformBalance()`.
- [x] Добавить метод `getPlatformSummary()`.
- [x] Добавить DTO с provider status + business status + provider telemetry fields.
- [ ] Методы для fraud cases endpoints вынесены в [FRAUD_DEFERRED_TASKS.md](c:/Users/Daniel/RiderProjects/FinTechAPI/FRAUD_DEFERRED_TASKS.md).

Файлы:

- [src/FinTechAPI.Client/src/app/api/client.ts](c:/Users/Daniel/RiderProjects/FinTechAPI/src/FinTechAPI.Client/src/app/api/client.ts)

---

## FE-12. Обновить продуктовую терминологию по всему фронту

Приоритет: P0
Статус: Completed

Задачи:

- [x] Убрать wallet semantics.
- [x] Убрать misleading wording про internal balance.
- [x] Везде разделить `platform balance`, `payment status`, `transaction status`, `fraud decision`.

Definition of Done:

- [x] В интерфейсе нет двусмысленности, что FinTechAPI хранит деньги пользователя.

---

## 5. Порядок реализации

### Шаг 1

- [x] BE-04 Stripe-backed balance endpoint
- [x] FE-01 Dashboard balance migration
- [ ] FE-12 Terminology cleanup

### Шаг 2

- [x] BE-02 Remove internal balance mutation
- [x] BE-06 Status split and mapping
- [x] FE-03 Transactions status split

### Шаг 3

- [ ] Deferred to fraud backlog: [FRAUD_DEFERRED_TASKS.md](c:/Users/Daniel/RiderProjects/FinTechAPI/FRAUD_DEFERRED_TASKS.md)

### Шаг 4

- [ ] Deferred to fraud backlog: [FRAUD_DEFERRED_TASKS.md](c:/Users/Daniel/RiderProjects/FinTechAPI/FRAUD_DEFERRED_TASKS.md)

### Шаг 5

- [x] BE-07 Reconciliation hardening
- [x] BE-12 Tests
- [x] FE-04 Transaction Details
- [x] FE-06 Payment Details

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
