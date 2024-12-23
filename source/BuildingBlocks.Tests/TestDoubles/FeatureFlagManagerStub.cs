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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;

namespace Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;

/// <summary>
/// A FeatureFlagManager used to set default values and which allows overriding feature flags during tests
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Tests")]
public class FeatureFlagManagerStub : IFeatureFlagManager
{
    public Task<bool> UsePeekMessagesAsync() => Task.FromResult(true);

    public Task<bool> UsePeekTimeSeriesMessagesAsync() => Task.FromResult(true);

    public Task<bool> RequestStaysInEdiAsync() => Task.FromResult(false);

    public Task<bool> ReceiveMeteredDataForMeasurementPointsAsync() => Task.FromResult(true);

    public Task<bool> UseRequestWholesaleServicesProcessOrchestrationAsync() => Task.FromResult(false);

    public Task<bool> UseRequestAggregatedMeasureDataProcessOrchestrationAsync() => Task.FromResult(false);
}
