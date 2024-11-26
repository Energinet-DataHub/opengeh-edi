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

using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Interfaces;
using MediatR;

namespace Energinet.DataHub.EDI.Process.Application.ProcessInitializationHandlers;

public class InitializeWholesaleServicesProcessHandler : IProcessInitializationHandler
{
    private readonly IMediator _mediator;
    private readonly ISerializer _serializer;
    private readonly IFeatureFlagManager _featureFlagManager;
    private readonly IRequestProcessOrchestrationStarter _requestProcessOrchestrationStarter;

    public InitializeWholesaleServicesProcessHandler(
        IMediator mediator,
        ISerializer serializer,
        IFeatureFlagManager featureFlagManager,
        IRequestProcessOrchestrationStarter requestProcessOrchestrationStarter)
    {
        _mediator = mediator;
        _serializer = serializer;
        _featureFlagManager = featureFlagManager;
        _requestProcessOrchestrationStarter = requestProcessOrchestrationStarter;
    }

    public bool CanHandle(string processTypeToInitialize)
    {
        ArgumentNullException.ThrowIfNull(processTypeToInitialize);
        return processTypeToInitialize.Equals(nameof(InitializeWholesaleServicesProcessDto), StringComparison.Ordinal);
    }

    public async Task ProcessAsync(byte[] processInitializationData)
    {
        var marketMessage = _serializer.Deserialize<InitializeWholesaleServicesProcessDto>(System.Text.Encoding.UTF8.GetString(processInitializationData));

        if (await _featureFlagManager.UseRequestWholesaleServicesProcessOrchestrationAsync().ConfigureAwait(false))
        {
            await _requestProcessOrchestrationStarter
                .StartRequestWholesaleServicesOrchestrationAsync(marketMessage)
                .ConfigureAwait(false);
        }
        else
        {
            await _mediator.Send(new InitializeWholesaleServicesProcessesCommand(marketMessage)).ConfigureAwait(false);
        }
    }
}
