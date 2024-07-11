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

namespace Energinet.DataHub.EDI.B2CWebApi.Models;

/// <summary>
/// The price type enum is used to make B2C Wholesale Settlement requests from the UI, and describes a combination of
/// the resolution and charge type fields in the RequestWholesaleSettlement document
/// </summary>
public enum PriceType
{
    /// <summary>
    /// Charges (non-monthly) of the type Tariff, Subscription and Fee
    /// Maps to: resolution = null, charge type = null (default value)
    /// </summary>
    TariffSubscriptionAndFee = 0,

    /// <summary>
    /// Charges (non-monthly) of the type Tariff
    /// Maps to: resolution = null, charge type = D03
    /// </summary>
    Tariff = 1,

    /// <summary>
    /// Charges (non-monthly) of the type Subscription
    /// Maps to: resolution = null, charge type = D01
    /// </summary>
    Subscription = 2,

    /// <summary>
    /// Charges (non-monthly) of the type Fee
    /// Maps to: resolution = null, charge type = D02
    /// </summary>
    Fee = 3,

    /// <summary>
    /// Monthly charges of the type Tariff
    /// Maps to: resolution = P1M, charge type = D03
    /// </summary>
    MonthlyTariff = 4,

    /// <summary>
    /// Monthly charges of the type Subscription
    /// Maps to: resolution = P1M, charge type = D01
    /// </summary>
    MonthlySubscription = 5,

    /// <summary>
    /// Monthly charges of the type Fee
    /// Maps to: resolution = P1M, charge type = D02
    /// </summary>
    MonthlyFee = 6,

    /// <summary>
    /// All monthly charges, including total monthly sums
    /// Maps to: resolution = P1M, charge type = null
    /// </summary>
    MonthlyTariffSubscriptionAndFee = 7,
}
