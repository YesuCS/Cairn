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

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Lava;
using Rock.Model;

namespace com.yesuchum.Cairn.FollowingEvents.Follow.Event
{
    /// <summary>
    /// Fires once when the followed group was inactivated or archived, with the change
    /// inside the window.
    /// </summary>
    [DisplayName( "Group Inactivated or Archived (cairn plugin)" )]
    [Description( "Group Inactivated or Archived (cairn plugin)" )]
    [Export( typeof( Rock.Follow.EventComponent ) )]
    [ExportMetadata( "ComponentName", "GroupInactivated" )]

    [IntegerField( "Max Days Back", "Maximum number of days back to consider.", false, 30, "", 0, AttributeKey.MaxDaysBack )]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.GROUP_INACTIVATED )]
    public class GroupInactivated : CairnEventComponent
    {
        private static class AttributeKey
        {
            public const string MaxDaysBack = "MaxDaysBack";
        }

        /// <inheritdoc/>
        public override string DefaultNotificationFormat
        {
            get
            {
                return BasicNotificationRow(
                    GroupLinkLava + " was {{ EventData.ChangeType | Downcase }} on {{ EventData.ChangeDateTime | Date:'dddd, MMMM d' }}" );
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

            // Once ever.
            if ( lastNotified.HasValue )
            {
                return false;
            }

            var group = entity as Group;
            if ( followingEvent == null || group == null )
            {
                return false;
            }

            int daysBack = GetAttributeValue( followingEvent, AttributeKey.MaxDaysBack ).AsInteger();
            var cutoff = RockDateTime.Today.AddDays( -daysBack );

            string changeType = null;
            DateTime? changeDateTime = null;

            if ( group.IsArchived && group.ArchivedDateTime.HasValue && group.ArchivedDateTime > cutoff )
            {
                changeType = "Archived";
                changeDateTime = group.ArchivedDateTime;
            }
            else if ( !group.IsActive && group.ModifiedDateTime.HasValue && group.ModifiedDateTime > cutoff )
            {
                // Rock stores no inactivated timestamp on Group; the last-modified time
                // stands in, so an unrelated later edit can fall outside the window.
                changeType = "Inactivated";
                changeDateTime = group.ModifiedDateTime;
            }

            if ( changeType == null )
            {
                return false;
            }

            followedEventObjects.Add( "EventData", new List<object>
            {
                new GroupChangeData
                {
                    SourceName = group.Name,
                    ChangeType = changeType,
                    ChangeDateTime = changeDateTime
                }
            } );
            return true;
        }

        /// <summary>
        /// Merge object for the notification template.
        /// </summary>
        [Serializable]
        public class GroupChangeData : LavaDataObject
        {
            /// <summary>The group's name.</summary>
            public string SourceName { get; set; }

            /// <summary>"Inactivated" or "Archived".</summary>
            public string ChangeType { get; set; }

            /// <summary>When the change happened (best available signal).</summary>
            public DateTime? ChangeDateTime { get; set; }
        }
    }
}
