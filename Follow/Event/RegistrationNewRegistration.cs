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
    /// Fires when a new registration was created on the followed registration instance
    /// within the window. Core notifies one configured contact per instance; this
    /// notifies every follower of the instance, opt-in.
    /// </summary>
    [DisplayName( "Registration New Registration (cairn plugin)" )]
    [Description( "Registration New Registration (cairn plugin)" )]
    [Export( typeof( Rock.Follow.EventComponent ) )]
    [ExportMetadata( "ComponentName", "RegistrationNewRegistration" )]

    [IntegerField( "Max Days Back", "Maximum number of days back to consider.", false, 7, "", 0, AttributeKey.MaxDaysBack )]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.REGISTRATION_NEW_REGISTRATION )]
    public class RegistrationNewRegistration : CairnEventComponent
    {
        private static class AttributeKey
        {
            public const string MaxDaysBack = "MaxDaysBack";
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
            if ( followingEvent == null || instance == null )
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
                var registrations = new RegistrationService( rockContext )
                    .Queryable().AsNoTracking()
                    .Where( r =>
                        r.RegistrationInstanceId == instance.Id &&
                        r.CreatedDateTime.HasValue &&
                        r.CreatedDateTime > cutoff )
                    .Select( r => new
                    {
                        RegistrantNames = r.Registrants.Where( g => !g.OnWaitList ).Select( g => g.PersonAlias.Person.NickName + " " + g.PersonAlias.Person.LastName ),
                        r.FirstName,
                        r.LastName,
                        r.CreatedDateTime
                    } )
                    .ToList();

                if ( !registrations.Any() )
                {
                    return false;
                }

                followedEventObjects.Add( "RegistrationData", new List<object>( registrations.Select( r => new RegistrationData
                {
                    RegistrantNames = string.Join( ", ", r.RegistrantNames ),
                    RegisteredBy = ( r.FirstName + " " + r.LastName ).Trim(),
                    RegisteredDateTime = r.CreatedDateTime
                } ) ) );
                return true;
            }
        }

        /// <summary>
        /// Merge object for the notification template, one per new registration.
        /// </summary>
        [Serializable]
        public class RegistrationData : LavaDataObject
        {
            /// <summary>Comma-separated names of the registrants on this registration.</summary>
            public string RegistrantNames { get; set; }

            /// <summary>Who submitted the registration.</summary>
            public string RegisteredBy { get; set; }

            /// <summary>When the registration was created.</summary>
            public DateTime? RegisteredDateTime { get; set; }
        }
    }
}
