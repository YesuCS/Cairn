# Cairn Uninstall Guide

Cairn ships no tables, no pages, no blocks, and no jobs, only components and config-only seed migrations, so uninstall is short.

## Uninstalling or downgrading through Rock

Rock doesn't support uninstalling or downgrading plugins yet. The Rock Shop can install a plugin and update it to a newer version, but at the time this was written (November 2026) there's no way to remove a plugin or roll it back to an older version from inside Rock.

Cairn is ready for when Spark adds that. The package ships an `uninstall` folder with the file list and a `run.sql` that does the SQL in step 1, and its migrations have a `Down()`, so Rock will have what it needs once it can do this on its own. Until then, uninstalling is the manual process below, and downgrading means uninstalling and then installing the older version by hand.

## 1. Remove the event types

Admin Tools > System Settings > Following Events: delete any event types built on the `(cairn plugin)` components, including the seeded `(cairn starter)` records. Do this first — an event type whose component DLL is missing logs exceptions when the Send Following Events job runs.

If you would rather sweep them in SQL:

```sql
DELETE fet
FROM [FollowingEventType] fet
INNER JOIN [EntityType] et ON et.[Id] = fet.[EntityTypeId]
WHERE et.[Name] LIKE 'com.yesuchum.Cairn%';

DELETE FROM [PluginMigration] WHERE [PluginAssemblyName] LIKE 'com.yesuchum.Cairn%';
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
