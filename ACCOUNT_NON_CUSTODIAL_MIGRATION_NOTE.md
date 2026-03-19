# Account Non-Custodial Migration Note

Дата: 2026-03-19
Статус: active migration note

## Контекст

Система перешла на Stripe-first non-custodial модель.
Это означает:
- платформа не хранит клиентские средства;
- отображаемый денежный баланс должен приходить со Stripe;
- внутренние account/transaction/payment сущности используются для бизнес-учета, оркестрации, аудита и аналитики.

## Что изменено

1. В доменной и DTO модели поле account balance помечено как legacy semantics.
2. В Firestore документе account balance уже помечен как legacy semantics.
3. При создании нового account входной баланс больше не принимается как денежная истина:
   - `CreateAccountAsync` принудительно устанавливает `Balance = 0m`.

## Что остается до полного завершения миграции

1. Dashboard и связанные frontend экраны должны перейти на Stripe-backed endpoint (`/api/platform/balance`).
2. В API нужно добавить platform balance/summary endpoints.
3. Поле `Account.balance` можно удалить физически после полного перевода frontend/backend на новую модель.

## Правило до cleanup схемы

Пока поле `Account.balance` физически существует:
- не использовать его как официальный денежный остаток;
- не делать на нем бизнес-решения по движению средств;
- трактовать как legacy/compatibility поле.
