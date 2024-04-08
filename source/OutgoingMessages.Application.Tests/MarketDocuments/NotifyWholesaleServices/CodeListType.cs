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

namespace OutgoingMessages.Application.Tests.MarketDocuments.NotifyWholesaleServices;

public enum CodeListType
{
    /// <summary>
    /// Codes that is a valid number is typically from UN/CEFACT (6, 9 etc.)
    /// </summary>
    UnitedNations,

    /// <summary>
    /// ebIX codes that doesn't start with D (E03 etc.)
    /// </summary>
    Ebix,

    /// <summary>
    /// ebIX codes that start with D is typically danish (D01, D05 etc.)
    /// </summary>
    EbixDenmark,
}

public enum ActorNumberType
{
    Gln,
    Eic,
}
