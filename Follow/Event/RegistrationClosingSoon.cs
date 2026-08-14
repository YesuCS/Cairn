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
    /// Fires once when the followed registration instance's registration end date falls
    /// within Lead Days. Instances with no end date never fire.
    /// </summary>
    [DisplayName( "Registration Closing Soon (cairn plugin)" )]
    [Description( "Registration Closing Soon (cairn plugin)" )]
    [Export( typeof( Rock.Follow.EventComponent ) )]
    [ExportMetadata( "ComponentName", "RegistrationClosingSoon" )]

    [IntegerField( "Lead Days", "The number of days before the registration close date that the notification should be sent.", false, 5, "", 0, AttributeKey.LeadDays )]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.REGISTRATION_CLOSING_SOON )]
    public class RegistrationClosingSoon : CairnEventComponent
    {
        private static class AttributeKey
        {
            public const string LeadDays = "LeadDays";
        }

        /// <inheritdoc/>
        protected override string DefaultNotificationFormat
        {
            get
            {
                return "<p>{{ EventData.SourceName }} registration closes {{ EventData.CloseDate | Date:'dddd, MMMM d' }} — {{ EventData.DaysRemaining }} days left, {{ EventData.RegistrantCount }} registered.</p>";
            }
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

            // Once ever (per follow).
            if ( lastNotified.HasValue )
            {
                return false;
            }

            var instance = entity as RegistrationInstance;
            if ( followingEvent == null || instance == null || !instance.EndDateTime.HasValue )
            {
                return false;
            }

            var closeDate = instance.EndDateTime.Value.Date;
            var today = RockDateTime.Today;
            int leadDays = GetAttributeValue( followingEvent, AttributeKey.LeadDays ).AsInteger();

            int daysRemaining = closeDate.Subtract( today ).Days;
            if ( daysRemaining < 0 || daysRemaining > leadDays )
            {
                return false;
            }

            using ( var rockContext = new RockContext() )
            {
                int registrantCount = new RegistrationRegistrantService( rockContext )
                    .Queryable().AsNoTracking()
                    .Count( g =>
                        g.Registration.RegistrationInstanceId == instance.Id &&
                        !g.OnWaitList );

                followedEventObjects.Add( "EventData", new List<object>
                {
                    new ClosingData
                    {
                        SourceName = instance.Name,
                        CloseDate = closeDate,
                        DaysRemaining = daysRemaining,
                        RegistrantCount = registrantCount
                    }
                } );
            }

            return true;
        }

        /// <summary>
        /// Merge object for the notification template.
        /// </summary>
        [Serializable]
        public class ClosingData : LavaDataObject
        {
            /// <summary>The registration instance's name.</summary>
            public string SourceName { get; set; }

            /// <summary>When registration closes.</summary>
            public DateTime CloseDate { get; set; }

            /// <summary>Whole days until close (0 = closes today).</summary>
            public int DaysRemaining { get; set; }

            /// <summary>Current non-waitlist registrant count.</summary>
            public int RegistrantCount { get; set; }
        }
    }
}
