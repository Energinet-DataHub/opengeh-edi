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
using NodaTime;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices;

[Serializable]
public record AcceptedWholesaleServicesSerieDto(
    IReadOnlyCollection<Point> Points,
    MeteringPointType MeteringPointType,
    Resolution Resolution,
    ChargeType ChargeType,
    QuantityUnit QuantityUnit,
    SettlementVersion? SettlementVersion,
    SettlementMethod? SettlementMethod,
    Currency Currency,
    string ChargeOwnerId,
    string EnergySupplierId,
    string GridArea,
    string ChargeCode,
    Instant StartOfPeriod,
    Instant EndOfPeriod,
    long CalculationResultVersion);

[Serializable]
public record Point(int Position, decimal Quantity, CalculatedQuantityQuality QuantityQuality, decimal? Price, decimal? Amount);

[Serializable]
public enum Currency
{
    Dkk,
}

[Serializable]
public enum SettlementVersion
{
    WholesaleFixing,
    FirstCorrectionSettlement,
    SecondCorrectionSettlement,
    ThirdCorrectionSettlement,
}

[Serializable]
public enum SettlementMethod
{
    Flex,
    NonProfiled,
}

[Serializable]
public enum QuantityUnit
{
    Kwh,
    Pieces,
}

[Serializable]
public enum ChargeType
{
    Fee,
    Tariff,
    Subscription,
}

[Serializable]
public enum Resolution
{
    Day,
    Hour,
    Monthly,
}

[Serializable]
public enum MeteringPointType
{
    Production,
    Consumption,
}

[Serializable]
public enum CalculatedQuantityQuality
{
    Missing,
    Estimated,
    Measured,
    Calculated,
}
