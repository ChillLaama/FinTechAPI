# Расширение функционала Stripe

> Текущее состояние: реализованы Payment Intents, Webhooks, Payouts (Connect), Platform Balance, Reconciliation, внутренняя ML-модель фрода.
> Ниже — задачи по расширению, сгруппированные по направлениям.

---

## 1. Возвраты (Refunds)

- [ ] **1.1** Создать `IStripeRefundService` + `StripeRefundService` — обёртка над `RefundService` Stripe SDK
- [ ] **1.2** Добавить endpoint `POST /api/payments/{paymentId}/refund` (полный и частичный возврат)
- [ ] **1.3** Расширить `PaymentDocument` полями `refundedAmountMinorUnits`, `refundStatus`, `stripeRefundId`
- [ ] **1.4** Обработать webhook-события `charge.refunded`, `charge.refund.updated` в `PaymentService`
- [ ] **1.5** Добавить бизнес-правила: запрет возврата для `canceled` платежей, лимит суммы возврата ≤ оригинала
- [ ] **1.6** UI: кнопка «Вернуть средства» в `PaymentDetails.tsx` с подтверждением суммы
- [ ] **1.7** Юнит- и интеграционные тесты на сценарии: полный возврат, частичный, повторный, отклонённый

## 2. Споры и чарджбеки (Disputes)

- [ ] **2.1** Обработать webhook-события `charge.dispute.created`, `charge.dispute.updated`, `charge.dispute.closed`
- [ ] **2.2** Создать `DisputeDocument` (Firestore): `stripeDisputeId`, `paymentId`, `reason`, `status`, `amount`, `evidence_due_by`
- [ ] **2.3** Endpoint `GET /api/disputes` — список активных споров пользователя/платформы
- [ ] **2.4** Endpoint `POST /api/disputes/{disputeId}/evidence` — загрузка доказательств через Stripe Dispute API
- [ ] **2.5** UI: страница `DisputesDashboard.tsx` — таблица споров со статусами и дедлайнами
- [ ] **2.6** Уведомления (email/in-app) при создании нового спора

## 3. Подписки и регулярные платежи (Subscriptions)

- [ ] **3.1** Создать Stripe Products + Prices для тарифных планов платформы
- [ ] **3.2** Реализовать `ISubscriptionService` + `SubscriptionService` (CRUD подписок)
- [ ] **3.3** Endpoints: `POST /api/subscriptions`, `GET /api/subscriptions`, `DELETE /api/subscriptions/{id}`, `PATCH /api/subscriptions/{id}` (смена плана)
- [ ] **3.4** Обработать webhook-события: `customer.subscription.created`, `customer.subscription.updated`, `customer.subscription.deleted`, `invoice.payment_failed`
- [ ] **3.5** Создать `SubscriptionDocument` (Firestore): `stripeSubscriptionId`, `stripePriceId`, `status`, `currentPeriodEnd`, `cancelAtPeriodEnd`
- [ ] **3.6** UI: страница выбора тарифа, управление подпиской, индикатор текущего плана
- [ ] **3.7** Grace-period логика при неудачном списании: retry, dunning emails, деактивация

## 4. Stripe Customers

- [ ] **4.1** Привязать каждого пользователя системы к `Stripe Customer` (поле `stripeCustomerId` в профиле)
- [ ] **4.2** Автоматически создавать Customer при регистрации или первом платеже
- [ ] **4.3** Endpoint `GET /api/profile/payment-methods` — список сохранённых карт/методов оплаты
- [ ] **4.4** Endpoint `POST /api/profile/payment-methods` — привязка нового метода через SetupIntent
- [ ] **4.5** Endpoint `DELETE /api/profile/payment-methods/{pmId}` — удаление метода
- [ ] **4.6** UI: раздел «Способы оплаты» в `Profile.tsx` — список карт, добавление, удаление, установка по умолчанию

## 5. Расширение Stripe Connect

- [ ] **5.1** Реализовать полный onboarding-флоу для Connected Accounts (`Account Links` API)
- [ ] **5.2** Endpoint `POST /api/connect/onboarding` — генерация ссылки на Stripe-hosted onboarding
- [ ] **5.3** Webhook-обработка `account.updated` — отслеживание статуса верификации аккаунта
- [ ] **5.4** Transfers API: разделение платежей между платформой и connected accounts (application fees)
- [ ] **5.5** Dashboard для connected accounts: баланс, история выплат, статус верификации
- [ ] **5.6** Поддержка `destination charges` и `separate charges and transfers` моделей
- [ ] **5.7** Обработка `payout.failed`, `payout.paid` webhook-событий для connected accounts

## 6. Stripe Checkout Sessions

- [ ] **6.1** Альтернативный flow через `Checkout Sessions` для упрощённого приёма платежей
- [ ] **6.2** Endpoints: `POST /api/checkout/sessions` → redirect URL, `GET /api/checkout/sessions/{id}/status`
- [ ] **6.3** Webhook-обработка `checkout.session.completed`, `checkout.session.expired`
- [ ] **6.4** Поддержка нескольких line items (корзина / invoice-like)
- [ ] **6.5** Страница success/cancel (`CheckoutSuccess.tsx`, `CheckoutCancel.tsx`)

