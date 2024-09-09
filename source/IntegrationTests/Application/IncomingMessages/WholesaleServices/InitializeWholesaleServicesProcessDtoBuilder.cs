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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;
using Energinet.DataHub.EDI.Process.Interfaces;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages.WholesaleServices;

public class InitializeWholesaleServicesProcessDtoBuilder
{
    private readonly string _startDateAndOrTimeDateTime = "2022-06-17T22:00:00Z";
    private readonly string _endDateAndOrTimeDateTime = "2022-07-22T22:00:00Z";
    private readonly string _gridArea = "244";
    private readonly string? _resolution = Resolution.Hourly.Code;
    private readonly string? _chargeOwner = SampleData.ChargeOwner;
    private readonly string? _chargeTypeId = "EA-001";
    private readonly string? _chargeTypeType = "D03";
    private string _businessReason = BusinessReason.WholesaleFixing.Code;
    private ActorNumber _senderId = ActorNumber.Create(SampleData.NewEnergySupplierNumber);
    private string? _settlementVersion;
    private string _messageId = Guid.NewGuid().ToString();
    private string _seriesId = Guid.NewGuid().ToString();
    private ActorRole _senderRole = ActorRole.EnergySupplier;
    private string? _energySupplierMarketParticipantId = SampleData.NewEnergySupplierNumber;

    public InitializeWholesaleServicesProcessDtoBuilder SetMessageId(string id)
    {
        _messageId = id;
        return this;
    }

    public InitializeWholesaleServicesProcessDtoBuilder SetSenderRole(string senderRoleCode)
    {
        _senderRole = ActorRole.FromCode(senderRoleCode);
        return this;
    }

    public InitializeWholesaleServicesProcessDtoBuilder SetSettlementVersion(string? settlementVersion)
    {
        _settlementVersion = settlementVersion;
        return this;
    }

    public InitializeWholesaleServicesProcessDtoBuilder SetEnergySupplierId(string? energySupplierId = null)
    {
        _energySupplierMarketParticipantId = energySupplierId;
        return this;
    }

    public InitializeWholesaleServicesProcessDtoBuilder SetTransactionId(string transactionId)
    {
        _seriesId = transactionId;
        return this;
    }

    public InitializeWholesaleServicesProcessDtoBuilder SetSenderId(string senderNumber)
    {
        _senderId = ActorNumber.Create(senderNumber);
        return this;
    }

    public InitializeWholesaleServicesProcessDtoBuilder SetBusinessReason(string businessReason)
    {
        _businessReason = businessReason;
        return this;
    }

    internal InitializeWholesaleServicesProcessDto Build()
    {
        return new InitializeWholesaleServicesProcessDto(
            _businessReason,
            _messageId,
            new List<InitializeWholesaleServicesSeries> { CreateSeries() }.AsReadOnly());
    }

    private InitializeWholesaleServicesSeries CreateSeries() =>
        new(
            _seriesId,
            _startDateAndOrTimeDateTime,
            _endDateAndOrTimeDateTime,
            _gridArea,
            _energySupplierMarketParticipantId,
            _settlementVersion,
            _resolution,
            _chargeOwner,
            new List<InitializeWholesaleServicesChargeType> { CreateChargeType() }.AsReadOnly(),
            new List<string> { _gridArea },
            RequestedByActor.From(_senderId, _senderRole),
            OriginalActor.From(_senderId, _senderRole));

    private InitializeWholesaleServicesChargeType CreateChargeType() =>
        new(_chargeTypeId, _chargeTypeType);
}
