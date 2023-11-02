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

using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Process.Domain.OutgoingMessages.Queueing;

namespace Energinet.DataHub.EDI.Process.Domain.Documents;

public class DocumentType : EnumerationType
{
    public static readonly DocumentType NotifyAggregatedMeasureData = new(7, nameof(NotifyAggregatedMeasureData), MessageCategory.Aggregations);
    public static readonly DocumentType RejectRequestAggregatedMeasureData = new(8, nameof(RejectRequestAggregatedMeasureData), MessageCategory.Aggregations);

    protected DocumentType(int id, string name, MessageCategory category)
        : base(id, name)
    {
        Category = category;
    }

    public MessageCategory Category { get; }

    public override string ToString()
    {
        return Name;
    }
}
