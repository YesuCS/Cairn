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
using System.Data.Entity;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace com.yesuchum.Cairn.FollowingEvents.Follow.Event
{
    /// <summary>
    /// Annual recurrence on the date a person first joined a group of the configured
    /// type — a serving anniversary when pointed at the serve-team group type.
    /// </summary>
    [DisplayName( "Person Serving Anniversary (cairn plugin)" )]
    [Description( "Person Serving Anniversary (cairn plugin)" )]
    [Export( typeof( Rock.Follow.EventComponent ) )]
    [ExportMetadata( "ComponentName", "PersonServingAnniversary" )]

    [GroupTypeField( "Group Type", "The group type whose first-join date anchors the anniversary.", true, "", "", 0, AttributeKey.GroupType )]
    [IntegerField( "Lead Days", "The number of days prior to the anniversary that the notification should be sent.", false, 5, "", 1, AnnualDateEventComponent.AnnualAttributeKey.LeadDays )]
    [IntegerField( "Nth Year", "Only notify for anniversaries that are a multiple of this number (0 = every year).", false, 0, "", 2, AnnualDateEventComponent.AnnualAttributeKey.NthYear )]
    [BooleanField( "Active Members Only", "Only consider memberships that are currently active.", true, "", 3, AttributeKey.ActiveMembersOnly )]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.PERSON_SERVING_ANNIVERSARY )]
    public class PersonServingAnniversary : AnnualDateEventComponent
    {
        private static class AttributeKey
        {
            public const string GroupType = "GroupType";
            public const string ActiveMembersOnly = "ActiveMembersOnly";
        }

        /// <inheritdoc/>
        protected override DateTime? GetSourceDate( FollowingEventType followingEvent, PersonAlias personAlias, RockContext rockContext )
        {
            var groupTypeGuid = GetAttributeValue( followingEvent, AttributeKey.GroupType ).AsGuidOrNull();
            if ( !groupTypeGuid.HasValue )
            {
                return null;
            }

            bool activeOnly = GetAttributeValue( followingEvent, AttributeKey.ActiveMembersOnly ).AsBoolean( true );

            var memberQry = new GroupMemberService( rockContext )
                .Queryable().AsNoTracking()
                .Where( m =>
                    m.PersonId == personAlias.PersonId &&
                    m.Group.GroupType.Guid == groupTypeGuid.Value );

            if ( activeOnly )
            {
                memberQry = memberQry.Where( m => m.GroupMemberStatus == GroupMemberStatus.Active );
            }

            return memberQry.Min( m => m.DateTimeAdded );
        }

        /// <inheritdoc/>
        protected override string GetSourceName( FollowingEventType followingEvent, RockContext rockContext )
        {
            var groupTypeGuid = GetAttributeValue( followingEvent, AttributeKey.GroupType ).AsGuidOrNull();
            if ( groupTypeGuid.HasValue )
            {
                var groupType = GroupTypeCache.Get( groupTypeGuid.Value );
                if ( groupType != null )
                {
                    return groupType.Name;
                }
            }

            return string.Empty;
        }
    }
}
