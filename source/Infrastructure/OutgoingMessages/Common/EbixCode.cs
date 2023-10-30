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
using Energinet.DataHub.EDI.Domain.Common;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;

namespace Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.Common;

public static class EbixCode
{
    public static string Of<T>(string value)
        where T : EnumerationCodeType
    {
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        var code = EnumerationType.FromName<T>(value);
        return !string.IsNullOrWhiteSpace(code.Code) ? code.Code : throw NoCodeFoundFor(code.Name);
    }

    public static BusinessReason To(string businessReasonCode)
    {
        ArgumentNullException.ThrowIfNull(businessReasonCode);

        return BusinessReason.From(businessReasonCode);
    }

    public static string Of(Quality quality)
    {
        // The codes for Ebix documents are different from those in CIM - thats why this functionlooks like it does
        ArgumentNullException.ThrowIfNull(quality);

        if (quality == Quality.Estimated)
            return "56";
        if (quality == Quality.Calculated)
            return "D01";
        if (quality == Quality.Measured)
            return "E01";
        if (quality == Quality.Adjusted)
            return "36";

        throw NoCodeFoundFor(quality.Name);
    }

    public static string Of(ReasonCode reasonCode)
    {
        // The codes for Ebix documents are different from those in CIM - thats why this functionlooks like it does
        ArgumentNullException.ThrowIfNull(reasonCode);

        if (reasonCode == ReasonCode.FullyAccepted)
            return "39";
        if (reasonCode == ReasonCode.FullyRejected)
            return "41";

        throw NoCodeFoundFor(reasonCode.Name);
    }

    private static Exception NoCodeFoundFor(string domainType)
    {
        return new InvalidOperationException($"No code has been defined for {domainType}");
    }
}
