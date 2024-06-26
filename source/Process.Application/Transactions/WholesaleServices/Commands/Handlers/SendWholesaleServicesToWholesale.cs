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

using System;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using MediatR;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Commands.Handlers;

public class SendWholesaleServicesRequestToWholesaleHandler : IRequestHandler<SendWholesaleServicesRequestToWholesale, Unit>
{
    private readonly IWholesaleServicesProcessRepository _wholesaleServicesProcessRepository;

    public SendWholesaleServicesRequestToWholesaleHandler(
        IWholesaleServicesProcessRepository wholesaleServicesProcessRepository)
    {
        _wholesaleServicesProcessRepository = wholesaleServicesProcessRepository;
    }

    public async Task<Unit> Handle(
        SendWholesaleServicesRequestToWholesale request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var process = await _wholesaleServicesProcessRepository
            .GetAsync(ProcessId.Create(request.ProcessId), cancellationToken).ConfigureAwait(false);
        process.SendToWholesale();

        return Unit.Value;
    }
}
