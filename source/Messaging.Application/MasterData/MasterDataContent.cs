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

namespace Messaging.Application.MasterData;

public class MasterDataContent
{
    public MasterDataContent(string gsrnNumber, Address address, Series series, GridAreaDetails gridAreaDetails, string connectionState, string meteringMethod, string readingPeriodicity, string type, int maximumCurrent, int maximumPower, string powerPlantGsrnNumber, DateTime effectiveDate, string meterNumber, double capacity, string assetType, string settlementMethod, string scheduledMeterReadingDate, bool productionObligation, string netSettlementGroup, string disconnectionType, string connectionType, string? parentMarketEvaluationPointId, string? meteringPointResponsible)
    {
        GsrnNumber = gsrnNumber;
        Address = address;
        Series = series;
        GridAreaDetails = gridAreaDetails;
        ConnectionState = connectionState;
        MeteringMethod = meteringMethod;
        ReadingPeriodicity = readingPeriodicity;
        Type = type;
        MaximumCurrent = maximumCurrent;
        MaximumPower = maximumPower;
        PowerPlantGsrnNumber = powerPlantGsrnNumber;
        EffectiveDate = effectiveDate;
        MeterNumber = meterNumber;
        Capacity = capacity;
        AssetType = assetType;
        SettlementMethod = settlementMethod;
        ScheduledMeterReadingDate = scheduledMeterReadingDate;
        ProductionObligation = productionObligation;
        NetSettlementGroup = netSettlementGroup;
        DisconnectionType = disconnectionType;
        ConnectionType = connectionType;
        ParentMarketEvaluationPointId = parentMarketEvaluationPointId;
        MeteringPointResponsible = meteringPointResponsible;
    }

    public string GsrnNumber { get; }

    public Address Address { get; }

    public Series Series { get; }

    public GridAreaDetails GridAreaDetails { get; }

    public string ConnectionState { get; }

    public string MeteringMethod { get; }

    public string ReadingPeriodicity { get; }

    public string Type { get; }

    public int MaximumCurrent { get; }

    public int MaximumPower { get; }

    public string PowerPlantGsrnNumber { get; }

    public DateTime EffectiveDate { get; }

    public string MeterNumber { get; }

    public double Capacity { get; }

    public string AssetType { get; }

    public string SettlementMethod { get; }

    public string ScheduledMeterReadingDate { get; }

    public bool ProductionObligation { get; }

    public string NetSettlementGroup { get; }

    public string DisconnectionType { get; }

    public string ConnectionType { get; }

    public string? ParentMarketEvaluationPointId { get; }

    public string? MeteringPointResponsible { get; }
}

public record Address(
    string StreetName,
    string StreetCode,
    string PostCode,
    string City,
    string CountryCode,
    string CitySubDivision,
    string Floor,
    string Room,
    string BuildingNumber,
    int MunicipalityCode,
    bool IsActualAddress,
    Guid GeoInfoReference,
    string LocationDescription);

public record Series(string Product, string UnitType);

public record GridAreaDetails(string Code, string ToCode, string FromCode);
