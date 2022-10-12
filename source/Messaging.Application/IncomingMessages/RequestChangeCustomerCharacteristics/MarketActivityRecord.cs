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

namespace Messaging.Application.IncomingMessages.RequestChangeCustomerCharacteristics;

public record MarketActivityRecord(
    string Id,
    string EffectiveDate,
    MarketEvaluationPoint MarketEvaluationPoint) : IMarketActivityRecord;

public record MarketEvaluationPoint(
    string GsrnNumber,
    bool ElectricalHeating,
    Customer FirstCustomer,
    Customer SecondCustomer,
    bool ProtectedName,
    PointLocation FirstPointLocation,
    PointLocation SecondPointLocation);

public record Customer(
    string Id,
    string Name);

public record PointLocation(
    string Type,
    string GeoInfoReference,
    Address Address,
    bool ProtectedAddress,
    string Name,
    string AttnName);

public record Address(
    StreetDetails StreetDetails,
    TownDetails TownDetails,
    string PostalCode,
    string PoBox);

public record TownDetails(string MunicipalityCode, string CityName, string CitySubDivisionName, string CountryCode);

public record StreetDetails(string StreetCode, string StreetName, string BuildingNumber, string FloorIdentification, string RoomIdentification);
