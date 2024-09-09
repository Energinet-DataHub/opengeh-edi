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

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

/// <summary>
///     Represents the quantity quality of a data point in a time series.
///     The enum is an internal representation used solely internally in the the EDI system.
///     This enum can and will be translated to an output format specific code and/or value as part of the generation of
///     outgoing messages.
///     The values of the enum are to be understood as follows:
///     <list type="bullet">
///         <item>
///             <description>Missing: The quality is missing or all qualities are missing.</description>
///         </item>
///         <item>
///             <description>Incomplete: At least one quality is missing.</description>
///         </item>
///         <item>
///             <description>Estimated: No qualities are missing, but at least one quality is estimated.</description>
///         </item>
///         <item>
///             <description>Measured: No qualities are missing or estimated, but at least one quality is measured.</description>
///         </item>
///         <item>
///             <description>
///                 Calculated: No qualities are missing, estimated or measured, but at least one quality is
///                 calculated.
///             </description>
///         </item>
///         <item>
///             <description>NotAvailable: The quality is not available (e.g. used for "0-stilling").</description>
///         </item>
///     </list>
/// </summary>
[Serializable]
public enum CalculatedQuantityQuality
{
    Missing,
    Incomplete,
    Estimated,
    Measured,
    Calculated,
    NotAvailable,
}
