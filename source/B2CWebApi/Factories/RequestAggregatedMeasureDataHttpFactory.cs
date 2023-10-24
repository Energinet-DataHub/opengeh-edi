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

using Energinet.DataHub.EDI.B2CWebApi.Models;
using Energinet.DataHub.Edi.Requests;
using NodaTime;

namespace Energinet.DataHub.EDI.B2CWebApi.Factories;

public static class RequestAggregatedMeasureDataHttpFactory
{
    public static RequestAggregatedMeasureData Create(
        RequestAggregatedMeasureDataMarketRequest request,
        string actorNumber,
        string role,
        DateTimeZone dateTimeZone)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var senderRoleCode = MapRoleNameToCode(role);
        var data = new RequestAggregatedMeasureData
        {
            MessageId = Guid.NewGuid().ToString(),
            SenderId = actorNumber,
            SenderRoleCode = senderRoleCode,
            ReceiverId = "5790001330552",
            ReceiverRoleCode = MarketRole.CalculationResponsibleRole.Code,
            AuthenticatedUser = actorNumber,
            AuthenticatedUserRoleCode = senderRoleCode,
            BusinessReason = MapToBusinessReasonCode(request.ProcessType),
            MessageType = "E74",
        };

        var serie = new Serie
        {
            Id = Guid.NewGuid().ToString(),
            StartDateAndOrTimeDateTime = InstantFormatFactory.SetInstantToMidnight(request.StartDate, dateTimeZone),
            EndDateAndOrTimeDateTime = InstantFormatFactory.SetInstantToMidnight(request.EndDate, dateTimeZone),
        };

        if (request.GridArea != null)
        {
            serie.MeteringGridAreaDomainId = request.GridArea;
        }

        if (request.EnergySupplierId != null)
        {
            serie.EnergySupplierMarketParticipantId = request.EnergySupplierId;
        }

        if (request.BalanceResponsibleId != null)
        {
            serie.BalanceResponsiblePartyMarketParticipantId = request.BalanceResponsibleId;
        }

        if (request.ProcessType == ProcessType.FirstCorrection || request.ProcessType == ProcessType.SecondCorrection || request.ProcessType == ProcessType.ThirdCorrection)
        {
            serie.SettlementSeriesVersion = SetSettlementSeriesVersion(request.ProcessType);
        }

        MapEvaluationPointTypeAndSettlementMethod(serie, request);

        data.Series.Add(serie);

        return data;
    }

    private static string SetSettlementSeriesVersion(ProcessType processType)
    {
        if (processType == ProcessType.FirstCorrection)
        {
            return "D01";
        }

        if (processType == ProcessType.SecondCorrection)
        {
            return "D02";
        }

        if (processType == ProcessType.ThirdCorrection)
        {
            return "D03";
        }

        throw new ArgumentOutOfRangeException(nameof(processType), processType, "Unknown ProcessType for setting SettlementSeriesVersion");
    }

    private static string MapToBusinessReasonCode(ProcessType requestProcessType)
    {
        return requestProcessType switch
        {
            ProcessType.PreliminaryAggregation => "D03",
            ProcessType.BalanceFixing => "D04",
            ProcessType.WholesaleFixing => "D05",
            ProcessType.FirstCorrection => "D32",
            ProcessType.SecondCorrection => "D32",
            ProcessType.ThirdCorrection => "D32",
            _ => throw new ArgumentOutOfRangeException(nameof(requestProcessType), requestProcessType, "Unknown ProcessType"),
        };
    }

    private static void MapEvaluationPointTypeAndSettlementMethod(Serie serie, RequestAggregatedMeasureDataMarketRequest request)
    {
        switch (request.MeteringPointType)
        {
            case MeteringPointType.Production:
                serie.MarketEvaluationPointType = "E18";
                break;
            case MeteringPointType.FlexConsumption:
                serie.MarketEvaluationPointType = "E17";
                serie.MarketEvaluationSettlementMethod = "D01";
                break;
            case MeteringPointType.TotalConsumption:
                serie.MarketEvaluationPointType = "E17";
                break;
            case MeteringPointType.NonProfiledConsumption:
                serie.MarketEvaluationPointType = "E17";
                serie.MarketEvaluationSettlementMethod = "E02";
                break;
            case MeteringPointType.Exchange:
                serie.MarketEvaluationPointType = "E20";
                break;
        }
    }

    private static string MapRoleNameToCode(string roleName)
    {
        ArgumentException.ThrowIfNullOrEmpty(roleName);

        if (roleName.Equals(MarketRole.MeteredDataResponsible.Name, StringComparison.OrdinalIgnoreCase))
        {
            return MarketRole.MeteredDataResponsible.Code;
        }

        if (roleName.Equals(MarketRole.EnergySupplier.Name, StringComparison.OrdinalIgnoreCase))
        {
            return MarketRole.EnergySupplier.Code;
        }

        if (roleName.Equals(MarketRole.BalanceResponsibleParty.Name, StringComparison.OrdinalIgnoreCase))
        {
            return MarketRole.BalanceResponsibleParty.Code;
        }

        throw new ArgumentException($"roleName: {roleName}. is unsupported to map to a role name");
    }
}