## 7. Инвойсы (Invoices)

- [ ] **7.1** Генерация Stripe Invoice для разовых и подписочных платежей
- [ ] **7.2** Endpoint `GET /api/invoices` — список инвойсов пользователя
- [ ] **7.3** Endpoint `GET /api/invoices/{id}/pdf` — ссылка на PDF инвойса из Stripe
- [ ] **7.4** Webhook-обработка `invoice.paid`, `invoice.payment_failed`, `invoice.finalized`
- [ ] **7.5** UI: раздел «Счета» с таблицей инвойсов, скачивание PDF

## 8. Stripe Radar и расширенный фрод

- [ ] **8.1** Интегрировать Stripe Radar: передавать `radar_options` и metadata при создании PaymentIntent
- [ ] **8.2** Передавать результат внутренней ML-модели (`fraudScore`, `fraudDecision`) в metadata PaymentIntent для корреляции
- [ ] **8.3** Обрабатывать `radar.early_fraud_warning.created` webhook
- [ ] **8.4** Создать Radar Rules через API или Dashboard для автоблокировки по IP, стране, BIN
- [ ] **8.5** Сопоставлять решения Radar vs. внутренней модели — dashborad сравнения точности
- [ ] **8.6** Передавать `shipping`, `billing_details`, `customer` в PaymentIntent для повышения точности Radar

## 9. Мульти-валютность и локализация

- [ ] **9.1** Расширить Platform Balance до поддержки нескольких валют (Stripe возвращает массив `available[]` по currency)
- [ ] **9.2** Конвертация `PlatformBalanceDto` из single-currency в multi-currency модель
- [ ] **9.3** UI Dashboard: отображение баланса по каждой валюте
- [ ] **9.4** Поддержка Payment Methods по региону: iDEAL, Bancontact, SEPA Direct Debit, Przelewy24
- [ ] **9.5** Конфигурация разрешённых валют и методов оплаты на уровне платформы

## 10. Stripe Identity (KYC)

- [ ] **10.1** Интегрировать Stripe Identity для верификации личности пользователей
- [ ] **10.2** Endpoint `POST /api/identity/verification-sessions` — создание сессии верификации
- [ ] **10.3** Webhook-обработка `identity.verification_session.verified`, `...requires_input`
- [ ] **10.4** Хранение статуса верификации в профиле пользователя
- [ ] **10.5** Обязательная верификация перед выплатами выше пороговой суммы

## 11. Аналитика и отчётность

- [ ] **11.1** Stripe Reporting API: автоматическая выгрузка ежедневных/ежемесячных отчётов
- [ ] **11.2** Endpoint `GET /api/reports/stripe-fees` — комиссии Stripe за период
- [ ] **11.3** Endpoint `GET /api/reports/settlement` — сверка расчётов: наши записи vs. Stripe
- [ ] **11.4** Расширить `PlatformController.GetSummary()` данными из Stripe Balance Transactions API
- [ ] **11.5** UI: графики комиссий, net revenue, объёмы по дням

## 12. Улучшение Webhook-обработки

- [ ] **12.1** Добавить обработку `payment_intent.requires_action` — уведомление пользователя о необходимости 3DS
- [ ] **12.2** Dead-letter queue: сохранять необработанные/неизвестные webhook-события для ручного анализа
- [ ] **12.3** Webhook replay endpoint для тестирования: `POST /api/admin/webhooks/replay/{eventId}`
- [ ] **12.4** Метрики: счётчик обработанных/отклонённых/failed webhook-событий в Platform Summary
- [ ] **12.5** Обработать `payment_intent.partially_funded` (новый тип события Stripe)

## 13. Stripe Tax

- [ ] **13.1** Включить автоматический расчёт налогов через Stripe Tax при создании PaymentIntent
- [ ] **13.2** Передавать `customer_details.address` для корректного определения налоговой юрисдикции
- [ ] **13.3** Хранить и отображать сумму налога в PaymentDocument и UI
- [ ] **13.4** Интеграция с Invoice для автоматического включения Tax в счёт

---

## Приоритизация

| Приоритет | Задачи | Обоснование |
|-----------|--------|-------------|
| **P0 — Критично** | 1 (Refunds), 2 (Disputes), 4.1–4.2 (Customer привязка) | Базовый функционал платёжной платформы |
| **P1 — Высокий** | 5 (Connect расширение), 8 (Radar), 12 (Webhooks) | Безопасность, масштабируемость |
| **P2 — Средний** | 3 (Subscriptions), 6 (Checkout), 9 (Мульти-валюта) | Расширение бизнес-модели |
| **P3 — Низкий** | 7 (Invoices), 10 (Identity), 11 (Reporting), 13 (Tax) | Nice-to-have, compliance |
