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
namespace com.yesuchum.Cairn.FollowingEvents.SystemGuid
{
    /// <summary>
    /// Pinned EntityType guids for every Cairn event component. These are permanent:
    /// changing one after first deploy orphans any Following Event Type records built on it.
    /// </summary>
    public static class EntityType
    {
        // Phase 1 — Person annual
        public const string PERSON_DATE_ATTRIBUTE_ANNIVERSARY = "875B15BF-979C-4B0F-8A6A-0C392AE21AF1";
        public const string PERSON_MILESTONE_BIRTHDAY = "344D003D-ECC2-4467-B2EB-00EC53346349";
        public const string PERSON_SERVING_ANNIVERSARY = "73C781D2-EAA2-4321-9E2D-556E02C8B558";

        // Phase 2 — Person
        public const string PERSON_DATA_VIEW_MATCH = "12818EAE-CABF-499B-8628-773C13A31D32";
        public const string PERSON_LEFT_GROUP_TYPE = "A13041A3-B3A5-4C1B-AA42-F439A2BAAC9D";

        // Phase 3 — Connections
        public const string PERSON_ENTERED_CONNECTION_OPPORTUNITY = "9A15B799-31C3-48D5-9A7C-304A1AB23D22";
        public const string PERSON_CONNECTION_REQUEST_CONNECTED = "F3B00C6C-E141-42CA-A87C-34BA9734EBE5";

        // Phase 4 — Group
        public const string GROUP_MEMBER_ADDED = "4E6C89F7-E840-4841-B7C3-BD90DF1BD72B";
        public const string GROUP_MEMBER_REMOVED = "3D50072C-FA4E-4051-BF11-7BBC4175D8D5";
        public const string GROUP_ATTENDANCE_NOT_ENTERED = "CFD0F724-D7E9-477A-A740-AA89E3E0DE6E";
        public const string GROUP_INACTIVATED = "C8D8ABE2-BA87-4E94-865E-05FEF6D205A3";

        // v1.0.1
        public const string PERSON_STOPPED_ATTENDING_GROUP_TYPE = "AE46FA16-9231-4E72-9CCD-03148546BC23";

        // Phase 5 — Registration
        public const string REGISTRATION_NEW_REGISTRATION = "D7C6BA5F-F608-4DDE-9C31-638713A59E4E";
        public const string REGISTRATION_NEARING_CAPACITY = "FBE51E66-0B71-4B0D-879C-0FA1B0430F78";
        public const string REGISTRATION_CLOSING_SOON = "25CA5FA4-6169-4919-A5E8-21D43A72DE13";
    }
}
