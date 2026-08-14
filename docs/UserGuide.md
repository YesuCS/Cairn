# Cairn User Guide

## What this is

Rock's Following system lets any staff member follow a person and get a nightly digest email when something happens: a birthday approaches, a note is added, a prayer request comes in. Core ships eight of those event components. Cairn adds fourteen more, and extends following beyond people: follow a group, follow a registration instance, and hear about the things that matter to each.

Notifications ride Rock's existing **Send Following Events** job and its daily digest email. Cairn adds no jobs, no schedules, and no communication templates of its own.

## Install

Install **Cairn** from the Rock Shop (Admin Tools > Rock Shop) like any other plugin; Rock restarts and everything below happens automatically.

Installing outside the shop (staging boxes, air-gapped instances) is one file:

1. Copy `com.yesuchum.Cairn.FollowingEvents.dll` into `RockWeb\Bin`.
2. Recycle the application pool.

Either way, open **Admin Tools > System Settings > Following Events** afterward. The Cairn components are in the Event Type dropdown when you add a new event type; every one ends in `(cairn plugin)`.

On first start a single seed migration runs — config rows only, no tables — creating one inactive starter event type per component with a working template baked in (see Starter event types below).

## Creating an event type

An event type is a record staff create once per thing-worth-hearing-about: pick the component, configure its settings, write the notification template, set security. People then follow whoever or whatever they care about, and subscribe to the event types they want (or get them automatically if **Is Notice Required** is on).

One component can back many event types. "Sobriety Anniversary" and "Salvation Anniversary" are two event types on the same Date Attribute Anniversary component with different attributes picked.

## The components

### Person components

**Date Attribute Anniversary.** Annual recurrence on any Person date attribute. Settings: the attribute, Lead Days (5), Nth Year (0 = every year). The attribute picker lists all Person attributes — Rock's picker cannot filter by field type — but a non-date pick is harmless: the value parses to nothing and the event never fires.

**Milestone Birthday.** Core Birthday, plus the Nth Year multiplier it lacks. Set Nth Year to 10 and you hear about 10th, 20th, 30th birthdays. `Years` in the template is the age they are turning.

**Serving Anniversary.** Annual recurrence on the date the person first joined a group of the configured type. Point it at your serve-team group type and you get serving anniversaries. Active Members Only (default on) ignores memberships that have since gone inactive.

**Data View Match.** The person is in the configured Data View's result set. The Data View must be persisted — the component reads the persisted values and never runs the view live, so follower-scale evaluation stays cheap. Rock stores no "entered the data view" date, so this fires on current membership with a Re-notify Days interval; 0 means once ever per follow.

**Left Group of Type.** A membership in the configured group type went inactive or was archived inside the window. **Only If No Remaining Membership** (default on) stays quiet while the person still has another active group of that type — moving between small groups is not leaving small groups. Hard-deleted memberships leave no signal and are not detected; archive or inactivate instead of delete if you want this event to see it.

**Entered Connection Opportunity.** A connection request was created for the person, matching a specific opportunity or a whole connection type (configure one of the two; the opportunity wins if both are set).

**Connection Request Connected.** A request reached Connected. Fires once ever per follow.

### Group components

Follow a group with the star on Group Detail. These notify the follower — a coach, a pastor, an admin — not the group's members.

**Member Added.** Someone was added inside the window. Include Pending (default off) also counts pending members. The template receives a `MemberData` list: name, role, date.

**Member Removed.** A membership went inactive or was archived inside the window. Same hard-delete caveat as Left Group of Type.

**Attendance Not Entered.** A past occurrence is more than Days After Occurrence old with no attendance recorded and Did Not Occur unset. Re-notify Days (default 7) keeps reminding until somebody enters it; 0 nags exactly once. Two boundaries: the group's type must take attendance, and an occurrence row must actually exist (check-in, the attendance entry page, or the reminder infrastructure creates them). A meeting whose occurrence was never generated is invisible to this event — it catches "the row is there and nobody filled it in," which is the common leader-forgot case.

**Inactivated or Archived.** The group itself was inactivated or archived. Once ever per follow. Rock stores no inactivated timestamp, so the group's last-modified time stands in for the inactivate case.

### Registration instance components

Core registration notifies one configured contact per instance. These flip that: anyone who follows the instance opts in, no event-owner configuration.

**New Registration.** A registration came in inside the window. `RegistrationData` list: registrant names, who registered them, when.

**Nearing Capacity.** Non-waitlist registrant count reached Threshold Percent (default 90) of Max Attendees. Instances with no Max Attendees never fire. Re-notify Days 0 = once ever.

**Closing Soon.** The registration end date falls inside Lead Days. Once ever per follow, and it includes the current registrant count so the notification doubles as a final-count preview.

Follow an instance with the star on its detail page — the Obsidian Registration Instance Detail block ships one natively.

## Starter event types

Install seeds one **inactive** starter event type per component, named `... (cairn starter)`, with a working notification template baked in — the same fleshed-out style as core's defaults (photo, bold headline, contact lines for person events). Edit a starter and activate it, or copy it and keep the original as a reference. Deleting starters is safe; the seed runs exactly once and never resurrects them.

## Notification templates

Every component hands the template ready merge objects — no date math in Lava. The multi-item components (`MemberData`, `OccurrenceData`, `RegistrationData`) render the template once per item, the same way core's Person Note Added works.

