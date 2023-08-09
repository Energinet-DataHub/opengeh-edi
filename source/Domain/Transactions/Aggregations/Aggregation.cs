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

namespace Domain.Transactions.Aggregations;

public record Aggregation(
    IReadOnlyList<Point> Points,
    string MeteringPointType,
    string MeasureUnitType,
    string Resolution,
    Period Period,
    string? SettlementType,
    string BusinessReason,
    ActorGrouping ActorGrouping,
    GridAreaDetails GridAreaDetails,
    string? OriginalTransactionIdReference = null,
    string? Receiver = null,
    string? ReceiverRole = null,
    string? Product = null,
    string? SettlementVersion = null);

public record Point(int Position, decimal? Quantity, string Quality, string SampleTime);

public record ActorGrouping(string? EnergySupplierNumber, string? BalanceResponsibleNumber);

public record GridAreaDetails(string GridAreaCode, string OperatorNumber);
