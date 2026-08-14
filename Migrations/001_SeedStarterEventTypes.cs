// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using Rock.Plugin;

using com.yesuchum.Cairn.FollowingEvents.Follow;
using com.yesuchum.Cairn.FollowingEvents.Follow.Event;

namespace com.yesuchum.Cairn.FollowingEvents.Migrations
{
    /// <summary>
    /// Seeds one inactive starter event type per component, template baked in, so staff
    /// edit or copy a visible record instead of writing Lava from scratch. Config rows
    /// only; no schema. Deleting a starter is safe — this migration runs exactly once
    /// and never resurrects them.
    /// </summary>
    [MigrationNumber( 1, "1.18.2" )]
    public class SeedStarterEventTypes : Migration
    {
        /// <summary>
        /// The pinned guids for the seeded starter event type records.
        /// </summary>
        private static class StarterGuid
        {
            public const string DATE_ATTRIBUTE_ANNIVERSARY = "2A76E0B0-9D86-4E33-AA1B-687CB7FF29BD";
            public const string MILESTONE_BIRTHDAY = "4DDBA8E4-00A5-4194-9756-7DE3584144C4";
            public const string SERVING_ANNIVERSARY = "272FF36F-215F-46F1-9100-574DAB6696F2";
            public const string DATA_VIEW_MATCH = "2AABF260-64F4-4781-921D-7788CAC832E0";
            public const string LEFT_GROUP_TYPE = "EE708E84-4267-46F1-BB98-3F2DF05FC9CA";
            public const string ENTERED_CONNECTION_OPPORTUNITY = "AE919166-9BBC-4757-A32C-DB491AB8C591";
            public const string CONNECTION_REQUEST_CONNECTED = "590DD384-5D6A-4128-8367-15539F63A52E";
            public const string GROUP_MEMBER_ADDED = "F6E34B21-9625-4DA0-99A1-35C36020BA62";
            public const string GROUP_MEMBER_REMOVED = "C52AB45B-9237-41FC-BD24-509DAD8E67A2";
            public const string GROUP_ATTENDANCE_NOT_ENTERED = "019AB6F8-E48A-438E-9521-825CF2181EF9";
            public const string GROUP_INACTIVATED = "87D7F21F-E2F7-45F8-A8D3-30CD83262274";
            public const string REGISTRATION_NEW_REGISTRATION = "D859AC5E-2E2F-4CCB-9ECA-EC5D7869CA8D";
            public const string REGISTRATION_NEARING_CAPACITY = "675F6185-763B-40BC-BFC8-E7F4521387B6";
            public const string REGISTRATION_CLOSING_SOON = "7D6F694A-9771-4C8A-9DC5-4A3F8B996662";
        }

        public override void Up()
        {
            SeedEventType( StarterGuid.DATE_ATTRIBUTE_ANNIVERSARY, SystemGuid.EntityType.PERSON_DATE_ATTRIBUTE_ANNIVERSARY, "Rock.Model.PersonAlias",
                "Date Attribute Anniversary (cairn starter)",
                "Notifies followers ahead of the annual anniversary of a date attribute on a person. Pick the attribute and lead days, then activate.",
                new PersonDateAttributeAnniversary() );
            SeedEventType( StarterGuid.MILESTONE_BIRTHDAY, SystemGuid.EntityType.PERSON_MILESTONE_BIRTHDAY, "Rock.Model.PersonAlias",
                "Milestone Birthday (cairn starter)",
                "Notifies followers ahead of a person's birthday. Set Nth Year to only hear about milestones (10th, 20th...).",
                new PersonMilestoneBirthday() );
            SeedEventType( StarterGuid.SERVING_ANNIVERSARY, SystemGuid.EntityType.PERSON_SERVING_ANNIVERSARY, "Rock.Model.PersonAlias",
                "Serving Anniversary (cairn starter)",
                "Notifies followers ahead of the anniversary of a person's first join into a group of the chosen type.",
                new PersonServingAnniversary() );
            SeedEventType( StarterGuid.DATA_VIEW_MATCH, SystemGuid.EntityType.PERSON_DATA_VIEW_MATCH, "Rock.Model.PersonAlias",
                "Data View Match (cairn starter)",
                "Notifies followers while a person is in the chosen data view. The data view must be persisted.",
                new PersonDataViewMatch() );
            SeedEventType( StarterGuid.LEFT_GROUP_TYPE, SystemGuid.EntityType.PERSON_LEFT_GROUP_TYPE, "Rock.Model.PersonAlias",
                "Left Group of Type (cairn starter)",
                "Notifies followers when a person's membership in the chosen group type goes inactive or is archived.",
                new PersonLeftGroupType() );
            SeedEventType( StarterGuid.ENTERED_CONNECTION_OPPORTUNITY, SystemGuid.EntityType.PERSON_ENTERED_CONNECTION_OPPORTUNITY, "Rock.Model.PersonAlias",
                "Entered Connection Opportunity (cairn starter)",
                "Notifies followers when a connection request is created for a person, filtered by opportunity or connection type.",
                new PersonEnteredConnectionOpportunity() );
            SeedEventType( StarterGuid.CONNECTION_REQUEST_CONNECTED, SystemGuid.EntityType.PERSON_CONNECTION_REQUEST_CONNECTED, "Rock.Model.PersonAlias",
                "Connection Request Connected (cairn starter)",
                "Notifies followers once when a person's connection request reaches Connected.",
                new PersonConnectionRequestConnected() );
            SeedEventType( StarterGuid.GROUP_MEMBER_ADDED, SystemGuid.EntityType.GROUP_MEMBER_ADDED, "Rock.Model.Group",
                "Group Member Added (cairn starter)",
                "Notifies a group's followers when someone is added to the group.",
                new GroupMemberAdded() );
            SeedEventType( StarterGuid.GROUP_MEMBER_REMOVED, SystemGuid.EntityType.GROUP_MEMBER_REMOVED, "Rock.Model.Group",
                "Group Member Removed (cairn starter)",
                "Notifies a group's followers when a membership goes inactive or is archived.",
                new GroupMemberRemoved() );
            SeedEventType( StarterGuid.GROUP_ATTENDANCE_NOT_ENTERED, SystemGuid.EntityType.GROUP_ATTENDANCE_NOT_ENTERED, "Rock.Model.Group",
                "Group Attendance Not Entered (cairn starter)",
                "Notifies a group's followers when a past occurrence still has no attendance entered.",
                new GroupAttendanceNotEntered() );
            SeedEventType( StarterGuid.GROUP_INACTIVATED, SystemGuid.EntityType.GROUP_INACTIVATED, "Rock.Model.Group",
                "Group Inactivated or Archived (cairn starter)",
                "Notifies a group's followers once when the group is inactivated or archived.",
                new GroupInactivated() );
            SeedEventType( StarterGuid.REGISTRATION_NEW_REGISTRATION, SystemGuid.EntityType.REGISTRATION_NEW_REGISTRATION, "Rock.Model.RegistrationInstance",
                "Registration New Registration (cairn starter)",
                "Notifies a registration instance's followers when a new registration comes in.",
                new RegistrationNewRegistration() );
            SeedEventType( StarterGuid.REGISTRATION_NEARING_CAPACITY, SystemGuid.EntityType.REGISTRATION_NEARING_CAPACITY, "Rock.Model.RegistrationInstance",
                "Registration Nearing Capacity (cairn starter)",
                "Notifies a registration instance's followers when registrations reach a threshold percent of capacity.",
                new RegistrationNearingCapacity() );
            SeedEventType( StarterGuid.REGISTRATION_CLOSING_SOON, SystemGuid.EntityType.REGISTRATION_CLOSING_SOON, "Rock.Model.RegistrationInstance",
                "Registration Closing Soon (cairn starter)",
                "Notifies a registration instance's followers shortly before registration closes.",
                new RegistrationClosingSoon() );
        }

