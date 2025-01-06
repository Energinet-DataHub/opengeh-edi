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

using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Interfaces;
using MediatR;

namespace Energinet.DataHub.EDI.Process.Application.ProcessInitializationHandlers;

public class InitializeAggregatedMeasureDataHandler : IProcessInitializationHandler
{
    private readonly IMediator _mediator;
    private readonly ISerializer _serializer;

    public InitializeAggregatedMeasureDataHandler(
        IMediator mediator,
        ISerializer serializer)
    {
        _mediator = mediator;
        _serializer = serializer;
    }

    public bool CanHandle(string processTypeToInitialize)
    {
        ArgumentNullException.ThrowIfNull(processTypeToInitialize);
        return processTypeToInitialize.Equals(nameof(InitializeAggregatedMeasureDataProcessDto), StringComparison.Ordinal);
    }

    public async Task ProcessAsync(byte[] processInitializationData)
    {
        var marketMessage = _serializer.Deserialize<InitializeAggregatedMeasureDataProcessDto>(System.Text.Encoding.UTF8.GetString(processInitializationData));
        await _mediator.Send(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage)).ConfigureAwait(false);
    }
}
