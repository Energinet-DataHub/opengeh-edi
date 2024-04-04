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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;
using Energinet.DataHub.EDI.Process.Interfaces;
using NodaTime;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages.WholesaleServices;

public class InitializeWholesaleServicesProcessDtoBuilder
{
    private readonly string _startDateAndOrTimeDateTime = "2022-06-17T22:00:00Z";
    private readonly string _endDateAndOrTimeDateTime = "2022-07-22T22:00:00Z";
    private readonly string _meteringGridAreaDomainId = "244";
    private readonly string _messageType = "E74";
    private readonly string _businessType = "23";
    private readonly string _receiverId = DataHubDetails.DataHubActorNumber.Value;
    private readonly string _receiverRole = ActorRole.MeteredDataAdministrator.Code;
    private readonly string _createdAt = SystemClock.Instance.GetCurrentInstant().ToString();
    private readonly string? _resolution = Resolution.Hourly.Code;
    private readonly string? _chargeOwner = SampleData.ChargeOwner;
    private readonly string? _chargeTypeId = "EA-001";
    private readonly string? _chargeTypeType = "D03";
    private string _businessReason = BusinessReason.WholesaleFixing.Code;
    private string _senderId = SampleData.NewEnergySupplierNumber;
    private string? _settlementVersion;
    private string _messageId = Guid.NewGuid().ToString();
    private string _serieId = Guid.NewGuid().ToString();
    private string _senderRole = ActorRole.EnergySupplier.Code;
    private string? _energySupplierMarketParticipantId = SampleData.NewEnergySupplierNumber;

    public InitializeWholesaleServicesProcessDtoBuilder SetMessageId(string id)
    {
        _messageId = id;
        return this;
    }

    public InitializeWholesaleServicesProcessDtoBuilder SetSenderRole(string senderRole)
    {
        _senderRole = senderRole;
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
        _serieId = transactionId;
        return this;
    }

    public InitializeWholesaleServicesProcessDtoBuilder SetSenderId(string s)
    {
        _senderId = s;
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
            _senderId,
            _senderRole,
            _receiverId,
            _receiverRole,
            _businessReason,
            _messageType,
            _messageId,
            _createdAt,
            _businessType,
            new List<InitializeWholesaleServicesSeries> { CreateSerie() }.AsReadOnly());
    }

    private InitializeWholesaleServicesSeries CreateSerie() =>
        new(
            _serieId,
            _startDateAndOrTimeDateTime,
            _endDateAndOrTimeDateTime,
            _meteringGridAreaDomainId,
            _energySupplierMarketParticipantId,
            _settlementVersion,
            _resolution,
            _chargeOwner,
            new List<InitializeWholesaleServicesChargeType> { CreateChargeType() }.AsReadOnly());

    private InitializeWholesaleServicesChargeType CreateChargeType() =>
        new(_chargeTypeId, _chargeTypeType);
}
