﻿// Copyright 2020 Energinet DataHub A/S
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

namespace Energinet.DataHub.Wholesale.CalculationResults.Interfaces.CalculationResults;

/// <summary>
/// The unit of measurement for the quantity.
/// </summary>
public enum QuantityUnit
{
    /// <summary>
    /// The quantity unit is Kilo Watt Hour.
    /// Code: H87
    /// </summary>
    Kwh,

    /// <summary>
    /// The quantity unit is pieces.
    /// The unit is used for subscriptions and fees that are associated with the metering point.
    /// </summary>
    Pieces,
}