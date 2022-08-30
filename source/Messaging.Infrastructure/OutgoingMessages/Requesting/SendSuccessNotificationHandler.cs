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

using System;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MessageHub.Client.Peek;
using Energinet.DataHub.MessageHub.Model.Extensions;
using Energinet.DataHub.MessageHub.Model.Model;
using MediatR;

namespace Messaging.Infrastructure.OutgoingMessages.Requesting;

public class SendSuccessNotificationHandler : IRequestHandler<SendSuccessNotification>
{
    private readonly IDataBundleResponseSender _dataBundleResponseSender;

    public SendSuccessNotificationHandler(IDataBundleResponseSender dataBundleResponseSender)
    {
        _dataBundleResponseSender = dataBundleResponseSender;
    }

    public async Task<Unit> Handle(SendSuccessNotification request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var bundleRequest = new DataBundleRequestDto(
            request.RequestId,
            request.ReferenceId,
            request.IdempotencyId,
            new MessageTypeDto(request.DocumentType),
            Enum.Parse<ResponseFormat>(request.RequestedFormat),
            1);

        await _dataBundleResponseSender.SendAsync(bundleRequest.CreateResponse(request.MessageStorageLocation)).ConfigureAwait(false);

        return Unit.Value;
    }
}
