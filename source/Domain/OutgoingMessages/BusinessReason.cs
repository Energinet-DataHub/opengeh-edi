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

using Domain.SeedWork;

namespace Domain.OutgoingMessages;

public sealed class BusinessReason : EnumerationType
{
    public static readonly BusinessReason MoveIn = new(0, nameof(MoveIn));
    public static readonly BusinessReason BalanceFixing = new(1, nameof(BalanceFixing));
    public static readonly BusinessReason PreliminaryAggregation = new(2, nameof(PreliminaryAggregation));
    // Todo: Until we have a better name for the D05 BusinessReason from our PO, we use D05 as name.
    public static readonly BusinessReason D05 = new(3, nameof(D05));

    private BusinessReason(int id, string name)
     : base(id, name)
    {
    }

    public static BusinessReason From(string valueToParse)
    {
        var processType = GetAll<BusinessReason>().FirstOrDefault(processType =>
            processType.Name.Equals(valueToParse, StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidOperationException($"{valueToParse} is not a valid process type");
        return processType;
    }
}
