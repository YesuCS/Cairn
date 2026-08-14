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

namespace com.yesuchum.Cairn.FollowingEvents.Follow.Event
{
    /// <summary>
    /// Fires when a membership in the followed group went Inactive or was archived
    /// within the window. Hard-deleted memberships leave no exit signal and are not
    /// detected.
    /// </summary>
    [DisplayName( "Group Member Removed (cairn plugin)" )]
    [Description( "Group Member Removed (cairn plugin)" )]
    [Export( typeof( Rock.Follow.EventComponent ) )]
    [ExportMetadata( "ComponentName", "GroupMemberRemoved" )]

    [IntegerField( "Max Days Back", "Maximum number of days back to consider.", false, 7, "", 0, AttributeKey.MaxDaysBack )]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.GROUP_MEMBER_REMOVED )]
    public class GroupMemberRemoved : CairnEventComponent
    {
        private static class AttributeKey
        {
            public const string MaxDaysBack = "MaxDaysBack";
        }

        /// <inheritdoc/>
        public override Type FollowedType
        {
            get { return typeof( Rock.Model.Group ); }
        }

        /// <inheritdoc/>
        public override bool HasEventHappened( FollowingEventType followingEvent, IEntity entity, DateTime? lastNotified, out Dictionary<string, List<object>> followedEventObjects )
        {
            followedEventObjects = new Dictionary<string, List<object>>();

            var group = entity as Group;
            if ( followingEvent == null || group == null )
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
                var removed = new GroupMemberService( rockContext )
                    .Queryable( true ).AsNoTracking()
                    .Where( m =>
                        m.GroupId == group.Id &&
                        ( ( m.GroupMemberStatus == GroupMemberStatus.Inactive && m.InactiveDateTime.HasValue && m.InactiveDateTime > cutoff ) ||
                          ( m.IsArchived && m.ArchivedDateTime.HasValue && m.ArchivedDateTime > cutoff ) ) )
                    .Select( m => new
                    {
                        m.Person.NickName,
                        m.Person.LastName,
                        RoleName = m.GroupRole.Name,
                        ExitDateTime = m.IsArchived ? m.ArchivedDateTime : m.InactiveDateTime
                    } )
                    .ToList();

                if ( !removed.Any() )
                {
                    return false;
                }

                followedEventObjects.Add( "MemberData", new List<object>( removed.Select( m => new MemberData
                {
                    MemberName = m.NickName + " " + m.LastName,
                    RoleName = m.RoleName,
                    ExitDateTime = m.ExitDateTime
                } ) ) );
                return true;
            }
        }

        /// <summary>
        /// Merge object for the notification template, one per removed member.
        /// </summary>
        [Serializable]
        public class MemberData : LavaDataObject
        {
            /// <summary>The removed member's name.</summary>
            public string MemberName { get; set; }

            /// <summary>The member's role in the group.</summary>
            public string RoleName { get; set; }

            /// <summary>When the membership went inactive or was archived.</summary>
            public DateTime? ExitDateTime { get; set; }
        }
    }
}
