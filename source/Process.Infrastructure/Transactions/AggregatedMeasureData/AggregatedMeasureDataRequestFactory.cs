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
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.Edi.Requests;
using Google.Protobuf;
using Period = Energinet.DataHub.Edi.Requests.Period;

namespace Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData;

public static class AggregatedMeasureDataRequestFactory
{
    public static ServiceBusMessage CreateServiceBusMessage(AggregatedMeasureDataProcess process)
    {
        ArgumentNullException.ThrowIfNull(process);

        var body = CreateAggregatedMeasureDataRequest(process);

        var message = new ServiceBusMessage()
        {
            Body = new BinaryData(body.ToByteArray()),
            Subject = body.GetType().Name,
            MessageId = process.ProcessId.Id.ToString(),
        };
        message.ApplicationProperties.Add("ReferenceId", process.ProcessId.Id.ToString());
        return message;
    }

    private static Period MapPeriod(AggregatedMeasureDataProcess process)
    {
        var period = new Period
        {
            Start = process.StartOfPeriod,
        };

        if (process.EndOfPeriod != null)
            period.End = process.EndOfPeriod;

        return period;
    }

    private static AggregatedTimeSeriesRequest CreateAggregatedMeasureDataRequest(AggregatedMeasureDataProcess process)
    {
        var request = new AggregatedTimeSeriesRequest()
        {
            Period = MapPeriod(process),
            RequestedByActorId = process.RequestedByActorId.Value,
            RequestedByActorRole = process.RequestedByActorRoleCode,
            BusinessReason = process.BusinessReason.Name,
        };

        if (process.MeteringPointType != null)
        {
            request.MeteringPointType = process.MeteringPointType; // TODO: Introduce value type and use .Name
        }

        if (process.SettlementMethod != null)
            request.SettlementMethod = process.SettlementMethod; // TODO: Introduce value type and use .Name

        if (process.EnergySupplierId != null)
            request.EnergySupplierId = process.EnergySupplierId;

        if (process.MeteringGridAreaDomainId != null)
            request.GridAreaCode = process.MeteringGridAreaDomainId;

        if (process.BalanceResponsibleId != null)
            request.BalanceResponsibleId = process.BalanceResponsibleId;

        if (process.SettlementVersion != null)
            request.SettlementSeriesVersion = process.SettlementVersion.Name;

        if (process.MeteringGridAreaDomainId != null)
            request.GridAreaCode = process.MeteringGridAreaDomainId;

        return request;
    }
}
