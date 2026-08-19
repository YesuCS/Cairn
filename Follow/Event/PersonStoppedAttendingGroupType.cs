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
    /// Fires when a person who had been attending groups of the configured type(s) has
    /// no attendance in any of them for the threshold number of days. Someone who never
    /// attended is not a lapse and never fires.
    /// </summary>
    [DisplayName( "Person Stopped Attending Group Type(s) (cairn plugin)" )]
    [Description( "Person Stopped Attending Group Type(s) (cairn plugin)" )]
    [Export( typeof( Rock.Follow.EventComponent ) )]
    [ExportMetadata( "ComponentName", "PersonStoppedAttendingGroupType" )]

    [GroupTypesField( "Group Type(s)", "The group type(s) whose attendance is watched.", true, "", "", 0, AttributeKey.GroupTypes )]
    [IntegerField( "Days Without Attendance", "How many days without attendance counts as stopped.", false, 30, "", 1, AttributeKey.DaysWithoutAttendance )]
    [IntegerField( "Re-notify Days", "Days before notifying again while the lapse continues (0 = notify once ever).", false, 0, "", 2, AttributeKey.RenotifyDays )]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.PERSON_STOPPED_ATTENDING_GROUP_TYPE )]
    public class PersonStoppedAttendingGroupType : CairnEventComponent
    {
        private static class AttributeKey
        {
            public const string GroupTypes = "GroupTypes";
            public const string DaysWithoutAttendance = "DaysWithoutAttendance";
            public const string RenotifyDays = "RenotifyDays";
        }

        /// <inheritdoc/>
        public override string DefaultNotificationFormat
        {
            get
            {
                return PersonNotificationRow( PersonLinkLava + " hasn't attended {{ EventData.GroupName }} since {{ EventData.LastAttendedDateTime | Date:'dddd, MMMM d' }} ({{ EventData.DaysSinceAttended }} days)" );
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
            var groupTypeGuids = GetAttributeValue( followingEvent, AttributeKey.GroupTypes ).SplitDelimitedValues().AsGuidList();
            if ( followingEvent == null || personAlias == null || !groupTypeGuids.Any() )
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

            int daysWithout = GetAttributeValue( followingEvent, AttributeKey.DaysWithoutAttendance ).AsIntegerOrNull() ?? 30;

            using ( var rockContext = new RockContext() )
            {
                var lastAttendance = new AttendanceService( rockContext )
                    .Queryable().AsNoTracking()
                    .Where( a =>
                        a.PersonAlias.PersonId == personAlias.PersonId &&
                        a.DidAttend == true &&
                        a.Occurrence.Group != null &&
                        groupTypeGuids.Contains( a.Occurrence.Group.GroupType.Guid ) )
                    .OrderByDescending( a => a.StartDateTime )
                    .Select( a => new
                    {
                        a.StartDateTime,
                        GroupName = a.Occurrence.Group.Name,
                        GroupTypeName = a.Occurrence.Group.GroupType.Name
                    } )
                    .FirstOrDefault();

                // Never attended is not a lapse.
                if ( lastAttendance == null )
                {
                    return false;
                }

                int daysSince = RockDateTime.Today.Subtract( lastAttendance.StartDateTime.Date ).Days;
                if ( daysSince < daysWithout )
                {
                    return false;
                }

                followedEventObjects.Add( "EventData", new List<object>
                {
                    new LapseData
                    {
                        SourceName = lastAttendance.GroupTypeName,
                        GroupName = lastAttendance.GroupName,
                        LastAttendedDateTime = lastAttendance.StartDateTime,
                        DaysSinceAttended = daysSince
                    }
                } );
                return true;
            }
        }

        /// <summary>
        /// Merge object for the notification template.
        /// </summary>
        [Serializable]
        public class LapseData : LavaDataObject
        {
            /// <summary>The group type of the last-attended group.</summary>
            public string SourceName { get; set; }

            /// <summary>The group last attended.</summary>
            public string GroupName { get; set; }

            /// <summary>When they last attended.</summary>
            public DateTime LastAttendedDateTime { get; set; }

            /// <summary>Whole days since the last attendance.</summary>
            public int DaysSinceAttended { get; set; }
        }
    }
}
