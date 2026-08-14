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
using System.Text;

using Rock;
using Rock.Data;
using Rock.Follow;
using Rock.Model;

namespace com.yesuchum.Cairn.FollowingEvents.Follow
{
    /// <summary>
    /// Base for every Cairn event component: owns the IEventComponentAdditionalMergeFields
    /// plumbing so each component only implements the out-parameter HasEventHappened and
    /// hands the notification template ready merge objects — no date math in Lava.
    /// </summary>
    public abstract class CairnEventComponent : EventComponent, IEventComponentAdditionalMergeFields
    {
        /// <summary>
        /// Determines whether the event happened and outputs the merge objects for the
        /// notification template.
        /// </summary>
        public abstract bool HasEventHappened( FollowingEventType followingEvent, IEntity entity, DateTime? lastNotified, out Dictionary<string, List<object>> followedEventObjects );

        /// <summary>
        /// (Not Implemented) Use the overload with the followedEventObjects out parameter.
        /// </summary>
        public sealed override bool HasEventHappened( FollowingEventType followingEvent, IEntity entity, DateTime? lastNotified )
        {
            throw new NotImplementedException( "This EventComponent implements IEventComponentAdditionalMergeFields. Use HasEventHappened( FollowingEventType followingEvent, IEntity entity, DateTime? lastNotified, out Dictionary<string, List<object>> followedEventObjects )." );
        }

        /// <summary>
        /// The starter Lava the install migration bakes into this component's seeded
        /// (inactive) event type record, so staff edit or copy a visible template
        /// instead of writing one from scratch. Not used as a runtime fallback — the
        /// event type's own format is always what renders.
        /// </summary>
        public virtual string DefaultNotificationFormat
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// v19 moved the ConnectionState enum to a different assembly, so code compiled
        /// against the v18.2 floor cannot touch ConnectionRequest.ConnectionState
        /// directly (MissingMethodException at runtime on v19+). Resolved once via
        /// reflection; state is compared by enum name, which is stable across versions.
        /// </summary>
        private static readonly System.Reflection.PropertyInfo _connectionStateProperty =
            typeof( ConnectionRequest ).GetProperty( "ConnectionState" );

        /// <summary>
        /// The request's connection state name ("Active", "Connected", ...), version-safe.
        /// </summary>
        protected static string GetConnectionStateName( ConnectionRequest request )
        {
            return _connectionStateProperty?.GetValue( request )?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Whether the request is in the Connected state, version-safe.
        /// </summary>
        protected static bool IsConnected( ConnectionRequest request )
        {
            return GetConnectionStateName( request ) == "Connected";
        }

        /// <summary>
        /// Builds a digest table row in core's notification style: photo cell, bold
        /// headline, then the person's email/cell/home contact lines.
        /// </summary>
        protected static string PersonNotificationRow( string headlineLava )
        {
            return @"<tr>
    <td style='padding-bottom: 12px; padding-right: 12px; min-width: 87px;'>
        {% if Entity.Person.PhotoId %}
            <img src='{{ 'Global' | Attribute:'PublicApplicationRoot' }}GetImage.ashx?id={{ Entity.Person.PhotoId }}&maxwidth=75&maxheight=75'/>
        {% endif %}
    </td>
    <td valign=""top"" style='padding-bottom: 12px; min-width: 300px;'>
        <strong>" + headlineLava + @"</strong><br />

        {% if Entity.Person.Email != empty %}
            Email: <a href=""mailto:{{ Entity.Person.Email }}"">{{ Entity.Person.Email }}</a><br />
        {% endif %}

        {% assign mobilePhone = Entity.Person.PhoneNumbers | Where:'NumberTypeValueId', 12 | Select:'NumberFormatted' %}
        {% if mobilePhone != empty %}
            Cell: {{ mobilePhone }}<br />
        {% endif %}

        {% assign homePhone = Entity.Person.PhoneNumbers | Where:'NumberTypeValueId', 13 | Select:'NumberFormatted' %}
        {% if homePhone != empty %}
            Home: {{ homePhone }}<br />
        {% endif %}
    </td>
</tr>";
        }

        /// <summary>
        /// Builds a digest table row for non-person entities: bold headline plus
        /// optional detail lines, aligned with the core two-cell layout.
        /// </summary>
        protected static string BasicNotificationRow( string headlineLava, string detailLava = "" )
        {
            return @"<tr>
    <td style='padding-bottom: 12px; padding-right: 12px; min-width: 87px;'>&nbsp;</td>
    <td valign=""top"" style='padding-bottom: 12px; min-width: 300px;'>
        <strong>" + headlineLava + @"</strong><br />
        " + detailLava + @"
    </td>
</tr>";
        }

        /// <summary>
        /// The standard Lava for a linked person name, used to open person headlines.
        /// </summary>
        protected const string PersonLinkLava = @"<a href=""{{ 'Global' | Attribute:'PublicApplicationRoot' }}Person/{{ Entity.PersonId }}"">{{ Entity.Person.FullName }}</a>";

        /// <summary>
        /// The standard Lava for a linked group name (Group Viewer is an internal page).
        /// </summary>
        protected const string GroupLinkLava = @"<a href=""{{ 'Global' | Attribute:'InternalApplicationRoot' }}Group/{{ Entity.Id }}"">{{ Entity.Name }}</a>";

        /// <summary>
        /// The standard Lava for a linked registration instance name (internal page).
        /// </summary>
        protected const string RegistrationInstanceLinkLava = @"<a href=""{{ 'Global' | Attribute:'InternalApplicationRoot' }}RegistrationInstance/{{ Entity.Id }}"">{{ Entity.Name }}</a>";

        /// <summary>
        /// Formats the entity notification, rendering the template once per merge object.
        /// </summary>
        public string FormatEntityNotification( FollowingEventType followingEvent, IEntity entity, Dictionary<string, List<object>> additionalMergeFields )
        {
            if ( followingEvent == null )
            {
                return string.Empty;
            }

            var template = followingEvent.EntityNotificationFormatLava ?? string.Empty;

            var sb = new StringBuilder();
            var mergeFields = new Dictionary<string, object>();
            mergeFields.Add( "Entity", entity );
            foreach ( var mergeFieldPair in additionalMergeFields )
            {
                foreach ( var mergeFieldValue in mergeFieldPair.Value )
                {
                    mergeFields.AddOrReplace( mergeFieldPair.Key, mergeFieldValue );
                    sb.Append( template.ResolveMergeFields( mergeFields ) );
                }
            }

            return sb.ToString();
        }
    }
}
