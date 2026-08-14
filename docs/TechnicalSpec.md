# Cairn Technical Specification

## Shape

A single net472 class library, `com.yesuchum.Cairn.FollowingEvents`, containing fourteen `Rock.Follow.EventComponent` subclasses, two abstract bases, and one seed migration. No custom tables, no blocks, no jobs, no REST. Components register themselves as EntityTypes on discovery; deploy is a DLL copy and a recycle.

The seed migration (`Migrations/001_SeedStarterEventTypes.cs`) inserts one inactive starter `FollowingEventType` per component — pinned guids, template baked in from the component's `DefaultNotificationFormat` property, per-component descriptions. Config rows only; idempotent per guid; `Down()` removes only starters that were never activated or renamed. The templates follow core's digest-row style (photo cell, bold headline, contact lines for person events) because the notification email wraps each section in a table. `DefaultNotificationFormat` is not a runtime fallback — the event type's own format is always what renders.

The namespace keeps "FollowingEvents" so the assembly stays self-describing in Bin.

## Version floor: 18.2.4, and why

The schematic's original floor was v16, raised on contact with reality:

- Spark published no v16 packages to NuGet at all. Earliest stable is 17.0.43.
- v17's `Rock.dll` cannot be compiled against in practice: overload resolution on `Person` touches DotLiquid fork types (`ILiquidizable`, `IIndexable`) that live in a `DotLiquid.dll` Spark never distributed, and the v18/v19 forks removed those very types, so no obtainable binary satisfies the reference.
- A DLL compiled against a later Rock does not bind on an earlier instance anyway, so a v17 compile was the only route to v17 support, and it is closed.

18.2.4 compiles the entire pack with zero fallback code and zero warnings. Verified running on 18.2, 19.2, and the v20 pre-alpha.

Two places the floor shows in code:

- `ConnectionRequest.ConnectedDateTime` is a v19 column, resolved at runtime: a one-time reflection lookup uses the real connect time on v19+ and falls back to `ModifiedDateTime` only on 18.x, where the column does not exist.
- v19 also moved the `ConnectionState` enum to a different assembly, so v18.2-compiled code cannot touch `ConnectionRequest.ConnectionState` directly (`MissingMethodException` at runtime on v19+). Both connection components filter by person in SQL and resolve state in memory via a cached reflection read, compared by enum name. A person's request set is small, so the in-memory pass is cheap. (`GroupMemberStatus` already lived in `Rock.Enums` at 18.2, so the group components bind correctly everywhere.)
- `Group` has no inactivated timestamp in any version; `GroupInactivated` uses `ModifiedDateTime` for the inactivate case and the real `ArchivedDateTime` for the archive case.

## Base classes

### CairnEventComponent

Every component derives from this. It owns the `IEventComponentAdditionalMergeFields` plumbing, modeled line-for-line on core `PersonNoteAdded`:

- The interface moves the entry point to `HasEventHappened( ..., out Dictionary<string, List<object>> )`. The base seals the original overload to throw, exactly as core does.
- `FormatEntityNotification` renders the event type's Lava once per merge object, so multi-item components (three members added since yesterday) produce three rendered sections in the digest.

Components hand the template ready data objects (`LavaDataObject` subclasses); no date math happens in Lava.

### AnnualDateEventComponent

Core implements the weekend shift three inconsistent ways (Anniversary: full Fri/Sat/Sun shift; Birthday: full shift plus a today-exception; Baptized: Friday only, which is a defect). This class implements it once, correctly, and the three annual components supply only a source date and a display name.

Owns:

- **Next occurrence** from any source date: month/day forward to the next occurrence; Feb 29 resolves to Feb 28 in non-leap years.
- **Weekend shift**, honoring the event type's `SendOnWeekends`: Friday runs get `leadDays += 2` so events landing Sat/Sun are covered; Sat/Sun runs evaluate as if it were Friday.
- **Windowed dedupe** (Pattern A below).
- **Nth Year** multiplier; 0 = every year, matching core Anniversary semantics.

