# Cairn Uninstall Guide

Cairn ships no tables, no migrations, no pages, no blocks, and no jobs, so uninstall is short.

## 1. Remove the event types

Admin Tools > System Settings > Following Events: delete any event types built on the `(cairn plugin)` components. Do this first — an event type whose component DLL is missing logs exceptions when the Send Following Events job runs.

If you would rather sweep them in SQL:

```sql
DELETE fet
FROM [FollowingEventType] fet
INNER JOIN [EntityType] et ON et.[Id] = fet.[EntityTypeId]
WHERE et.[Name] LIKE 'com.yesuchum.Cairn%';
```

## 2. Remove the file

```
RockWeb\Bin\com.yesuchum.Cairn.FollowingEvents.dll
```

## 3. Housekeeping (optional)

- Entity types: the fourteen rows named `com.yesuchum.Cairn.*` are inert once the DLL is gone; delete them manually if you like tidy tables.
- Attribute definitions created for the event types (Lead Days, Nth Year, and friends) are keyed to the deleted event type records and clean up with them.

## 4. Restart

Restart the Rock application and confirm the exception log stays quiet after the next Send Following Events run.
