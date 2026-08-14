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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Web.Cache;

namespace com.yesuchum.Cairn.FollowingEvents.Follow.Event
{
    /// <summary>
    /// Fires when the followed person's membership in the configured group type went
    /// Inactive or was archived within the window. Hard-deleted memberships leave no
    /// exit signal and are not detected.
    /// </summary>
    [DisplayName( "Person Left Group of Type (cairn plugin)" )]
    [Description( "Person Left Group of Type (cairn plugin)" )]
    [Export( typeof( Rock.Follow.EventComponent ) )]
    [ExportMetadata( "ComponentName", "PersonLeftGroupType" )]

    [GroupTypeField( "Group Type", "The group type to watch for exits.", true, "", "", 0, AttributeKey.GroupType )]
    [IntegerField( "Max Days Back", "Maximum number of days back to consider.", false, 30, "", 1, AttributeKey.MaxDaysBack )]
    [BooleanField( "Only If No Remaining Membership", "Only notify when the person has no remaining active membership in any group of this type.", true, "", 2, AttributeKey.OnlyIfNoRemainingMembership )]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.PERSON_LEFT_GROUP_TYPE )]
    public class PersonLeftGroupType : CairnEventComponent
    {
        private static class AttributeKey
        {
            public const string GroupType = "GroupType";
            public const string MaxDaysBack = "MaxDaysBack";
            public const string OnlyIfNoRemainingMembership = "OnlyIfNoRemainingMembership";
        }

        /// <inheritdoc/>
        public override string DefaultNotificationFormat
        {
            get
            {
                return PersonNotificationRow( PersonLinkLava + " left {{ EventData.GroupName }} ({{ EventData.SourceName }}) on {{ EventData.ExitDateTime | Date:'dddd, MMMM d' }}" );
            }
        }

        /// <inheritdoc/>
        public override Type FollowedType
        {
            get { return typeof( Rock.Model.PersonAlias ); }
        }

        /// <inheritdoc/>
        public override bool HasEventHappened( FollowingEventType followingEvent, IEntity entity, DateTime? lastNotified, out Dictionary<string, List<object>> followedEventObjects )
        {
            followedEventObjects = new Dictionary<string, List<object>>();

            var personAlias = entity as PersonAlias;
            var groupTypeGuid = GetAttributeValue( followingEvent, AttributeKey.GroupType ).AsGuidOrNull();
            if ( followingEvent == null || personAlias == null || !groupTypeGuid.HasValue )
            {
                return false;
            }

            // Trailing cutoff dedupe: later of (today - daysBack) and lastNotified.
            int daysBack = GetAttributeValue( followingEvent, AttributeKey.MaxDaysBack ).AsInteger();
            var cutoff = RockDateTime.Today.AddDays( -daysBack );
            if ( lastNotified.HasValue && lastNotified.Value > cutoff )
            {
                cutoff = lastNotified.Value;
            }

            using ( var rockContext = new RockContext() )
            {
                var memberQry = new GroupMemberService( rockContext )
                    .Queryable( true ).AsNoTracking()
                    .Where( m =>
                        m.PersonId == personAlias.PersonId &&
                        m.Group.GroupType.Guid == groupTypeGuid.Value );

                var exited = memberQry
                    .Where( m =>
                        ( m.GroupMemberStatus == GroupMemberStatus.Inactive && m.InactiveDateTime.HasValue && m.InactiveDateTime > cutoff ) ||
                        ( m.IsArchived && m.ArchivedDateTime.HasValue && m.ArchivedDateTime > cutoff ) )
                    .Select( m => new
                    {
                        GroupName = m.Group.Name,
                        ExitDateTime = m.IsArchived ? m.ArchivedDateTime : m.InactiveDateTime
                    } )
                    .ToList();

                if ( !exited.Any() )
                {
                    return false;
                }

                if ( GetAttributeValue( followingEvent, AttributeKey.OnlyIfNoRemainingMembership ).AsBoolean( true ) )
                {
                    bool hasRemaining = memberQry.Any( m => m.GroupMemberStatus == GroupMemberStatus.Active && !m.IsArchived );
                    if ( hasRemaining )
                    {
                        return false;
                    }
                }

                var groupTypeName = GroupTypeCache.Get( groupTypeGuid.Value )?.Name ?? string.Empty;
                followedEventObjects.Add( "EventData", new List<object>( exited.Select( e => new LeftGroupData
                {
                    SourceName = groupTypeName,
                    GroupName = e.GroupName,
                    ExitDateTime = e.ExitDateTime
                } ) ) );
                return true;
            }
        }

        /// <summary>
        /// Merge object for the notification template, one per exited membership.
        /// </summary>
        [Serializable]
        public class LeftGroupData : LavaDataObject
        {
            /// <summary>The group type's name.</summary>
            public string SourceName { get; set; }

            /// <summary>The group the person left.</summary>
            public string GroupName { get; set; }

            /// <summary>When the membership went inactive or was archived.</summary>
            public DateTime? ExitDateTime { get; set; }
        }
    }
}
