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
using System.Collections.Generic;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.NotifyAggregatedMeasureData;

// This is a copy of TimeSeries defined in the process module
public record TimeSeriesMarketActivityRecord(
    TransactionId TransactionId,
    string GridAreaCode,
    string MeteringPointType,
    string? SettlementMethod,
    string MeasureUnitType,
    string Resolution,
    string? EnergySupplierNumber,
    string? BalanceResponsibleNumber,
    Period Period,
    IReadOnlyCollection<Point> Point,
    long CalculationResultVersion,
    TransactionId? OriginalTransactionIdReference = null,
    string? SettlementVersion = null);

public record Point(int Position, decimal? Quantity, CalculatedQuantityQuality QuantityQuality, string SampleTime);
