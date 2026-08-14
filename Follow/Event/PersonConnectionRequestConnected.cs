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
    /// Fires once when a connection request for the followed person reaches the
    /// Connected state within the window, matching the configured opportunity or
    /// connection type (one of the two must be configured).
    /// </summary>
    [DisplayName( "Person Connection Request Connected (cairn plugin)" )]
    [Description( "Person Connection Request Connected (cairn plugin)" )]
    [Export( typeof( Rock.Follow.EventComponent ) )]
    [ExportMetadata( "ComponentName", "PersonConnectionRequestConnected" )]

    [ConnectionOpportunityField( "Connection Opportunity", "The specific opportunity to watch. Leave blank to watch a whole connection type instead.", false, "", category: "", order: 0, key: AttributeKey.ConnectionOpportunity )]
    [ConnectionTypeField( "Connection Type", "The connection type to watch (any opportunity). Ignored when a specific opportunity is selected; one of the two is required.", false, "", "", 1, AttributeKey.ConnectionType )]
    [IntegerField( "Max Days Back", "Maximum number of days back to consider.", false, 30, "", 2, AttributeKey.MaxDaysBack )]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.PERSON_CONNECTION_REQUEST_CONNECTED )]
    public class PersonConnectionRequestConnected : CairnEventComponent
    {
        private static class AttributeKey
        {
            public const string ConnectionOpportunity = "ConnectionOpportunity";
            public const string ConnectionType = "ConnectionType";
            public const string MaxDaysBack = "MaxDaysBack";
        }

        /// <inheritdoc/>
        protected override string DefaultNotificationFormat
        {
            get
            {
                return "<p><a href=\"{{ 'Global' | Attribute:'InternalApplicationRoot' }}Person/{{ Entity.PersonId }}\">{{ Entity.Person.FullName }}</a>'s {{ EventData.SourceName }} request was connected on {{ EventData.ConnectedDateTime | Date:'MMMM d' }}{% if EventData.ConnectorName != '' %} by {{ EventData.ConnectorName }}{% endif %}.</p>";
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

            // Once ever.
            if ( lastNotified.HasValue )
            {
                return false;
            }

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

            int daysBack = GetAttributeValue( followingEvent, AttributeKey.MaxDaysBack ).AsInteger();
            var cutoff = RockDateTime.Today.AddDays( -daysBack );

            using ( var rockContext = new RockContext() )
            {
                var requestQry = new ConnectionRequestService( rockContext )
                    .Queryable().AsNoTracking()
                    .Where( r =>
                        r.PersonAlias.PersonId == personAlias.PersonId &&
                        r.ConnectionState == ConnectionState.Connected &&
                        r.ModifiedDateTime.HasValue &&
                        r.ModifiedDateTime > cutoff );

                if ( opportunityGuid.HasValue )
                {
                    requestQry = requestQry.Where( r => r.ConnectionOpportunity.Guid == opportunityGuid.Value );
                }
                else
                {
                    requestQry = requestQry.Where( r => r.ConnectionOpportunity.ConnectionType.Guid == connectionTypeGuid.Value );
                }

                var connected = requestQry
                    .Select( r => new
                    {
                        OpportunityName = r.ConnectionOpportunity.Name,
                        ConnectorName = r.ConnectorPersonAlias != null ? r.ConnectorPersonAlias.Person.NickName + " " + r.ConnectorPersonAlias.Person.LastName : null,
                        // v19 adds ConnectionRequest.ConnectedDateTime; the v18.2 floor has no such
                        // column, so the request's last-modified time stands in for the connect time.
                        ConnectedDateTime = r.ModifiedDateTime
                    } )
                    .ToList();

                if ( !connected.Any() )
                {
                    return false;
                }

                followedEventObjects.Add( "EventData", new List<object>( connected.Select( r => new ConnectedData
                {
                    SourceName = r.OpportunityName,
                    ConnectorName = r.ConnectorName ?? string.Empty,
                    ConnectedDateTime = r.ConnectedDateTime
                } ) ) );
                return true;
            }
        }

        /// <summary>
        /// Merge object for the notification template, one per connected request.
        /// </summary>
        [Serializable]
        public class ConnectedData : LavaDataObject
        {
            /// <summary>The connection opportunity's name.</summary>
            public string SourceName { get; set; }

            /// <summary>The connector who made the connection, if any.</summary>
            public string ConnectorName { get; set; }

            /// <summary>When the request was connected.</summary>
            public DateTime? ConnectedDateTime { get; set; }
        }
    }
}
