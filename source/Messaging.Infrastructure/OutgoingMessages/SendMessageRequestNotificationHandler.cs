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
using MediatR;

namespace Messaging.Infrastructure.OutgoingMessages
{
    public class SendMessageRequestNotificationHandler : IRequestHandler<SendMessageRequestNotification, Unit>
    {
        private readonly IDataBundleResponseSender _dataBundleResponseSender;

        public SendMessageRequestNotificationHandler(IDataBundleResponseSender dataBundleResponseSender)
        {
            _dataBundleResponseSender = dataBundleResponseSender;
        }

        public async Task<Unit> Handle(SendMessageRequestNotification request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            if (request.Uri != null)
                await _dataBundleResponseSender.SendAsync(request.DataBundleRequestDto.CreateResponse(request.Uri)).ConfigureAwait(false);
            if (request.DataBundleResponseErrorDto != null)
                await _dataBundleResponseSender.SendAsync(request.DataBundleRequestDto.CreateErrorResponse(request.DataBundleResponseErrorDto)).ConfigureAwait(false);

            return Unit.Value;
        }
    }
}
