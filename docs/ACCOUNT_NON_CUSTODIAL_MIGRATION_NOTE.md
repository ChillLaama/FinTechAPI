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

1. Dashboard и связанные frontend экраны переведены на platform endpoints.
2. В API добавлены platform balance/summary endpoints и reconciliation endpoint.
3. Поле `Account.balance` решено оставить физически как legacy compatibility поле до отдельного cleanup-релиза схемы.

## Правило до cleanup схемы

Пока поле `Account.balance` физически существует:

- не использовать его как официальный денежный остаток;
- не делать на нем бизнес-решения по движению средств;
- трактовать как legacy/compatibility поле.
