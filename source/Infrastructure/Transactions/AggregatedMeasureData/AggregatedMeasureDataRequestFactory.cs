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
using Energinet.DataHub.EDI.Application.Configuration;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.Edi.Requests;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Energinet.DataHub.EDI.Infrastructure.Transactions.AggregatedMeasureData;

public class AggregatedMeasureDataRequestFactory
{
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

    public AggregatedMeasureDataRequestFactory(ISystemDateTimeProvider systemDateTimeProvider)
    {
        _systemDateTimeProvider = systemDateTimeProvider ?? throw new ArgumentNullException(nameof(systemDateTimeProvider));
    }

    public ServiceBusMessage CreateServiceBusMessage(AggregatedMeasureDataProcess process)
    {
        if (process == null) throw new ArgumentNullException(nameof(process));

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

    private static void MapGridArea(AggregatedTimeSeriesRequest request, AggregatedMeasureDataProcess process)
    {
        if (process.EnergySupplierId == null && process.BalanceResponsibleId == null)
        {
            if (string.IsNullOrWhiteSpace(process.MeteringGridAreaDomainId)) throw new InvalidOperationException("Missing grid area code for grid responsible");
            request.AggregationPerGridarea = new AggregationPerGridArea()
            {
                GridAreaCode = process.MeteringGridAreaDomainId,
                GridResponsibleId = process.RequestedByActorId.Value,
            };
        }
    }

    private static void MapEnergySupplierPerGridArea(
        AggregatedTimeSeriesRequest request,
        AggregatedMeasureDataProcess process)
    {
        if (process.EnergySupplierId != null && process.BalanceResponsibleId == null)
        {
            if (string.IsNullOrWhiteSpace(process.MeteringGridAreaDomainId)) throw new InvalidOperationException($"Missing grid area code for energy supplier: {process.EnergySupplierId}");
            request.AggregationPerEnergysupplierPerGridarea = new AggregationPerEnergySupplierPerGridArea()
            {
                GridAreaCode = process.MeteringGridAreaDomainId,
                EnergySupplierId = process.EnergySupplierId,
            };
        }
    }

    private static void MapEnergyPerBalancePerGridArea(
        AggregatedTimeSeriesRequest request,
        AggregatedMeasureDataProcess process)
    {
        if (process.EnergySupplierId != null && process.BalanceResponsibleId != null)
        {
            if (string.IsNullOrWhiteSpace(process.MeteringGridAreaDomainId)) throw new InvalidOperationException($"Missing grid area code for energy supplier: {process.EnergySupplierId} per balance responsible: {process.BalanceResponsibleId}");
            request.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea =
                new AggregationPerEnergySupplierPerBalanceResponsiblePartyPerGridArea()
                {
                    GridAreaCode = process.MeteringGridAreaDomainId,
                    BalanceResponsiblePartyId = process.BalanceResponsibleId,
                    EnergySupplierId = process.EnergySupplierId,
                };
        }
    }

    private static void MapBalanceResponsiblePerGridArea(
        AggregatedTimeSeriesRequest request,
        AggregatedMeasureDataProcess process)
    {
        if (process.EnergySupplierId == null && process.BalanceResponsibleId != null)
        {
            if (string.IsNullOrWhiteSpace(process.MeteringGridAreaDomainId)) throw new InvalidOperationException($"Missing grid area code for balance responsible: {process.BalanceResponsibleId}");
            request.AggregationPerBalanceresponsiblepartyPerGridarea =
                new AggregationPerBalanceResponsiblePartyPerGridArea()
                {
                    GridAreaCode = process.MeteringGridAreaDomainId,
                    BalanceResponsiblePartyId = process.BalanceResponsibleId,
                };
        }
    }

    private static TimeSeriesType MapTimeSeriesTypeAsGridOperator(AggregatedMeasureDataProcess process)
    {
        return process.MeteringPointType switch
        {
            "E18" => TimeSeriesType.Production,
            "E20" => TimeSeriesType.NetExchangePerGa,
            "E17" => process.SettlementMethod switch
            {
                "D01" => TimeSeriesType.NonProfiledConsumption,
                "E02" => TimeSeriesType.FlexConsumption,
                "" => TimeSeriesType.TotalConsumption,
                null => TimeSeriesType.TotalConsumption,
                _ => ThrowInvalidOperationExceptionForTimeSeries(process),
            },
            _ => ThrowInvalidOperationExceptionForTimeSeries(process),
        };
    }

    private static TimeSeriesType MapTimeSeriesTypeAsBalanceResponsibleOrEnergySupplier(AggregatedMeasureDataProcess process)
    {
        return process.MeteringPointType switch
        {
            "E18" => TimeSeriesType.Production,
            "E17" => process.SettlementMethod switch
            {
                "D01" => TimeSeriesType.NonProfiledConsumption,
                "E02" => TimeSeriesType.FlexConsumption,
                _ => ThrowInvalidOperationExceptionForTimeSeries(process),
            },
            _ => ThrowInvalidOperationExceptionForTimeSeries(process),
        };
    }

    private static TimeSeriesType ThrowInvalidOperationExceptionForTimeSeries(AggregatedMeasureDataProcess process)
    {
        throw new InvalidOperationException(
            $"Unknown time series type for metering point type {process.MeteringPointType}" +
            $" and settlement method {process.SettlementMethod}" +
            $"as a {MarketRole.FromCode(process.RequestedByActorRoleCode).Name}");
    }

    private IMessage CreateAggregatedMeasureDataRequest(AggregatedMeasureDataProcess process)
    {
        var request = new AggregatedTimeSeriesRequest()
        {
            Period = MapPeriod(process),
            TimeSeriesType = process.RequestedByActorRoleCode == MarketRole.MeteredDataResponsible.Code ||
                             process.RequestedByActorRoleCode == MarketRole.GridOperator.Code
                ? MapTimeSeriesTypeAsGridOperator(process)
                : MapTimeSeriesTypeAsBalanceResponsibleOrEnergySupplier(process),
        };

        MapGridArea(request, process);
        MapEnergySupplierPerGridArea(request, process);
        MapBalanceResponsiblePerGridArea(request, process);
        MapEnergyPerBalancePerGridArea(request, process);

        return request;
    }

    private Energinet.DataHub.Edi.Requests.Period MapPeriod(AggregatedMeasureDataProcess process)
    {
        return new Energinet.DataHub.Edi.Requests.Period()
        {
            StartOfPeriod = new Timestamp() { Seconds = process.StartOfPeriod.ToUnixTimeSeconds(), },
            EndOfPeriod = new Timestamp() { Seconds = process.EndOfPeriod?.ToUnixTimeSeconds() ?? _systemDateTimeProvider.Now().ToUnixTimeSeconds(), },
        };
    }
}
