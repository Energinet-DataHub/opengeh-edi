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

public class ProcessTypeValidator : IProcessTypeValidator
{
    private static readonly IReadOnlyCollection<string> _aggregatedMeasureDataWhitelist =
        new[] { "D03", "D04", "D05", "D32" };

    private static readonly IReadOnlyCollection<string> _wholesaleServicesWhitelist =
        new[] { "D03", "D04", "D05", "D32" };

    public async Task<Result> ValidateAsync(IIncomingMessage message, CancellationToken cancellationToken)
    {
        return await Task.FromResult(
                message switch
                {
                    RequestAggregatedMeasureDataMessage ragdm =>
                        _aggregatedMeasureDataWhitelist.Contains(ragdm.BusinessReason)
                            ? Result.Succeeded()
                            : Result.Failure(new NotSupportedProcessType(ragdm.BusinessReason)),
                    RequestWholesaleServicesMessage rwsm =>
                        _wholesaleServicesWhitelist.Contains(rwsm.BusinessReason)
                            ? Result.Succeeded()
                            : Result.Failure(new NotSupportedProcessType(rwsm.BusinessReason)),
                    _ => throw new InvalidOperationException($"The baw's on the slates! {message.GetType().Name}"),
                })
            .ConfigureAwait(false);
    }
}
