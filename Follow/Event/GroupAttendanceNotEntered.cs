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
    /// Fires when a past attendance occurrence for the followed group is older than the
    /// threshold with no attendance recorded and Did Not Occur not set.
    /// </summary>
    [DisplayName( "Group Attendance Not Entered (cairn plugin)" )]
    [Description( "Group Attendance Not Entered (cairn plugin)" )]
    [Export( typeof( Rock.Follow.EventComponent ) )]
    [ExportMetadata( "ComponentName", "GroupAttendanceNotEntered" )]

    [IntegerField( "Days After Occurrence", "How many days after the occurrence date attendance counts as overdue.", false, 2, "", 0, AttributeKey.DaysAfterOccurrence )]
    [IntegerField( "Re-notify Days", "Days before notifying again while attendance is still missing (0 = notify once ever).", false, 7, "", 1, AttributeKey.RenotifyDays )]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.GROUP_ATTENDANCE_NOT_ENTERED )]
    public class GroupAttendanceNotEntered : CairnEventComponent
    {
        private static class AttributeKey
        {
            public const string DaysAfterOccurrence = "DaysAfterOccurrence";
            public const string RenotifyDays = "RenotifyDays";
        }

        /// <inheritdoc/>
        public override string DefaultNotificationFormat
        {
            get
            {
                return BasicNotificationRow(
                    "{{ Entity.Name }} has no attendance entered for {{ OccurrenceData.OccurrenceDate | Date:'dddd, MMMM d' }}",
                    "{% if OccurrenceData.ScheduleName != '' %}Schedule: {{ OccurrenceData.ScheduleName }}<br />{% endif %}" );
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

            // Re-notify interval dedupe: 0 collapses to once-ever.
            if ( lastNotified.HasValue )
            {
                int renotifyDays = GetAttributeValue( followingEvent, AttributeKey.RenotifyDays ).AsInteger();
                if ( renotifyDays <= 0 || RockDateTime.Today.Subtract( lastNotified.Value.Date ).Days < renotifyDays )
                {
                    return false;
                }
            }

            int daysAfter = GetAttributeValue( followingEvent, AttributeKey.DaysAfterOccurrence ).AsInteger();
            var overdueBefore = RockDateTime.Today.AddDays( -daysAfter );

            using ( var rockContext = new RockContext() )
            {
                var missing = new AttendanceOccurrenceService( rockContext )
                    .Queryable().AsNoTracking()
                    .Where( o =>
                        o.GroupId == group.Id &&
                        o.OccurrenceDate < overdueBefore &&
                        o.DidNotOccur != true &&
                        !o.Attendees.Any( a => a.DidAttend.HasValue ) )
                    .Select( o => new
                    {
                        o.OccurrenceDate,
                        ScheduleName = o.Schedule != null ? o.Schedule.Name : null
                    } )
                    .ToList();

                if ( !missing.Any() )
                {
                    return false;
                }

                followedEventObjects.Add( "OccurrenceData", new List<object>( missing.Select( o => new OccurrenceData
                {
                    OccurrenceDate = o.OccurrenceDate,
                    ScheduleName = o.ScheduleName ?? string.Empty
                } ) ) );
                return true;
            }
        }

        /// <summary>
        /// Merge object for the notification template, one per missing occurrence.
        /// </summary>
        [Serializable]
        public class OccurrenceData : LavaDataObject
        {
            /// <summary>The occurrence date missing attendance.</summary>
            public DateTime OccurrenceDate { get; set; }

            /// <summary>The occurrence's schedule name, if any.</summary>
            public string ScheduleName { get; set; }
        }
    }
}
