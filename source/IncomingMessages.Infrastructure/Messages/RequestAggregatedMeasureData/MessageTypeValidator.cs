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

using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ValidationErrors;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Messages.RequestAggregatedMeasureData;

public class MessageTypeValidator : IMessageTypeValidator
{
    private static readonly IReadOnlyCollection<string> _aggregatedMeasureDataWhiteList = new[] { "E74" };
    private static readonly IReadOnlyCollection<string> _wholesaleServicesWhiteList = new[] { "D21" };

    public async Task<Result> ValidateAsync(IIncomingMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        return await Task.FromResult(
                message switch
                {
                    RequestAggregatedMeasureDataMessage ramdm =>
                        _aggregatedMeasureDataWhiteList.Contains(ramdm.MessageType)
                            ? Result.Succeeded()
                            : Result.Failure(
                                new NotSupportedMessageType(ramdm.MessageType)),
                    RequestWholesaleServicesMessage rwsm =>
                        _wholesaleServicesWhiteList.Contains(rqsm.MessageType)
                            ? Result.Succeeded()
                            : Result.Failure(new NotSupportedMessageType(rqsm.MessageType)),
                    _ => throw new InvalidOperationException($"The baw's on the slates! {message.GetType().Name}"),
                })
            .ConfigureAwait(false);
    }
}
