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

using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.AcceptanceTests.Dsl;

public sealed class WholesaleServicesRequestDsl
{
    private readonly EdiDriver _edi;

#pragma warning disable VSTHRD200 // Since this is a DSL we don't want to suffix tasks with 'Async' since it is not part of the ubiquitous language

    internal WholesaleServicesRequestDsl(EdiDriver ediDriver)
    {
        _edi = ediDriver;
    }

    internal Task RequestWholesaleSettlementInJson()
    {
        return _edi.RequestWholesaleSettlementAsync(DocumentFormat.Json);
    }

    internal Task RequestInvalidWholesaleSettlementInJson()
    {
        return _edi.RequestWholesaleSettlementAsync(DocumentFormat.Json, validMessage: false);
    }

    internal Task ConfirmWholesaleServicesResultIsAvailableInJson()
    {
        return _edi.PeekWholesaleSettlementResponseAsync(DocumentFormat.Json, "NotifyWholesaleServices_MarketDocument");
    }

    internal Task ConfirmRejectedResultIsAvailableInXml()
    {
        return _edi.PeekWholesaleSettlementResponseAsync(DocumentFormat.Xml, "RejectRequestWholesaleSettlement_MarketDocument");
    }

    internal Task EmptyQueueForActor()
    {
        return _edi.EmptyQueueAsync();
    }
}
