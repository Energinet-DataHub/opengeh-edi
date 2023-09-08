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
using Application.Configuration;
using Azure.Messaging.ServiceBus;
using Domain.Actors;
using Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.Edi.Requests;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Infrastructure.Transactions.AggregatedMeasureData;

public class AggregatedMeasureDataResponseFactory
{
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

    public AggregatedMeasureDataResponseFactory(ISystemDateTimeProvider systemDateTimeProvider)
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

    private static bool IsValidRequestFromGridOperator(AggregatedMeasureDataProcess process)
    {
        return IsForGridArea(process)
               && (process.RequestedByActorRoleCode == MarketRole.MeteredDataResponsible.Code
                   || process.RequestedByActorRoleCode == MarketRole.GridOperator.Code);
    }

    private static bool IsForGridArea(AggregatedMeasureDataProcess process)
    {
        return process.EnergySupplierId is null && process.BalanceResponsibleId is null;
    }

    private static AggregationPerGridArea MapGridArea(AggregatedMeasureDataProcess process)
    {
        return new AggregationPerGridArea()
        {
            GridAreaCode = process.MeteringGridAreaDomainId,
            GridResponsibleId = process.RequestedByActorId.Value,
        };
    }

    private static AggregationPerEnergySupplierPerGridArea MapEnergySupplierPerGridArea(AggregatedMeasureDataProcess process)
    {
        return new AggregationPerEnergySupplierPerGridArea()
        {
            GridAreaCode = process.MeteringGridAreaDomainId,
            BalanceResponsiblePartyId = string.Empty,
            EnergySupplierId = process.EnergySupplierId,
        };
    }

    private static AggregationPerEnergySupplierPerBalanceResponsiblePartyPerGridArea MapEnergyAndBalancePerGridArea(AggregatedMeasureDataProcess process)
    {
        return new AggregationPerEnergySupplierPerBalanceResponsiblePartyPerGridArea()
        {
            GridAreaCode = process.MeteringGridAreaDomainId,
            BalanceResponsiblePartyId = process.BalanceResponsibleId,
            EnergySupplierId = process.EnergySupplierId,
        };
    }

    private static AggregationPerBalanceResponsiblePartyPerGridArea MapBalancePerGridArea(AggregatedMeasureDataProcess process)
    {
        return new AggregationPerBalanceResponsiblePartyPerGridArea()
        {
            GridAreaCode = process.MeteringGridAreaDomainId,
            BalanceResponsiblePartyId = process.BalanceResponsibleId,
            EnergySupplierId = string.Empty,
        };
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
                null => TimeSeriesType.TotalConsumption,
                _ => throw TimeSeriesException(process),
            },
            _ => throw TimeSeriesException(process),
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
                _ => throw TimeSeriesException(process),
            },
            _ => throw TimeSeriesException(process),
        };
    }

    private static InvalidOperationException TimeSeriesException(AggregatedMeasureDataProcess process)
    {
        return new InvalidOperationException(
            $"Unknown time series type for metering point type {process.MeteringPointType}" +
            $" and settlement method {process.SettlementMethod}" +
            $"as a {process.RequestedByActorRoleCode}");
    }

    private IMessage CreateAggregatedMeasureDataRequest(AggregatedMeasureDataProcess process)
    {
        var response = new AggregatedTimeSeriesRequest()
        {
            Period = MapPeriod(process),
            TimeSeriesType = process.RequestedByActorRoleCode == MarketRole.MeteredDataResponsible.Code ||
                             process.RequestedByActorRoleCode == MarketRole.GridOperator.Code
                ? MapTimeSeriesTypeAsGridOperator(process)
                : MapTimeSeriesTypeAsBalanceResponsibleOrEnergySupplier(process),
        };

        if (IsValidRequestFromGridOperator(process))
        {
            response.AggregationPerGridarea = MapGridArea(process);
            return response;
        }

        throw new InvalidOperationException(
            $"Invalid request by actor: {process.RequestedByActorId.Value}. " +
            $"The combination of actor code: {process.RequestedByActorRoleCode}, " +
            $"energy supplier id: {process.EnergySupplierId}," +
            $"balance responsible id: {process.BalanceResponsibleId} is invalid");
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