Abstract members: `GetSourceDate( followingEvent, personAlias, rockContext )` and `GetSourceName( followingEvent, rockContext )`.

## Dedupe patterns

Every component's re-fire behavior is one of four patterns, chosen per the semantics of the event:

| Pattern | Rule | Core precedent | Used by |
|---|---|---|---|
| A. Windowed annual | Fire when `nextDate - processDate <= leadDays` AND `nextDate - lastNotified > leadDays` | Birthday, Anniversary | the three annual components |
| B. Trailing cutoff | Cutoff = later of (today − daysBack) and `lastNotified`; fire on activity after cutoff | PrayerRequest | Left Group, Entered Opportunity, Member Added, Member Removed, New Registration |
| C. Once ever | `if lastNotified.HasValue return false` | FirstJoinedGroupType | Request Connected, Group Inactivated, Closing Soon |
| D. Re-notify interval | Fire while condition holds AND `lastNotified` is null or older than N days; N=0 collapses to C | none (new) | Data View Match, Attendance Not Entered, Nearing Capacity |

`lastNotified` is per follower per event type, supplied by the Send Following Events job, which is what makes all of this per-follower correct with no state of our own.

## Component notes

- **Date Attribute Anniversary** resolves the configured attribute guid through `AttributeCache`, loads the person's attributes once per evaluation, and parses with `AsDateTime()`. Rock's attribute picker cannot filter by field type (its qualifier settings filter by entity qualifier), so runtime parsing is the guard: non-date values yield null and never fire.
- **Data View Match** requires a persisted Data View and reads `DataViewPersistedValue` rows directly (`rockContext.Set<DataViewPersistedValue>()`); it never live-executes the view. Non-persisted views simply never fire.
- **Left Group of Type / Member Removed** share the exit-signal definition: `GroupMemberStatus == Inactive && InactiveDateTime > cutoff`, or `IsArchived && ArchivedDateTime > cutoff`, queried with `Queryable( true )` to include archived rows. Hard deletes leave no signal; that is documented user-facing, not worked around.
- **Attendance Not Entered** counts an occurrence missing when it is older than the threshold, `DidNotOccur != true`, and no `Attendance` row has `DidAttend` set.
- **Nearing Capacity / Closing Soon / New Registration** count non-waitlist registrants only (`!OnWaitList`).

## EntityType guids

All fourteen are pinned in `SystemGuid/EntityType.cs` and were generated before first deploy. They are permanent: Rock keys `FollowingEventType` records to the component's EntityType, so changing a guid after release orphans every event type built on it. The file is grouped by phase and carries the warning in a comment.

## Conventions

- Nested `private static class AttributeKey` per component; shared annual keys in `AnnualDateEventComponent.AnnualAttributeKey`. No string literals in `GetAttributeValue` calls.
- `RockDateTime` only; never `DateTime.Now`.
- One `RockContext` per evaluation, `AsNoTracking()` on all reads.
- No mutable instance state anywhere — components are MEF singletons.
- `[DisplayName]` carries the dropdown label (the ComponentPicker prefers it over the split-cased class name); `[Description]` matches it. Both end in `(cairn plugin)`.

## Follow affordances

All native: person and group detail pages carry follow stars in core, and the Obsidian Registration Instance Detail block ships one as well — no plugin-side follow UI needed.

## Build and deploy

```
dotnet build com.yesuchum.Cairn.FollowingEvents.csproj -c Release
```

One `PackageReference` (RockRMS.Rock 18.2.4) plus framework references for `System.Web`, `System.ComponentModel.Composition`, and `System.ComponentModel.DataAnnotations`. CI (GitHub Actions, `windows-latest`) builds every push and attaches the DLL to the release on version tags.

Deploy: copy `bin/Release/net472/com.yesuchum.Cairn.FollowingEvents.dll` to `RockWeb\Bin`, recycle, confirm the fourteen `com.yesuchum.Cairn.*` rows in EntityType and the dropdown entries. Rock discovers the MEF exports and registers EntityTypes automatically on startup.
