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
using System.ComponentModel;
using System.ComponentModel.Composition;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace com.yesuchum.Cairn.FollowingEvents.Follow.Event
{
    /// <summary>
    /// Annual recurrence on any Person date attribute (sobriety date, salvation date...).
    /// Notifications go to anyone following the person who can see the event type;
    /// security on the underlying attribute does NOT trim the notification. Restrict
    /// sensitive event types via the event type's own security.
    /// </summary>
    [DisplayName( "Person Date Attribute Anniversary (cairn plugin)" )]
    [Description( "Person Date Attribute Anniversary (cairn plugin)" )]
    [Export( typeof( Rock.Follow.EventComponent ) )]
    [ExportMetadata( "ComponentName", "PersonDateAttributeAnniversary" )]

    [AttributeField( Rock.SystemGuid.EntityType.PERSON,
        "Date Attribute",
        "The Person date attribute whose value anchors the annual recurrence. The attribute's value must be a date.",
        true, false, "", "", 0, AttributeKey.DateAttribute )]
    [IntegerField( "Lead Days", "The number of days prior to the anniversary that the notification should be sent.", false, 5, "", 1, AnnualDateEventComponent.AnnualAttributeKey.LeadDays )]
    [IntegerField( "Nth Year", "Only notify for anniversaries that are a multiple of this number (0 = every year).", false, 0, "", 2, AnnualDateEventComponent.AnnualAttributeKey.NthYear )]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.PERSON_DATE_ATTRIBUTE_ANNIVERSARY )]
    public class PersonDateAttributeAnniversary : AnnualDateEventComponent
    {
        private static class AttributeKey
        {
            public const string DateAttribute = "DateAttribute";
        }

        /// <inheritdoc/>
        protected override DateTime? GetSourceDate( FollowingEventType followingEvent, PersonAlias personAlias, RockContext rockContext )
        {
            var attributeCache = GetConfiguredAttribute( followingEvent );
            if ( attributeCache == null )
            {
                return null;
            }

            var person = personAlias.Person;
            if ( person.Attributes == null )
            {
                person.LoadAttributes( rockContext );
            }

            // Runtime validation stands in for picker-level field-type filtering: a
            // non-date value simply parses to null and the event never fires.
            return person.GetAttributeValue( attributeCache.Key ).AsDateTime();
        }

        /// <inheritdoc/>
        protected override string GetSourceName( FollowingEventType followingEvent, RockContext rockContext )
        {
            var attributeCache = GetConfiguredAttribute( followingEvent );
            return attributeCache != null ? attributeCache.Name : string.Empty;
        }

        private AttributeCache GetConfiguredAttribute( FollowingEventType followingEvent )
        {
            var attributeGuid = GetAttributeValue( followingEvent, AttributeKey.DateAttribute ).AsGuidOrNull();
            return attributeGuid.HasValue ? AttributeCache.Get( attributeGuid.Value ) : null;
        }
    }
}
