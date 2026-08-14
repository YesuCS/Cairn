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
    /// Fires when a member was added to the followed group within the window.
    /// </summary>
    [DisplayName( "Group Member Added (cairn plugin)" )]
    [Description( "Group Member Added (cairn plugin)" )]
    [Export( typeof( Rock.Follow.EventComponent ) )]
    [ExportMetadata( "ComponentName", "GroupMemberAdded" )]

    [IntegerField( "Max Days Back", "Maximum number of days back to consider.", false, 7, "", 0, AttributeKey.MaxDaysBack )]
    [BooleanField( "Include Pending", "Also notify for members added in Pending status.", false, "", 1, AttributeKey.IncludePending )]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.GROUP_MEMBER_ADDED )]
    public class GroupMemberAdded : CairnEventComponent
    {
        private static class AttributeKey
        {
            public const string MaxDaysBack = "MaxDaysBack";
            public const string IncludePending = "IncludePending";
        }

        /// <inheritdoc/>
        protected override string DefaultNotificationFormat
        {
            get
            {
                return "<p>{{ MemberData.MemberName }} was added to {{ Entity.Name }} as {{ MemberData.RoleName }} on {{ MemberData.AddedDateTime | Date:'MMMM d' }}.</p>";
            }
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

            bool includePending = GetAttributeValue( followingEvent, AttributeKey.IncludePending ).AsBoolean();

            using ( var rockContext = new RockContext() )
            {
                var addedQry = new GroupMemberService( rockContext )
                    .Queryable().AsNoTracking()
                    .Where( m =>
                        m.GroupId == group.Id &&
                        m.DateTimeAdded.HasValue &&
                        m.DateTimeAdded > cutoff );

                if ( includePending )
                {
                    addedQry = addedQry.Where( m => m.GroupMemberStatus == GroupMemberStatus.Active || m.GroupMemberStatus == GroupMemberStatus.Pending );
                }
                else
                {
                    addedQry = addedQry.Where( m => m.GroupMemberStatus == GroupMemberStatus.Active );
                }

                var added = addedQry
                    .Select( m => new
                    {
                        m.Person.NickName,
                        m.Person.LastName,
                        RoleName = m.GroupRole.Name,
                        m.DateTimeAdded
                    } )
                    .ToList();

                if ( !added.Any() )
                {
                    return false;
                }

                followedEventObjects.Add( "MemberData", new List<object>( added.Select( m => new MemberData
                {
                    MemberName = m.NickName + " " + m.LastName,
                    RoleName = m.RoleName,
                    AddedDateTime = m.DateTimeAdded
                } ) ) );
                return true;
            }
        }

        /// <summary>
        /// Merge object for the notification template, one per added member.
        /// </summary>
        [Serializable]
        public class MemberData : LavaDataObject
        {
            /// <summary>The added member's name.</summary>
            public string MemberName { get; set; }

            /// <summary>The member's role in the group.</summary>
            public string RoleName { get; set; }

            /// <summary>When the member was added.</summary>
            public DateTime? AddedDateTime { get; set; }
        }
    }
}
