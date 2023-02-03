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

using System;
using System.Text.Json.Serialization;
using Application.Configuration.Commands.Commands;

namespace Application.Transactions.Aggregations.HourlyConsumption;

public class PrepareTransactions : InternalCommand
{
    [JsonConstructor]
    public PrepareTransactions(Guid id, Guid resultId, string gridArea, Period period)
        : base(id)
    {
        ResultId = resultId;
        GridArea = gridArea;
        Period = period;
    }

    public PrepareTransactions(Guid resultId, string gridArea, Period period)
    {
        ResultId = resultId;
        GridArea = gridArea;
        Period = period;
    }

    public Guid ResultId { get; }

    public string GridArea { get; }

    public Period Period { get; }
}
