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

using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;

/// <summary>
/// Responsible for verifying that the Receiver is a Calculation Responsible Receiver and Datahub
/// </summary>
public class CalculationResponsibleReceiverValidator : IReceiverValidator
{
    private const string CalculationResponsibleRole = "DGL";
    private const string GlnOfDataHub = "5790001330552";

    public Task<Result> VerifyAsync(string receiverNumber, string receiverRole)
    {
        ArgumentNullException.ThrowIfNull(receiverNumber);
        ArgumentNullException.ThrowIfNull(receiverRole);

        if (IsCalculationResponsible(receiverRole) == false)
        {
            return Task.FromResult(Result.Failure(new InvalidReceiverRole()));
        }

        if (ReceiverIsDataHub(receiverNumber) == false)
        {
            return Task.FromResult(Result.Failure(new InvalidReceiverId(receiverNumber)));
        }

        return Task.FromResult(Result.Succeeded());
    }

    private static bool IsCalculationResponsible(string role)
    {
        return role.Equals(CalculationResponsibleRole, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ReceiverIsDataHub(string receiverId)
    {
        return receiverId.Equals(GlnOfDataHub, StringComparison.OrdinalIgnoreCase);
    }
}
