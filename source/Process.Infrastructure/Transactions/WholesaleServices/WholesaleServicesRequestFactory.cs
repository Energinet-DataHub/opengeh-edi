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
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Energinet.DataHub.Edi.Requests;
using Google.Protobuf;
using ChargeType = Energinet.DataHub.Edi.Requests.ChargeType;

namespace Energinet.DataHub.EDI.Process.Infrastructure.Transactions.WholesaleServices;

public static class WholesaleServicesRequestFactory
{
    public static ServiceBusMessage CreateServiceBusMessage(WholesaleServicesProcess process)
    {
        ArgumentNullException.ThrowIfNull(process);

        var body = CreateWholesaleServicesProcessRequest(process);

        var message = new ServiceBusMessage()
        {
            Body = new BinaryData(body.ToByteArray()),
            Subject = body.GetType().Name,
            MessageId = process.ProcessId.Id.ToString(),
        };
        message.ApplicationProperties.Add("ReferenceId", process.ProcessId.Id.ToString());
        return message;
    }

    private static WholesaleServicesRequest CreateWholesaleServicesProcessRequest(WholesaleServicesProcess process)
    {
        var request = new WholesaleServicesRequest()
        {
            RequestedByActorId = process.RequestedByActorId.Value,
            RequestedByActorRole = process.RequestedByActorRoleCode,
            BusinessReason = process.BusinessReason.Code,
            PeriodStart = process.StartOfPeriod,
        };

        if (process.EndOfPeriod != null)
            request.PeriodEnd = process.EndOfPeriod;

        if (process.Resolution != null)
            request.Resolution = process.Resolution; // TODO: Introduce value type (?) and use .Name

        if (process.EnergySupplierId != null)
            request.EnergySupplierId = process.EnergySupplierId;

        if (process.ChargeOwner != null)
            request.ChargeOwnerId = process.ChargeOwner;

        if (process.GridAreaCode != null)
            request.GridAreaCode = process.GridAreaCode;

        if (process.SettlementVersion != null)
            request.SettlementSeriesVersion = process.SettlementVersion.Name;

        foreach (var chargeType in process.ChargeTypes)
        {
            request.ChargeTypes.Add(
                new ChargeType() { ChargeCode = chargeType.Id, ChargeType_ = chargeType.Type, });
        }

        return request;
    }
}
