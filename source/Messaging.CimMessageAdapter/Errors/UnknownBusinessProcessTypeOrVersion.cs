// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Messaging.CimMessageAdapter.Errors
{
    public class UnknownBusinessProcessTypeOrVersion : ValidationError
    {
        public UnknownBusinessProcessTypeOrVersion(string businessProcessType, string version)
            : base($"Schema version {version} for business process type {businessProcessType} does not exist.", "B2B-001")
        {
        }
    }
}
