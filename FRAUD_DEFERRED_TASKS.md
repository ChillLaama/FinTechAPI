# FinTechAPI Fraud Deferred Tasks

Дата: 2026-03-20
Статус: deferred backlog
Причина: fraud контур отложен по решению команды, выполняется после завершения non-fraud frontend/test scope.

## 1. Backend Fraud

### FR-BE-01 FraudEvaluation storage
Приоритет: P0
Статус: Deferred

- [ ] Создать модель FraudEvaluation.
- [ ] Сохранять fraudScore, riskLevel, decision, reasons, rulesTriggered, rulesVersion, correlationId.
- [ ] Связать evaluation с transactionId и paymentId.

### FR-BE-02 Fraud Rule Engine MVP
Приоритет: P0
Статус: Deferred

- [ ] Реализовать velocity rules.
- [ ] Реализовать amount anomaly rules.
- [ ] Реализовать repeated failure rules.
- [ ] Реализовать new recipient rules.
- [ ] Реализовать geo/device anomaly rules.
- [ ] Вернуть decision: allow/review/block.
- [ ] Добавить shadow-mode для риска высокого уровня.

### FR-BE-03 FraudCase lifecycle + queue backend
Приоритет: P0
Статус: Deferred

- [ ] Создать модель FraudCase.
- [ ] Реализовать статусы open/in_review/approved/rejected/expired.
- [ ] API списка кейсов и статусов.
- [ ] approve/reject/escalate endpoints.
- [ ] audit trail действий аналитика.

### FR-BE-04 Fraud audit and correlation
Приоритет: P0
Статус: Deferred

- [ ] Логировать fraud decisions.
- [ ] Логировать analyst approvals/rejections.
- [ ] Вести end-to-end correlation id для fraud pipeline.

### FR-BE-05 Fraud tests
Приоритет: P0
Статус: Deferred

- [ ] Тест: fraud decision сохраняется и влияет на create payment flow.
- [ ] Тесты rules engine.
- [ ] Тесты review queue lifecycle.

## 2. Frontend Fraud

### FR-FE-01 Create Payment fraud UX
Приоритет: P0
Статус: Deferred

- [ ] Показать fraud pre-check state.
- [ ] Показать allow/review/block.
- [ ] Для review показать waiting state.
- [ ] Для block показать reason + next steps.

### FR-FE-02 Fraud Monitoring Dashboard
Приоритет: P1
Статус: Deferred

- [ ] KPI по fraud.
- [ ] blocked/reviewed trends.
- [ ] false-positive outcomes.
- [ ] operational alerts.

### FR-FE-03 Fraud Cases Queue UI
Приоритет: P0
Статус: Deferred

- [ ] Список кейсов review.
- [ ] Фильтры по status/risk/assignee/age.
- [ ] Быстрые actions open/assign.

### FR-FE-04 Fraud Case Details UI
Приоритет: P0
Статус: Deferred

- [ ] Полный контекст transaction/payment/fraud evaluation.
- [ ] Triggered rules.
- [ ] approve/reject/escalate.
- [ ] analyst audit trail.

### FR-FE-05 Client API fraud methods
Приоритет: P0
Статус: Deferred

- [ ] Добавить API методы для fraud cases endpoints.
- [ ] Добавить DTO fraud evaluation для frontend.

## 3. Activation Conditions

Вернуться к этому файлу после:
- [ ] завершения non-fraud frontend scope;
- [ ] стабилизации tests для non-fraud контуров;
- [ ] фиксации webhook reconciliation улучшений.