        public override void Down()
        {
            // Remove only starters that were never activated or renamed; anything staff
            // put into service stays.
            Sql( @"
DELETE FROM [FollowingEventType]
WHERE [Guid] IN (
    '2A76E0B0-9D86-4E33-AA1B-687CB7FF29BD','4DDBA8E4-00A5-4194-9756-7DE3584144C4','272FF36F-215F-46F1-9100-574DAB6696F2',
    '2AABF260-64F4-4781-921D-7788CAC832E0','EE708E84-4267-46F1-BB98-3F2DF05FC9CA','AE919166-9BBC-4757-A32C-DB491AB8C591',
    '590DD384-5D6A-4128-8367-15539F63A52E','F6E34B21-9625-4DA0-99A1-35C36020BA62','C52AB45B-9237-41FC-BD24-509DAD8E67A2',
    '019AB6F8-E48A-438E-9521-825CF2181EF9','87D7F21F-E2F7-45F8-A8D3-30CD83262274','D859AC5E-2E2F-4CCB-9ECA-EC5D7869CA8D',
    '675F6185-763B-40BC-BFC8-E7F4521387B6','7D6F694A-9771-4C8A-9DC5-4A3F8B996662'
) AND [IsActive] = 0 AND [Name] LIKE '%(cairn starter)';" );
        }

        /// <summary>
        /// Inserts one inactive starter event type for a component, template baked in.
        /// The component's EntityType row is ensured first so this works on a fresh
        /// install where startup discovery has not yet run.
        /// </summary>
        private void SeedEventType( string starterGuid, string componentEntityTypeGuid, string followedEntityTypeName, string name, string description, CairnEventComponent component )
        {
            var componentTypeName = component.GetType().FullName;
            var template = ( component.DefaultNotificationFormat ?? string.Empty ).Replace( "'", "''" );

            RockMigrationHelper.UpdateEntityType( componentTypeName, componentEntityTypeGuid, false, true );

            Sql( string.Format( @"
IF NOT EXISTS ( SELECT 1 FROM [FollowingEventType] WHERE [Guid] = '{0}' )
BEGIN
    INSERT INTO [FollowingEventType]
        ( [Name], [Description], [EntityTypeId], [FollowedEntityTypeId], [IsActive], [SendOnWeekends], [IsNoticeRequired], [EntityNotificationFormatLava], [Order], [IncludeNonPublicRequests], [Guid], [CreatedDateTime] )
    VALUES
        ( '{1}'
        , '{5}'
        , ( SELECT TOP 1 [Id] FROM [EntityType] WHERE [Guid] = '{2}' )
        , ( SELECT TOP 1 [Id] FROM [EntityType] WHERE [Name] = '{3}' )
        , 0, 0, 0
        , '{4}'
        , 0, 0
        , '{0}', GETDATE() );
END",
                starterGuid,
                name.Replace( "'", "''" ),
                componentEntityTypeGuid,
                followedEntityTypeName,
                template,
                description.Replace( "'", "''" ) ) );
        }
    }
}
