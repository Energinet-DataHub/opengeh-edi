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
    private MasterDataContent(string gsrnNumber, Address address, Series series, GridAreaDetails gridAreaDetails, string connectionState, string meteringMethod, string readingPeriodicity, string type, int maximumCurrent, int maximumPower, string powerPlantGsrnNumber, DateTime effectiveDate, string meterNumber, double capacity, string assetType, string settlementMethod, string scheduledMeterReadingDate, bool productionObligation, string netSettlementGroup, string disconnectionType, string connectionType)
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
    }

    public string? GsrnNumber { get; set; }

    public Address? Address { get; set; }

    public Series? Series { get; set; }

    public GridAreaDetails? GridAreaDetails { get; set; }

    public string? ConnectionState { get; set; }

    public string? MeteringMethod { get; set; }

    public string? ReadingPeriodicity { get; set; }

    public string? Type { get; set; }

    public int MaximumCurrent { get; set; }

    public int MaximumPower { get; set; }

    public string? PowerPlantGsrnNumber { get; set; }

    public DateTime EffectiveDate { get; set; }

    public string? MeterNumber { get; set; }

    public double Capacity { get; set; }

    public string? AssetType { get; set; }

    public string? SettlementMethod { get; set; }

    public string? ScheduledMeterReadingDate { get; set; }

    public bool ProductionObligation { get; set; }

    public string? NetSettlementGroup { get; set; }

    public string? DisconnectionType { get; set; }

    public string? ConnectionType { get; set; }

    public static MasterDataContent Create(string gsrnNumber, Address address, Series series, GridAreaDetails gridAreaDetails, string connectionState, string meteringMethod, string readingPeriodicity, string type, int maximumCurrent, int maximumPower, string powerPlantGsrnNumber, DateTime effectiveDate, string meterNumber, double capacity, string assetType, string settlementMethod, string scheduledMeterReadingDate, bool productionObligation, string netSettlementGroup, string disconnectionType, string connectionType)
    {
        return new MasterDataContent(
            gsrnNumber,
            address,
            series,
            gridAreaDetails,
            connectionState,
            meteringMethod,
            readingPeriodicity,
            type,
            maximumCurrent,
            maximumPower,
            powerPlantGsrnNumber,
            effectiveDate,
            meterNumber,
            capacity,
            assetType,
            settlementMethod,
            scheduledMeterReadingDate,
            productionObligation,
            netSettlementGroup,
            disconnectionType,
            connectionType);
    }
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
