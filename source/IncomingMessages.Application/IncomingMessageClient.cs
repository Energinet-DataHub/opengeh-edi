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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Application.UseCases;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.IncomingMessages.Application;

public class IncomingMessageClient : IIncomingMessageClient
{
    private readonly ReceiveIncomingMarketMessage _receiveIncomingMarketMessage;

    public IncomingMessageClient(ReceiveIncomingMarketMessage receiveIncomingMarketMessage)
    {
        _receiveIncomingMarketMessage = receiveIncomingMarketMessage;
    }

    public async Task<ResponseMessage> ReceiveIncomingMarketMessageAsync(
        IIncomingMarketMessageStream incomingMarketMessageStream,
        DocumentFormat incomingDocumentFormat,
        IncomingDocumentType incomingDocumentType,
        DocumentFormat responseDocumentFormat,
        CancellationToken cancellationToken,
        DataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(incomingDocumentType);
        ArgumentNullException.ThrowIfNull(incomingMarketMessageStream);

        return await _receiveIncomingMarketMessage.ReceiveIncomingMarketMessageAsync(
            incomingMarketMessageStream,
            incomingDocumentFormat,
            incomingDocumentType,
            responseDocumentFormat,
            dataSource,
            cancellationToken).ConfigureAwait(false);
    }
}
