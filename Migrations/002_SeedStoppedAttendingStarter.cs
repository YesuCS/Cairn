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

using com.yesuchum.Cairn.FollowingEvents.Follow.Event;

namespace com.yesuchum.Cairn.FollowingEvents.Migrations
{
    /// <summary>
    /// v1.0.1: seeds the inactive starter for Person Stopped Attending Group Type(s).
    /// Migration 001 is shipped and immutable; new starters get new migrations.
    /// </summary>
    [MigrationNumber( 2, "1.18.2" )]
    public class SeedStoppedAttendingStarter : Migration
    {
        private const string STARTER_GUID = "DE8B96B7-BAF8-4904-9607-8CDF3765EC89";

        public override void Up()
        {
            var component = new PersonStoppedAttendingGroupType();
            var componentTypeName = component.GetType().FullName;
            var template = ( component.DefaultNotificationFormat ?? string.Empty ).Replace( "'", "''" );

            RockMigrationHelper.UpdateEntityType( componentTypeName, SystemGuid.EntityType.PERSON_STOPPED_ATTENDING_GROUP_TYPE, false, true );

            Sql( string.Format( @"
IF NOT EXISTS ( SELECT 1 FROM [FollowingEventType] WHERE [Guid] = '{0}' )
BEGIN
    INSERT INTO [FollowingEventType]
        ( [Name], [Description], [EntityTypeId], [FollowedEntityTypeId], [IsActive], [SendOnWeekends], [IsNoticeRequired], [EntityNotificationFormatLava], [Order], [IncludeNonPublicRequests], [Guid], [CreatedDateTime] )
    VALUES
        ( 'Stopped Attending Group Type(s) (cairn starter)'
        , 'Notifies followers when a person who had been attending groups of the chosen type(s) has no attendance for the threshold number of days. Someone who never attended does not count as a lapse.'
        , ( SELECT TOP 1 [Id] FROM [EntityType] WHERE [Guid] = '{1}' )
        , ( SELECT TOP 1 [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.PersonAlias' )
        , 0, 0, 0
        , '{2}'
        , 0, 0
        , '{0}', GETDATE() );
END",
                STARTER_GUID,
                SystemGuid.EntityType.PERSON_STOPPED_ATTENDING_GROUP_TYPE,
                template ) );
        }

        public override void Down()
        {
            Sql( string.Format( @"
DELETE FROM [FollowingEventType]
WHERE [Guid] = '{0}' AND [IsActive] = 0 AND [Name] LIKE '%(cairn starter)';",
                STARTER_GUID ) );
        }
    }
}
