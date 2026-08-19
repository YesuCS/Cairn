# Cairn

Fifteen following-event components for Rock RMS, named for the stacked stones that guide followers along a path. Follow a person and get told about the anniversaries, milestones, and transitions that matter; follow a group and hear when members come and go or attendance goes missing; follow a registration instance and watch it fill up and close. No tables, no blocks: one DLL into `RockWeb/Bin`, config-only seed migrations create fifteen inactive starter event types with working templates, and the components appear in the Event Type dropdown.

Core v19 ships exactly eight event components. Nothing in this pack duplicates one.

## Documentation

- [User Guide](docs/UserGuide.md). Install, creating event types, every component's settings and merge fields, the security posture that actually matters.
- [Technical Specification](docs/TechnicalSpec.md). Architecture, base classes, dedupe patterns, the weekend shift, the version floor and why it is 18.2, build and deploy.
- [Uninstall Guide](docs/Uninstall.md). Short. There is no schema to clean up.

Follow affordances are all native: person and group detail pages have follow stars in core, and the Obsidian Registration Instance Detail block ships one too.

## The components

| Followed type | Component | One line |
|---|---|---|
| Person | Date Attribute Anniversary | Annual recurrence on any Person date attribute. Sobriety date, salvation date, whatever you track. |
| Person | Milestone Birthday | Birthday with the Nth Year multiplier core Birthday lacks. |
| Person | Serving Anniversary | Annual recurrence on the earliest first-join date across one or more group types. |
| Person | Data View Match | Any persisted Data View becomes a following event. |
| Person | Left Group of Type | Membership in any chosen group type went inactive or archived. |
| Person | Stopped Attending Group Type(s) | Had been attending, now has not for N days. |
| Person | Entered Connection Opportunity | A connection request was created. |
| Person | Connection Request Connected | A request reached Connected. Once ever. |
| Group | Member Added | Someone joined the group you follow. |
| Group | Member Removed | Someone left it. |
| Group | Attendance Not Entered | An occurrence is overdue with no attendance and no Did Not Occur. |
| Group | Inactivated or Archived | The group itself went away. |
| RegistrationInstance | New Registration | Every follower hears about new registrations, not one configured contact. |
| RegistrationInstance | Nearing Capacity | Registrants reached a threshold percent of Max Attendees. |
| RegistrationInstance | Closing Soon | Registration end date is inside the lead window. |

Every dropdown entry ends in `(cairn plugin)` so nobody mistakes ours for core and can come at me if something breaks haha.

## Repo layout

```
Follow/                     Base classes: CairnEventComponent (merge-field
                            plumbing), AnnualDateEventComponent (annual
                            recurrence, weekend shift, windowed dedupe).
Follow/Event/               The fourteen components.
SystemGuid/EntityType.cs    Fourteen pinned EntityType guids. Permanent.
docs/                       User guide, technical spec, uninstall.
```

## Build

```
dotnet build com.yesuchum.Cairn.FollowingEvents.csproj -c Release
```

Compiles against the RockRMS 18.2.4 NuGet packages (the floor; see the tech spec for why). Output deploys as a single DLL to `RockWeb/Bin`; recycle and the components register themselves as EntityTypes on discovery.
