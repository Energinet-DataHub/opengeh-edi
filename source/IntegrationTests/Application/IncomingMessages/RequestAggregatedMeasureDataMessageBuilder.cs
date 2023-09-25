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
using Energinet.DataHub.EDI.Application.IncomingMessages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.MarketTransactions;
using NodaTime;
using MessageHeader = Energinet.DataHub.EDI.MarketTransactions.MessageHeader;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages;

public class RequestAggregatedMeasureDataMessageBuilder
{
    private const string NotSet = "NotSet";
    private readonly string _serieId = "123353185";
    private readonly string _startDateAndOrTimeDateTime = "2022-06-17T22:00:00Z";
    private readonly string _endDateAndOrTimeDateTime = "2022-07-22T22:00:00Z";
    private readonly string _meteringGridAreaDomainId = "244";
    private readonly string _messageType = NotSet;
    private readonly string _processType = "D03";
    private readonly string _senderId = "1234567891234567";
    private readonly ActorNumber _receiverId = DataHubDetails.IdentificationNumber;
    private readonly string _receiverRole = "DDQ";
    private readonly string _createdAt = SystemClock.Instance.GetCurrentInstant().ToString();
    private readonly string _messageId = Guid.NewGuid().ToString();
    private string _senderRole = "DDZ";
    private string _marketEvaluationPointType = "E17";
    private string? _marketEvaluationSettlementMethod = "D01";
    private string? _energySupplierMarketParticipantId = "5790001330552";
    private string? _balanceResponsiblePartyMarketParticipantId = "5799999933318";

    public RequestAggregatedMeasureDataMessageBuilder SetMarketEvaluationPointType(string marketEvaluationPointType)
    {
        _marketEvaluationPointType = marketEvaluationPointType;
        return this;
    }

    public RequestAggregatedMeasureDataMessageBuilder SetSenderRole(string senderRole)
    {
        _senderRole = senderRole;
        return this;
    }

    public RequestAggregatedMeasureDataMessageBuilder SetMarketEvaluationSettlementMethod(string? marketEvaluationSettlementMethod = null)
    {
        _marketEvaluationSettlementMethod = marketEvaluationSettlementMethod;
        return this;
    }

    public RequestAggregatedMeasureDataMessageBuilder SetEnergySupplierId(string? energySupplierId = null)
    {
        _energySupplierMarketParticipantId = energySupplierId;
        return this;
    }

    public RequestAggregatedMeasureDataMessageBuilder SetBalanceResponsibleId(string? balanceResponsibleId = null)
    {
        _balanceResponsiblePartyMarketParticipantId = balanceResponsibleId;
        return this;
    }

    internal RequestAggregatedMeasureDataTransactionCommand Build()
    {
        return new RequestAggregatedMeasureDataTransactionCommand(
            CreateHeader(),
            CreateSerieCreateRecord());
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
            _balanceResponsiblePartyMarketParticipantId);

    private MessageHeader CreateHeader()
    {
        return new MessageHeader(
            _messageId,
            _messageType,
            _processType,
            _senderId,
            _senderRole,
            _receiverId.Value,
            _receiverRole,
            _createdAt);
    }
}
