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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.BaseParsers;
using Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;
using Energinet.DataHub.EDI.Process.Interfaces;
using NodaTime;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages.RequestAggregatedMeasureData;

public class InitializeAggregatedMeasureDataProcessDtoBuilder
{
    private readonly string _startDateTime = "2022-06-17T22:00:00Z";
    private readonly string _endDateTime = "2022-07-22T22:00:00Z";
    private readonly string _messageType = "E74";
    private readonly string _businessType = "23";
    private readonly BusinessReason _businessReason = BusinessReason.PreliminaryAggregation;
    private readonly ActorNumber _receiverId = DataHubDetails.DataHubActorNumber;
    private readonly ActorRole _receiverRole = ActorRole.MeteredDataAdministrator;
    private readonly string _createdAt = SystemClock.Instance.GetCurrentInstant().ToString();
    private string? _requestedGridArea = "244";
    private List<string> _gridAreas = new() { "244" };
    private ActorNumber _senderId = ActorNumber.Create(SampleData.NewEnergySupplierNumber);
    private string? _settlementVersion;
    private string _messageId = Guid.NewGuid().ToString();
    private string _seriesId = Guid.NewGuid().ToString();
    private ActorRole _senderRole = ActorRole.EnergySupplier;
    private string? _meteringPointType = MeteringPointType.Consumption.Code;
    private string? _settlementMethod = SettlementMethod.Flex.Code;
    private string? _energySupplierMarketParticipantId = SampleData.NewEnergySupplierNumber;
    private string? _balanceResponsiblePartyMarketParticipantId = "5799999933318";

    public InitializeAggregatedMeasureDataProcessDtoBuilder SetMessageId(string id)
    {
        _messageId = id;
        return this;
    }

    public InitializeAggregatedMeasureDataProcessDtoBuilder SetMeteringPointType(string? meteringPointType)
    {
        _meteringPointType = meteringPointType;
        return this;
    }

    public InitializeAggregatedMeasureDataProcessDtoBuilder SetSenderRole(string senderRole)
    {
        _senderRole = ActorRole.FromCode(senderRole);
        return this;
    }

    public InitializeAggregatedMeasureDataProcessDtoBuilder SetSettlementVersion(string? settlementVersion)
    {
        _settlementVersion = settlementVersion;
        return this;
    }

    public InitializeAggregatedMeasureDataProcessDtoBuilder SetSettlementMethod(string? settlementMethod)
    {
        _settlementMethod = settlementMethod;
        return this;
    }

    public InitializeAggregatedMeasureDataProcessDtoBuilder SetEnergySupplierId(string? energySupplierId)
    {
        _energySupplierMarketParticipantId = energySupplierId;
        return this;
    }

    public InitializeAggregatedMeasureDataProcessDtoBuilder SetBalanceResponsibleId(string? balanceResponsibleId)
    {
        _balanceResponsiblePartyMarketParticipantId = balanceResponsibleId;
        return this;
    }

    public InitializeAggregatedMeasureDataProcessDtoBuilder SetTransactionId(string transactionId)
    {
        _seriesId = transactionId;
        return this;
    }

    public InitializeAggregatedMeasureDataProcessDtoBuilder SetSenderId(string senderId)
    {
        _senderId = ActorNumber.Create(senderId);
        return this;
    }

    public InitializeAggregatedMeasureDataProcessDtoBuilder SetRequestedGridArea(string? requestedGridArea)
    {
        _requestedGridArea = requestedGridArea;
        return this;
    }

    public InitializeAggregatedMeasureDataProcessDtoBuilder SetGridAreas(string[] gridAreas)
    {
        _gridAreas = gridAreas.ToList();
        return this;
    }

    internal InitializeAggregatedMeasureDataProcessDto Build()
    {
        var header = CreateHeader();
        return new InitializeAggregatedMeasureDataProcessDto(
            header.SenderId,
            header.SenderRole,
            header.BusinessReason,
            header.MessageId,
            new List<InitializeAggregatedMeasureDataProcessSeries> { CreateSeries() }.AsReadOnly());
    }

    private InitializeAggregatedMeasureDataProcessSeries CreateSeries() =>
        new(
            TransactionId.From(_seriesId),
            _meteringPointType,
            _settlementMethod,
            _startDateTime,
            _endDateTime,
            _requestedGridArea,
            _energySupplierMarketParticipantId,
            _balanceResponsiblePartyMarketParticipantId,
            _settlementVersion,
            _gridAreas,
            RequestedByActor.From(_senderId, _senderRole),
            OriginalActor.From(_senderId, _senderRole));

    private MessageHeader CreateHeader()
    {
        return new MessageHeader(
            _messageId,
            _messageType,
            _businessReason.Code,
            _senderId.Value,
            _senderRole.Code,
            _receiverId.Value,
            _receiverRole.Code,
            _createdAt,
            _businessType);
    }
}
