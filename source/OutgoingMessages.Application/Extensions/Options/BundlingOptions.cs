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

using System.ComponentModel.DataAnnotations;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.Options;

public class BundlingOptions
{
    public const string SectionName = "Bundling";

    /// <summary>
    /// How old an outgoing messages can be before it should be bundled.
    /// </summary>
    [Required]
    public double BundleMessagesOlderThanSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// The maximum amount of messages that should be added to a bundle.
    /// </summary>
    [Required]
    public int MaxBundleMessageCount { get; set; } = 2000;

    /// <summary>
    /// The maximum amount of data points that should be added to a bundle. An example of a data point could be
    /// the number of energy observations in an RSM-012 transaction.
    /// <remarks>
    /// For reference, there are 96 data points in a daily RSM-012 transaction, if the resolution is 15 minutes (4*24=96).
    /// This means a bundle of 2000 daily RSM-012 transactions is 192000 data points (96*2000=192000), which is just
    /// above 50MB in ebIX format, so we must set the limit to a number below that.
    /// </remarks>
    /// </summary>
    [Required]
    public int MaxBundleDataCount { get; set; } = 150000;
}
