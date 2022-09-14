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

using System.Collections.Generic;
using NodaTime;

namespace Messaging.Application.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;

public record MarketActivityRecord(string Id, string OriginalTransactionId, Instant ValidityStart, MarketEvaluationPoint MarketEvaluationPoint);

public record MarketEvaluationPoint(
    string MarketEvaluationPointId,
    bool ElectricalHeating,
    Instant? ElectricalHeatingStart,
    MrId FirstCustomerId,
    string FirstCustomerName,
    MrId SecondCustomerId,
    string SecondCustomerName,
    bool ProtectedName,
    bool HasEnergySupplier,
    Instant SupplyStart,
    IEnumerable<UsagePointLocation> UsagePointLocation);

public record MrId(string Id, string CodingScheme);
public record UsagePointLocation(
    string Type,
    string GeoInfoReference,
    MainAddress MainAddress,
    string Name,
    string AttnName,
    string Phone1,
    string Phone2,
    string EmailAddress,
    bool ProtectedAddress);

public record MainAddress(StreetDetail StreetDetail, TownDetail TownDetail, string PostalCode, string PoBox);

public record StreetDetail(string Code, string Name, string Number, string FloorIdentification, string SuiteNumber);

public record TownDetail(string Code, string Name, string Section, string Country);
