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

namespace Messaging.Application.OutgoingMessages.AccountingPointCharacteristics;

public class Address
{
    public Address(string streetCode, string streetNmae, string streetNumber, string floorIdentification, string suiteNumber, string townCode, string townName, string townSection, string country, string postalCode)
    {
        StreetCode = streetCode;
        StreetNmae = streetNmae;
        StreetNumber = streetNumber;
        FloorIdentification = floorIdentification;
        SuiteNumber = suiteNumber;
        TownCode = townCode;
        TownName = townName;
        TownSection = townSection;
        Country = country;
        PostalCode = postalCode;
    }

    public string StreetCode { get; }

    public string StreetNmae { get; }

    public string StreetNumber { get; }

    public string FloorIdentification { get; }

    public string SuiteNumber { get; }

    public string TownCode { get; }

    public string TownName { get; }

    public string TownSection { get; }

    public string Country { get; }

    public string PostalCode { get; }
}
