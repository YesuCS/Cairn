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
        /// The Lava used when the event type's Entity Notification Format is left blank,
        /// so staff get a working email without writing a template from scratch. An
        /// event type's own format always wins when present.
        /// </summary>
        protected virtual string DefaultNotificationFormat
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Formats the entity notification, rendering the template once per merge object.
        /// </summary>
        public string FormatEntityNotification( FollowingEventType followingEvent, IEntity entity, Dictionary<string, List<object>> additionalMergeFields )
        {
            if ( followingEvent == null )
            {
                return string.Empty;
            }

            var template = followingEvent.EntityNotificationFormatLava;
            if ( string.IsNullOrWhiteSpace( template ) )
            {
                template = DefaultNotificationFormat;
            }

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
