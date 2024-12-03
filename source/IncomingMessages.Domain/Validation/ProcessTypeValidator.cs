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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;

public class ProcessTypeValidator : IProcessTypeValidator
{
    private static readonly IReadOnlyCollection<string> _meteredDataForMeasurementPointEbixWhiteList =
    [
        BusinessReason.PeriodicMetering.Code,
        // Flex metering is only supported for Ebix and should be rejected when used for CIM
        BusinessReason.PeriodicFlexMetering.Code,
    ];

    public async Task<Result> ValidateAsync(
        IIncomingMessage message,
        DocumentFormat documentFormat,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (documentFormat == DocumentFormat.Ebix
            && message is MeteredDataForMeasurementPointMessageBase meteredDataForMeasurementPointMessage
            && _meteredDataForMeasurementPointEbixWhiteList.Contains(
                meteredDataForMeasurementPointMessage.BusinessReason))
        {
            return await Task.FromResult(Result.Succeeded()).ConfigureAwait(false);
        }

        if (message.AllowedBusinessReasons.Select(x => x.Code).Contains(message.BusinessReason))
        {
            return await Task.FromResult(Result.Succeeded()).ConfigureAwait(false);
        }

        return await Task.FromResult(Result.Failure(new NotSupportedProcessType(message.BusinessReason))).ConfigureAwait(false);
    }
}
