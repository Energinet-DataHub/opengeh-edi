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

using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Microsoft.Azure.Functions.Worker;

namespace Energinet.DataHub.EDI.B2BApi.Functions.BundleMessages;

public class OutgoingMessagesBundler(
    IOutgoingMessagesBundleClient bundleClient)
{
    private readonly IOutgoingMessagesBundleClient _bundleClient = bundleClient;

    [Function(nameof(OutgoingMessagesBundler))]
    public Task BundleMessagesAsync(
        [TimerTrigger("*/30 * * * * *")] TimerInfo timerTimerInfo,
        FunctionContext context,
        CancellationToken cancellationToken)
    {
        return _bundleClient.BundleMessagesAndCommitAsync(cancellationToken);
    }
}
