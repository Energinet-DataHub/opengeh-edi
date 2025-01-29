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

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;

/// <summary>
/// List of all Feature Flags that exists in the system. A Feature Flag name must
/// correspond to a value found in the app configuration as "FeatureManagement__NameOfFeatureFlag"
/// </summary>
public enum FeatureFlagName
{
    /// <summary>
    /// Whether to disable peek messages
    /// </summary>
    UsePeekMessages,

    /// <summary>
    /// Whether to disable peek time series messages
    /// </summary>
    UsePeekTimeSeriesMessages,

    /// <summary>
    /// Whether to send requests for aggregated measured data to Wholesale, or handle it in EDI.
    /// </summary>
    RequestStaysInEdi,

    /// <summary>
    /// Whether to allow receiving metered data for metering points.
    /// </summary>
    ReceiveMeteredDataForMeasurementPoints,

    /// <summary>
    /// Whether to use orchestration for handling RequestWholesaleServices processes.
    /// </summary>
    UseRequestWholesaleServicesProcessOrchestration,

    /// <summary>
    /// Whether to use orchestration for handling RequestAggregatedMeasureData processes.
    /// </summary>
    UseRequestAggregatedMeasureDataProcessOrchestration,

    /// <summary>
    /// Whether to start using standard blob service client.
    /// </summary>
    UseStandardBlobServiceClient,

    /// <summary>
    /// Whether to enqueue BRS-023/027 messages via the Process Manager.
    /// </summary>
    EnqueueBrs023027MessagesFromProcessManager,

    /// <summary>
    /// Whether to enqueue BRS-023/027 messages via Wholesale.
    /// </summary>
    DisableEnqueueBrs023027MessagesFromWholesale,
}
