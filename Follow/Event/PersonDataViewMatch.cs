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
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Lava;
using Rock.Model;

namespace com.yesuchum.Cairn.FollowingEvents.Follow.Event
{
    /// <summary>
    /// Fires while the followed person is in the configured Data View's persisted result
    /// set. Rock stores no entry date for Data View membership, so this notifies on
    /// current membership with a re-notify interval — not on the moment of entry.
    /// Requires a persisted Data View; membership is read from the persisted values,
    /// never live-executed per follower-person pair.
    /// </summary>
    [DisplayName( "Person Data View Match (cairn plugin)" )]
    [Description( "Person Data View Match (cairn plugin)" )]
    [Export( typeof( Rock.Follow.EventComponent ) )]
    [ExportMetadata( "ComponentName", "PersonDataViewMatch" )]

    [DataViewField( "Data View",
        "The Person data view to watch. Must be persisted; the event never fires for a non-persisted data view.",
        true, "", "Rock.Model.Person", "", 0, AttributeKey.DataView )]
    [IntegerField( "Re-notify Days", "Days before notifying again while the person remains in the data view (0 = notify once ever).", false, 0, "", 1, AttributeKey.RenotifyDays )]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.PERSON_DATA_VIEW_MATCH )]
    public class PersonDataViewMatch : CairnEventComponent
    {
        private static class AttributeKey
        {
            public const string DataView = "DataView";
            public const string RenotifyDays = "RenotifyDays";
        }

        /// <inheritdoc/>
        protected override string DefaultNotificationFormat
        {
            get
            {
                return "<p><a href=\"{{ 'Global' | Attribute:'InternalApplicationRoot' }}Person/{{ Entity.PersonId }}\">{{ Entity.Person.FullName }}</a> is in the '{{ EventData.SourceName }}' data view.</p>";
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
            if ( followingEvent == null || personAlias == null )
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

            var dataViewGuid = GetAttributeValue( followingEvent, AttributeKey.DataView ).AsGuidOrNull();
            if ( !dataViewGuid.HasValue )
            {
                return false;
            }

            using ( var rockContext = new RockContext() )
            {
                var dataView = new DataViewService( rockContext ).Get( dataViewGuid.Value );
                if ( dataView == null || !dataView.PersistedLastRefreshDateTime.HasValue )
                {
                    return false;
                }

                bool isMatch = rockContext.Set<DataViewPersistedValue>()
                    .AsNoTracking()
                    .Any( v => v.DataViewId == dataView.Id && v.EntityId == personAlias.PersonId );

                if ( isMatch )
                {
                    followedEventObjects.Add( "EventData", new List<object>
                    {
                        new DataViewMatchData
                        {
                            SourceName = dataView.Name,
                            LastRefreshDateTime = dataView.PersistedLastRefreshDateTime.Value
                        }
                    } );
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Merge object for the notification template.
        /// </summary>
        [Serializable]
        public class DataViewMatchData : LavaDataObject
        {
            /// <summary>The data view's name.</summary>
            public string SourceName { get; set; }

            /// <summary>When the persisted result set was last refreshed.</summary>
            public DateTime LastRefreshDateTime { get; set; }
        }
    }
}
