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

using Rock.Attribute;
using Rock.Data;
using Rock.Model;

namespace com.yesuchum.Cairn.FollowingEvents.Follow.Event
{
    /// <summary>
    /// Birthday with the Nth Year multiplier core Birthday lacks — notify only on
    /// milestone years (every 10th, 25th...). Years merge field is the age turning.
    /// </summary>
    [DisplayName( "Person Milestone Birthday (cairn plugin)" )]
    [Description( "Person Milestone Birthday (cairn plugin)" )]
    [Export( typeof( Rock.Follow.EventComponent ) )]
    [ExportMetadata( "ComponentName", "PersonMilestoneBirthday" )]

    [IntegerField( "Lead Days", "The number of days prior to the birthday that the notification should be sent.", false, 5, "", 0, AnnualDateEventComponent.AnnualAttributeKey.LeadDays )]
    [IntegerField( "Nth Year", "Only notify for birthdays that are a multiple of this number (0 = every year).", false, 0, "", 1, AnnualDateEventComponent.AnnualAttributeKey.NthYear )]
    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.PERSON_MILESTONE_BIRTHDAY )]
    public class PersonMilestoneBirthday : AnnualDateEventComponent
    {
        /// <inheritdoc/>
        protected override DateTime? GetSourceDate( FollowingEventType followingEvent, PersonAlias personAlias, RockContext rockContext )
        {
            return personAlias.Person.BirthDate;
        }

        /// <inheritdoc/>
        protected override string GetSourceName( FollowingEventType followingEvent, RockContext rockContext )
        {
            return "Birthday";
        }
    }
}
