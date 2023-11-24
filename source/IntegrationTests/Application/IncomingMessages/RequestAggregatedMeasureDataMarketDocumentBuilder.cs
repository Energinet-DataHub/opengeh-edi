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
using System.Collections.Generic;
using Energinet.DataHub.EDI.BuildingBlocks.Domain;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Actors;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Infrastructure.IncomingMessages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Energinet.DataHub.EDI.Process.Interfaces;
using NodaTime;
using MessageHeader = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.MessageHeader;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages;

public class RequestAggregatedMeasureDataMarketDocumentBuilder
{
    private readonly string _startDateAndOrTimeDateTime = "2022-06-17T22:00:00Z";
    private readonly string _endDateAndOrTimeDateTime = "2022-07-22T22:00:00Z";
    private readonly string _meteringGridAreaDomainId = "244";
    private readonly string _messageType = "E74";
    private readonly string _businessType = "23";
    private readonly BusinessReason _businessReason = BusinessReason.PreliminaryAggregation;
    private readonly ActorNumber _receiverId = DataHubDetails.IdentificationNumber;
    private readonly MarketRole _receiverRole = MarketRole.CalculationResponsibleRole;
    private readonly string _createdAt = SystemClock.Instance.GetCurrentInstant().ToString();
    private string _senderId = SampleData.NewEnergySupplierNumber;
    private string? _settlementVersion;
    private string _messageId = Guid.NewGuid().ToString();
    private string _serieId = Guid.NewGuid().ToString();
    private string _senderRole = MarketRole.EnergySupplier.Code;
    private string _marketEvaluationPointType = "E17";
    private string? _marketEvaluationSettlementMethod = "D01";
    private string? _energySupplierMarketParticipantId = SampleData.NewEnergySupplierNumber;
    private string? _balanceResponsiblePartyMarketParticipantId = "5799999933318";

    public RequestAggregatedMeasureDataMarketDocumentBuilder SetMessageId(string id)
    {
        _messageId = id;
        return this;
    }

    public RequestAggregatedMeasureDataMarketDocumentBuilder SetMarketEvaluationPointType(string marketEvaluationPointType)
    {
        _marketEvaluationPointType = marketEvaluationPointType;
        return this;
    }

    public RequestAggregatedMeasureDataMarketDocumentBuilder SetSenderRole(string senderRole)
    {
        _senderRole = senderRole;
        return this;
    }

    public RequestAggregatedMeasureDataMarketDocumentBuilder SetSettlementVersion(string? settlementVersion)
    {
        _settlementVersion = settlementVersion;
        return this;
    }

    public RequestAggregatedMeasureDataMarketDocumentBuilder SetMarketEvaluationSettlementMethod(string? marketEvaluationSettlementMethod = null)
    {
        _marketEvaluationSettlementMethod = marketEvaluationSettlementMethod;
        return this;
    }

    public RequestAggregatedMeasureDataMarketDocumentBuilder SetEnergySupplierId(string? energySupplierId = null)
    {
        _energySupplierMarketParticipantId = energySupplierId;
        return this;
    }

    public RequestAggregatedMeasureDataMarketDocumentBuilder SetBalanceResponsibleId(string? balanceResponsibleId = null)
    {
        _balanceResponsiblePartyMarketParticipantId = balanceResponsibleId;
        return this;
    }

    public RequestAggregatedMeasureDataMarketDocumentBuilder SetTransactionId(string transactionId)
    {
        _serieId = transactionId;
        return this;
    }

    public RequestAggregatedMeasureDataMarketDocumentBuilder SetSenderId(string s)
    {
        _senderId = s;
        return this;
    }

    internal RequestAggregatedMeasureDataMarketMessage Build()
    {
        var messageParser = new RequestAggregatedMeasureDataMarketMessageParserResult(
            RequestAggregatedMeasureDataMarketMessageFactory.Create(
                CreateHeader(),
                new List<Serie> { CreateSerieCreateRecord() }.AsReadOnly()));
        return messageParser.MarketMessage!;
    }

    private Serie CreateSerieCreateRecord() =>
        new(
            _serieId,
            _marketEvaluationPointType,
            _marketEvaluationSettlementMethod,
            _startDateAndOrTimeDateTime,
            _endDateAndOrTimeDateTime,
            _meteringGridAreaDomainId,
            _energySupplierMarketParticipantId,
            _balanceResponsiblePartyMarketParticipantId,
            _settlementVersion);

    private MessageHeader CreateHeader()
    {
        return new MessageHeader(
            _messageId,
            _messageType,
            CimCode.Of(_businessReason),
            _senderId,
            _senderRole,
            _receiverId.Value,
            _receiverRole.Code,
            _createdAt,
            _businessType,
            _senderId,
            _senderRole);
    }
}
