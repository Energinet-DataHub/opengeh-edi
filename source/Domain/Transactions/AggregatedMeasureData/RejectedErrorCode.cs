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

namespace Domain.Transactions.AggregatedMeasureData;

public class RejectedErrorCode : EnumerationType
{
    public static readonly RejectedErrorCode InvalidEnergySupplierForPeriod = new(0, nameof(InvalidEnergySupplierForPeriod), "E16");
    public static readonly RejectedErrorCode InvalidBalanceResponsibleForPeriod = new(1, nameof(InvalidBalanceResponsibleForPeriod), "E18");
    public static readonly RejectedErrorCode InvalidGridOperator = new(2, nameof(InvalidGridOperator), "E0I");
    public static readonly RejectedErrorCode NoDataForPeriod = new(3, nameof(NoDataForPeriod), "E0H");
    public static readonly RejectedErrorCode InvalidPeriod = new(4, nameof(InvalidPeriod), "E50");
    public static readonly RejectedErrorCode InvalidSearchCriteria = new(4, nameof(InvalidSearchCriteria), "D11");

    public RejectedErrorCode(int id, string name, string code)
        : base(id, name)
    {
        Code = code;
    }

    public string Code { get; }

    public static RejectedErrorCode From(string valueToParseFrom)
    {
        return GetAll<RejectedErrorCode>()
            .First(rejectedErrorCode => rejectedErrorCode.Name.Equals(valueToParseFrom, StringComparison.OrdinalIgnoreCase) ||
                                        rejectedErrorCode.Code.Equals(valueToParseFrom, StringComparison.OrdinalIgnoreCase));
    }
}
