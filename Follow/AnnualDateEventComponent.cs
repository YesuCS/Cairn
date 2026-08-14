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
using Rock.Lava;
using Rock.Model;

namespace com.yesuchum.Cairn.FollowingEvents.Follow
{
    /// <summary>
    /// Abstract base for every Cairn annual-recurrence following event. Owns the
    /// next-occurrence calculation, the full Fri/Sat/Sun weekend shift (core implements
    /// this three inconsistent ways; this is the one correct implementation), the
    /// windowed dedupe, and the Nth Year multiplier check. Derived classes supply only
    /// the source date and its display name.
    /// </summary>
    public abstract class AnnualDateEventComponent : CairnEventComponent
    {
        /// <summary>
        /// Attribute keys shared by every annual-recurrence component. Derived classes
        /// must declare the matching [IntegerField] attributes using these keys.
        /// </summary>
        public static class AnnualAttributeKey
        {
            public const string LeadDays = "LeadDays";
            public const string NthYear = "NthYear";
        }

        /// <summary>
        /// All annual components follow a person.
        /// </summary>
        public override Type FollowedType
        {
            get { return typeof( Rock.Model.PersonAlias ); }
        }

        /// <summary>
        /// The date the recurrence is anchored to (birth date, attribute value, first
        /// join date). Null means the event can never fire for this person.
        /// </summary>
        protected abstract DateTime? GetSourceDate( FollowingEventType followingEvent, PersonAlias personAlias, RockContext rockContext );

        /// <summary>
        /// Display name of the source for the notification ("Sobriety Date", "Serve Team"...).
        /// </summary>
        protected abstract string GetSourceName( FollowingEventType followingEvent, RockContext rockContext );

        /// <summary>
        /// Determines whether the annual event is inside its notification window and not
        /// already notified for this occurrence, and outputs the EventData merge object.
        /// </summary>
        public override bool HasEventHappened( FollowingEventType followingEvent, IEntity entity, DateTime? lastNotified, out Dictionary<string, List<object>> followedEventObjects )
        {
            followedEventObjects = new Dictionary<string, List<object>>();

            var personAlias = entity as PersonAlias;
            if ( followingEvent == null || personAlias == null || personAlias.Person == null )
            {
                return false;
            }

            using ( var rockContext = new RockContext() )
            {
                DateTime? sourceDate = GetSourceDate( followingEvent, personAlias, rockContext );
                if ( !sourceDate.HasValue )
                {
                    return false;
                }

                var today = RockDateTime.Today;
                var nextDate = GetNextOccurrence( sourceDate.Value.Date, today );
                int years = nextDate.Year - sourceDate.Value.Year;

                int yearMultiplier = GetAttributeValue( followingEvent, AnnualAttributeKey.NthYear ).AsInteger();
                if ( yearMultiplier != 0 && years % yearMultiplier != 0 )
                {
                    return false;
                }

                int leadDays = GetAttributeValue( followingEvent, AnnualAttributeKey.LeadDays ).AsInteger();

                // Weekend shift: when notifications don't send on weekends, Friday's run
                // must cover events landing Sat/Sun, and a run that does execute on the
                // weekend evaluates as if it were Friday.
                var processDate = today;
                if ( !followingEvent.SendOnWeekends )
                {
                    switch ( today.DayOfWeek )
                    {
                        case DayOfWeek.Friday:
                            leadDays += 2;
                            break;
                        case DayOfWeek.Saturday:
                            processDate = processDate.AddDays( -1 );
                            leadDays += 2;
                            break;
                        case DayOfWeek.Sunday:
                            processDate = processDate.AddDays( -2 );
                            leadDays += 2;
                            break;
                    }
                }

                // Windowed dedupe: inside the lead window, and the last notification was
                // outside the window for this same occurrence.
                if ( nextDate.Subtract( processDate ).Days <= leadDays &&
                    ( !lastNotified.HasValue || nextDate.Subtract( lastNotified.Value.Date ).Days > leadDays ) )
                {
                    var eventData = new EventData
                    {
                        SourceDate = sourceDate.Value.Date,
                        NextDate = nextDate,
                        Years = years,
                        SourceName = GetSourceName( followingEvent, rockContext )
                    };
                    followedEventObjects.Add( "EventData", new List<object> { eventData } );
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Next occurrence of the source date's month/day on or after <paramref name="today"/>.
        /// Feb 29 resolves to Feb 28 in non-leap years.
        /// </summary>
        internal static DateTime GetNextOccurrence( DateTime sourceDate, DateTime today )
        {
            var candidate = GetOccurrenceInYear( sourceDate, today.Year );
            if ( candidate < today )
            {
                candidate = GetOccurrenceInYear( sourceDate, today.Year + 1 );
            }

            return candidate;
        }

        private static DateTime GetOccurrenceInYear( DateTime sourceDate, int year )
        {
            int day = sourceDate.Day;
            if ( sourceDate.Month == 2 && day == 29 && !DateTime.IsLeapYear( year ) )
            {
                day = 28;
            }

            return new DateTime( year, sourceDate.Month, day );
        }

        /// <summary>
        /// The merge object handed to the notification template. All date math is done
        /// here; none belongs in Lava.
        /// </summary>
        [Serializable]
        public class EventData : LavaDataObject
        {
            /// <summary>The date the recurrence is anchored to.</summary>
            public DateTime SourceDate { get; set; }

            /// <summary>The upcoming occurrence being notified about.</summary>
            public DateTime NextDate { get; set; }

            /// <summary>Number of years the upcoming occurrence represents.</summary>
            public int Years { get; set; }

            /// <summary>Display name of the date's source (attribute, group type...).</summary>
            public string SourceName { get; set; }
        }
    }
}
