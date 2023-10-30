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
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Common;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;

namespace Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.Common;

public static class CimCode
{
    public static string Of<T>(string value)
        where T : EnumerationCodeType
    {
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        var code = EnumerationType.FromName<T>(value);
        return !string.IsNullOrWhiteSpace(code.Code) ? code.Code : throw NoCodeFoundFor(code.Name);
    }

    public static string Of<T>(T value)
        where T : EnumerationCodeType
    {
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        return !string.IsNullOrWhiteSpace(value.Code) ? value.Code : throw NoCodeFoundFor(value.Name);
    }

    public static BusinessReason To(string businessReasonCode)
    {
        ArgumentNullException.ThrowIfNull(businessReasonCode);

        if (businessReasonCode == "D04")
            return BusinessReason.BalanceFixing;

        if (businessReasonCode == "E65")
            return BusinessReason.MoveIn;

        if (businessReasonCode == "D03")
            return BusinessReason.PreliminaryAggregation;

        if (businessReasonCode == "D05")
            return BusinessReason.WholesaleFixing;

        if (businessReasonCode == "D32")
            return BusinessReason.Correction;

        throw NoBusinessReasonFoundFor(businessReasonCode);
    }

    public static string CodingSchemeOf(ActorNumber actorNumber)
    {
        ArgumentNullException.ThrowIfNull(actorNumber);
        if (ActorNumber.IsGlnNumber(actorNumber.Value))
            return "A10";
        if (ActorNumber.IsEic(actorNumber.Value))
            return "A01";

        throw NoCodeFoundFor(actorNumber.Value);
    }

    private static Exception NoCodeFoundFor(string domainType)
    {
        return new InvalidOperationException($"No code has been defined for {domainType}");
    }

    private static Exception NoBusinessReasonFoundFor(string businessReasonCode)
    {
        return new InvalidOperationException($"No business reason has been defined for {businessReasonCode}");
    }
}
