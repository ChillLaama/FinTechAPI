# Payment Reconciliation Runbook

Дата: 2026-03-20
Scope: non-fraud payment consistency recovery

## Цель

Восстановить консистентность статусов между provider lifecycle и внутренними payment/transaction записями, если webhook задержан или потерян.

## Когда использовать

- Payment находится в промежуточном статусе слишком долго.
- Есть подозрение на потерянный webhook.
- Transaction status не совпадает с provider status.

## Основной recovery путь

1. Найти `paymentId` в системе.
2. Выполнить ручную синхронизацию:
   - `POST /api/payments/{paymentId}/reconcile`
3. Проверить ответ API:
   - provider status
   - lastWebhookEvent (`manual_reconcile`)
   - correlation id (`manual-reconcile:<timestamp>`)
4. Проверить связанную transaction запись:
   - статус должен быть синхронизирован из provider status mapper.

## Что делает endpoint reconcile

- Загружает payment запись по `paymentId`.
- Проверяет ownership по user id.
- Запрашивает актуальный PaymentIntent status у provider.
- Обновляет payment status и reconciliation telemetry.
- Синхронизирует business transaction status через provider->business mapper.

## Диагностика

Смотреть логи:
- `Manual reconciliation applied`
- `Transaction status synced from provider`
- `Transaction sync skipped`
- `Webhook status transition rejected`

## Ограничения текущего этапа

- Это ручной recovery endpoint, не background job.
- Fraud-аналитика и cases queue отложены и ведутся в [FRAUD_DEFERRED_TASKS.md](FRAUD_DEFERRED_TASKS.md).
