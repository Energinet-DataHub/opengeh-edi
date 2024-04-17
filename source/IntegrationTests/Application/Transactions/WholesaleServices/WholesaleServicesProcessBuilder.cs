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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using ChargeType = Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices.ChargeType;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.WholesaleServices;

public class WholesaleServicesProcessBuilder
{
    private readonly ProcessId _processId = ProcessId.New();
    private readonly string _startDateAndOrTimeDateTime = "2022-06-17T22:00:00Z";
    private readonly string _endDateAndOrTimeDateTime = "2022-07-22T22:00:00Z";
    private readonly string _gridArea = "244";
    private readonly BusinessReason _businessReason = BusinessReason.WholesaleFixing;
    private readonly string? _resolution = Resolution.Hourly.Code;
    private readonly string? _chargeOwner = ActorNumber.Create("5790000000002").Value;
    private readonly string? _chargeTypeId = "EA-001";
    private readonly string? _chargeTypeType = "D03";
    private readonly ActorRole _senderRole = ActorRole.EnergySupplier;
    private readonly MessageId _messageId = MessageId.New();
    private BusinessTransactionId _businessTransactionId = BusinessTransactionId.Create("1234");
    private SettlementVersion? _settlementVersion;
    private ActorNumber _senderNumber = ActorNumber.Create("5790000000000");
    private string? _energySupplierMarketParticipantId = ActorNumber.Create("5790000000001").Value;
    private WholesaleServicesProcess.State _state;

    public WholesaleServicesProcessBuilder SetEnergySupplierId(string? energySupplierId = null)
    {
        _energySupplierMarketParticipantId = energySupplierId;
        return this;
    }

    public WholesaleServicesProcessBuilder SetSenderId(string s)
    {
        _senderNumber = ActorNumber.Create(s);
        return this;
    }

    public WholesaleServicesProcessBuilder SetSettlementVersion(SettlementVersion settlementVersion)
    {
        _settlementVersion = settlementVersion;
        return this;
    }

    public WholesaleServicesProcessBuilder SetState(WholesaleServicesProcess.State state)
    {
        _state = state;
        return this;
    }

    public WholesaleServicesProcessBuilder SetBusinessTransactionId(Guid transactionId)
    {
        _businessTransactionId = BusinessTransactionId.Create(transactionId.ToString());
        return this;
    }

    internal WholesaleServicesProcess Build()
    {
        var chargeTypes = BuildChargeTypes();

        var process = new WholesaleServicesProcess(
            _processId,
            _senderNumber,
            _senderRole,
            _businessTransactionId,
            _messageId,
            _businessReason,
            _startDateAndOrTimeDateTime,
            _endDateAndOrTimeDateTime,
            _gridArea,
            _energySupplierMarketParticipantId,
            _settlementVersion,
            _resolution,
            _chargeOwner,
            chargeTypes.AsReadOnly(),
            new List<string> { _gridArea });

        var prop = process.GetType().GetField(
            "_state", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        prop!.SetValue(process, _state);

        return process;
    }

    private List<ChargeType> BuildChargeTypes() => new() { new(ChargeTypeId.New(), _chargeTypeId, _chargeTypeType), };
}
