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
    /// Fires when a connection request was created for the followed person within the
    /// window, matching the configured opportunity or connection type (one of the two
    /// must be configured).
    /// </summary>
    [DisplayName( "Person Entered Connection Opportunity (cairn plugin)" )]
    [Description( "Person Entered Connection Opportunity (cairn plugin)" )]
    [Export( typeof( Rock.Follow.EventComponent ) )]
    [ExportMetadata( "ComponentName", "PersonEnteredConnectionOpportunity" )]

    [ConnectionOpportunityField( "Connection Opportunity", "The specific opportunity to watch. Leave blank to watch a whole connection type instead.", false, "", category: "", order: 0, key: AttributeKey.ConnectionOpportunity )]
    [ConnectionTypeField( "Connection Type", "The connection type to watch (any opportunity). Ignored when a specific opportunity is selected; one of the two is required.", false, "", "", 1, AttributeKey.ConnectionType )]
    [IntegerField( "Max Days Back", "Maximum number of days back to consider.", false, 30, "", 2, AttributeKey.MaxDaysBack )]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.PERSON_ENTERED_CONNECTION_OPPORTUNITY )]
    public class PersonEnteredConnectionOpportunity : CairnEventComponent
    {
        private static class AttributeKey
        {
            public const string ConnectionOpportunity = "ConnectionOpportunity";
            public const string ConnectionType = "ConnectionType";
            public const string MaxDaysBack = "MaxDaysBack";
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
            if ( followingEvent == null || personAlias == null )
            {
                return false;
            }

            var opportunityGuid = GetAttributeValue( followingEvent, AttributeKey.ConnectionOpportunity ).AsGuidOrNull();
            var connectionTypeGuid = GetAttributeValue( followingEvent, AttributeKey.ConnectionType ).AsGuidOrNull();
            if ( !opportunityGuid.HasValue && !connectionTypeGuid.HasValue )
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
                var requestQry = new ConnectionRequestService( rockContext )
                    .Queryable().AsNoTracking()
                    .Where( r =>
                        r.PersonAlias.PersonId == personAlias.PersonId &&
                        r.CreatedDateTime.HasValue &&
                        r.CreatedDateTime > cutoff );

                if ( opportunityGuid.HasValue )
                {
                    requestQry = requestQry.Where( r => r.ConnectionOpportunity.Guid == opportunityGuid.Value );
                }
                else
                {
                    requestQry = requestQry.Where( r => r.ConnectionOpportunity.ConnectionType.Guid == connectionTypeGuid.Value );
                }

                var requests = requestQry
                    .Select( r => new
                    {
                        OpportunityName = r.ConnectionOpportunity.Name,
                        r.ConnectionState,
                        ConnectorName = r.ConnectorPersonAlias != null ? r.ConnectorPersonAlias.Person.NickName + " " + r.ConnectorPersonAlias.Person.LastName : null,
                        r.CreatedDateTime
                    } )
                    .ToList();

                if ( !requests.Any() )
                {
                    return false;
                }

                followedEventObjects.Add( "EventData", new List<object>( requests.Select( r => new ConnectionRequestData
                {
                    SourceName = r.OpportunityName,
                    RequestState = r.ConnectionState.ToString(),
                    ConnectorName = r.ConnectorName ?? string.Empty,
                    RequestDateTime = r.CreatedDateTime
                } ) ) );
                return true;
            }
        }

        /// <summary>
        /// Merge object for the notification template, one per new connection request.
        /// </summary>
        [Serializable]
        public class ConnectionRequestData : LavaDataObject
        {
            /// <summary>The connection opportunity's name.</summary>
            public string SourceName { get; set; }

            /// <summary>The request's current state.</summary>
            public string RequestState { get; set; }

            /// <summary>The assigned connector, if any.</summary>
            public string ConnectorName { get; set; }

            /// <summary>When the request was created.</summary>
            public DateTime? RequestDateTime { get; set; }
        }
    }
}
