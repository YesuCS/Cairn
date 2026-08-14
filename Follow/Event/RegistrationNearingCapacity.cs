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
    /// Fires when the followed registration instance's registrant count reaches the
    /// threshold percent of Max Attendees. Instances with no Max Attendees never fire.
    /// </summary>
    [DisplayName( "Registration Nearing Capacity (cairn plugin)" )]
    [Description( "Registration Nearing Capacity (cairn plugin)" )]
    [Export( typeof( Rock.Follow.EventComponent ) )]
    [ExportMetadata( "ComponentName", "RegistrationNearingCapacity" )]

    [IntegerField( "Threshold Percent", "Fire when registrants reach this percent of Max Attendees.", false, 90, "", 0, AttributeKey.ThresholdPercent )]
    [IntegerField( "Re-notify Days", "Days before notifying again while still at or above the threshold (0 = notify once ever).", false, 0, "", 1, AttributeKey.RenotifyDays )]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.REGISTRATION_NEARING_CAPACITY )]
    public class RegistrationNearingCapacity : CairnEventComponent
    {
        private static class AttributeKey
        {
            public const string ThresholdPercent = "ThresholdPercent";
            public const string RenotifyDays = "RenotifyDays";
        }

        /// <inheritdoc/>
        public override Type FollowedType
        {
            get { return typeof( Rock.Model.RegistrationInstance ); }
        }

        /// <inheritdoc/>
        public override bool HasEventHappened( FollowingEventType followingEvent, IEntity entity, DateTime? lastNotified, out Dictionary<string, List<object>> followedEventObjects )
        {
            followedEventObjects = new Dictionary<string, List<object>>();

            var instance = entity as RegistrationInstance;
            if ( followingEvent == null || instance == null || !instance.MaxAttendees.HasValue || instance.MaxAttendees.Value <= 0 )
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

            int thresholdPercent = GetAttributeValue( followingEvent, AttributeKey.ThresholdPercent ).AsIntegerOrNull() ?? 90;

            using ( var rockContext = new RockContext() )
            {
                int registrantCount = new RegistrationRegistrantService( rockContext )
                    .Queryable().AsNoTracking()
                    .Count( g =>
                        g.Registration.RegistrationInstanceId == instance.Id &&
                        !g.OnWaitList );

                int percentFull = ( int ) Math.Floor( registrantCount * 100m / instance.MaxAttendees.Value );
                if ( percentFull < thresholdPercent )
                {
                    return false;
                }

                followedEventObjects.Add( "EventData", new List<object>
                {
                    new CapacityData
                    {
                        SourceName = instance.Name,
                        RegistrantCount = registrantCount,
                        MaxAttendees = instance.MaxAttendees.Value,
                        PercentFull = percentFull
                    }
                } );
                return true;
            }
        }

        /// <summary>
        /// Merge object for the notification template.
        /// </summary>
        [Serializable]
        public class CapacityData : LavaDataObject
        {
            /// <summary>The registration instance's name.</summary>
            public string SourceName { get; set; }

            /// <summary>Current non-waitlist registrant count.</summary>
            public int RegistrantCount { get; set; }

            /// <summary>The instance's Max Attendees.</summary>
            public int MaxAttendees { get; set; }

            /// <summary>Whole-number percent full.</summary>
            public int PercentFull { get; set; }
        }
    }
}