| Component | Merge object | Fields |
|---|---|---|
| Annual components (Date Attribute, Birthday, Serving) | `EventData` | `SourceDate`, `NextDate`, `Years`, `SourceName` |
| Data View Match | `EventData` | `SourceName`, `LastRefreshDateTime` |
| Left Group of Type | `EventData` (per exit) | `SourceName`, `GroupName`, `ExitDateTime` |
| Entered Connection Opportunity | `EventData` (per request) | `SourceName`, `RequestState`, `ConnectorName`, `RequestDateTime` |
| Connection Request Connected | `EventData` (per request) | `SourceName`, `ConnectorName`, `ConnectedDateTime` |
| Member Added | `MemberData` (per member) | `MemberName`, `RoleName`, `AddedDateTime` |
| Member Removed | `MemberData` (per member) | `MemberName`, `RoleName`, `ExitDateTime` |
| Attendance Not Entered | `OccurrenceData` (per occurrence) | `OccurrenceDate`, `ScheduleName` |
| Inactivated or Archived | `EventData` | `SourceName`, `ChangeType`, `ChangeDateTime` |
| New Registration | `RegistrationData` (per registration) | `RegistrantNames`, `RegisteredBy`, `RegisteredDateTime` |
| Nearing Capacity | `EventData` | `SourceName`, `RegistrantCount`, `MaxAttendees`, `PercentFull` |
| Closing Soon | `EventData` | `SourceName`, `CloseDate`, `DaysRemaining`, `RegistrantCount` |

### The template shapes

The notification email wraps each event section in a `<table>`, so templates are `<tr>` rows — the same shape core's defaults use. Every starter event type ships its component's template pre-filled; these are the two shapes they build on, ready to copy when writing your own.

Person events (photo, linked headline, contact lines):

```
<tr>
    <td style='padding-bottom: 12px; padding-right: 12px; min-width: 87px;'>
        {% if Entity.Person.PhotoId %}
            <img src='{{ 'Global' | Attribute:'PublicApplicationRoot' }}GetImage.ashx?id={{ Entity.Person.PhotoId }}&maxwidth=75&maxheight=75'/>
        {% endif %}
    </td>
    <td valign="top" style='padding-bottom: 12px; min-width: 300px;'>
        <strong><a href="{{ 'Global' | Attribute:'PublicApplicationRoot' }}Person/{{ Entity.PersonId }}">{{ Entity.Person.FullName }}</a>
        has their {{ EventData.Years | NumberToOrdinal }} {{ EventData.SourceName }} anniversary
        on {{ EventData.NextDate | Date:'dddd, MMMM d' }} ({{ EventData.NextDate | DaysFromNow | Capitalize }})</strong><br />

        {% if Entity.Person.Email != empty %}
            Email: <a href="mailto:{{ Entity.Person.Email }}">{{ Entity.Person.Email }}</a><br />
        {% endif %}

        {% assign mobilePhone = Entity.Person.PhoneNumbers | Where:'NumberTypeValueId', 12 | Select:'NumberFormatted' %}
        {% if mobilePhone != empty %}
            Cell: {{ mobilePhone }}<br />
        {% endif %}

        {% assign homePhone = Entity.Person.PhoneNumbers | Where:'NumberTypeValueId', 13 | Select:'NumberFormatted' %}
        {% if homePhone != empty %}
            Home: {{ homePhone }}<br />
        {% endif %}
    </td>
</tr>
```

Swap the headline (the `<strong>` line) for the component you're using — the merge-field table above lists what each one provides.

Group and registration events (linked headline plus detail lines; here, Member Added):

```
<tr>
    <td style='padding-bottom: 12px; padding-right: 12px; min-width: 87px;'>&nbsp;</td>
    <td valign="top" style='padding-bottom: 12px; min-width: 300px;'>
        <strong>{{ MemberData.MemberName }} was added to
        <a href="{{ 'Global' | Attribute:'InternalApplicationRoot' }}Group/{{ Entity.Id }}">{{ Entity.Name }}</a></strong><br />
        Role: {{ MemberData.RoleName }}<br />
        Added: {{ MemberData.AddedDateTime | Date:'dddd, MMMM d' }}<br />
    </td>
</tr>
```

Registration templates link the instance the same way: `<a href="{{ 'Global' | Attribute:'InternalApplicationRoot' }}RegistrationInstance/{{ Entity.Id }}">{{ Entity.Name }}</a>`. Every default headline links the entity it's about — person, group, or instance — so the digest is one click from the thing itself.

## Security. Read this part.

Notifications go to **anyone following the person who can see the event type**. Security on the underlying Person attribute does not trim the notification. If you build a Sobriety Anniversary event type, everyone who follows that person and can see the event type learns the date — the attribute's own security never gets a vote. The event type record is the enforcement point:

- Restrict **View** on the event type to the owning ministry role.
- Turn **Is Notice Required** off, so only deliberate subscribers receive it.

Do both for anything sensitive. Benign event types (birthdays, serving anniversaries) are fine with defaults.

## Testing an event type

Create it with Lead Days or Max Days Back set to hit today, follow a staged test person or group, run **Send Following Events** manually from Jobs Administration, and check the email. Run it a second time and confirm nothing re-fires — every component dedupes against the follower's last-notified date.

**If the job reports "0 following events emails sent" and you know an event should have fired:** the job only notifies followers who are active members of the group configured in its **Eligible Followers** setting, with an active email address and an email preference that allows it. A follower outside that group is silently skipped — this is the job's own gate, not the event type's security, and it catches almost everyone once.
