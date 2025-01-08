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

namespace Energinet.DataHub.EDI.B2BApi.Configuration;

public class EdiTopicOptions
{
    public const string SectionName = "EdiTopic";

    /// <summary>
    /// EDI Service Bus topic name
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Enqueue BRS 023/027 messages subscription name, retrieved from application settings. The Process Manager
    /// uses this to signal EDI to enqueue messages for BRS 021/023.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string EnqueueBrs_023_027_SubscriptionName { get; set; } = string.Empty;

    /// <summary>
    /// Enqueue BRS 026 messages subscription name, retrieved from application settings. The Process Manager
    /// uses this to signal EDI to enqueue messages for BRS 026.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string EnqueueBrs_026_SubscriptionName { get; set; } = string.Empty;

    /// <summary>
    /// Enqueue BRS 028 messages subscription name, retrieved from application settings. The Process Manager
    /// uses this to signal EDI to enqueue messages for BRS 028.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string EnqueueBrs_028_SubscriptionName { get; set; } = string.Empty;
}
