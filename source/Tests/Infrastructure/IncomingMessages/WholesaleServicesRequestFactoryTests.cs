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
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Interfaces;
using Energinet.DataHub.Edi.Requests;
using FluentAssertions;
using Xunit;
using ChargeType = Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices.ChargeType;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.IncomingMessages;

public class WholesaleServicesRequestFactoryTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("ASD", "D03")]
    [InlineData("ASD", null)]
    [InlineData(null, "D03")]
    public void Given_ProcessWithNullableChargeTypeIdAndType_When_CreateServiceBusMessage_Then_ServiceBusMessageIsCreated(
        string? id,
        string? type)
    {
        // Arrange
        var requestedByActor = RequestedByActor.From(ActorNumber.Create("1111111111111"), ActorRole.GridOperator);

        var process = new WholesaleServicesProcess(
            ProcessId.New(),
            requestedByActor,
            OriginalActor.From(requestedByActor),
            TransactionId.From("85f00b2e-cbfa-4b17-86e0-b9004d683f9f"),
            MessageId.Create("9b6184af-2f05-40b9-d783-08dc814df95a"),
            BusinessReason.WholesaleFixing,
            "2023-04-30T22:00:00Z",
            "2023-05-31T22:00:00Z",
            "904",
            null,
            null,
            null,
            "2222222222222",
            [new ChargeType(ChargeTypeId.New(), id, type)],
            ["904"]);

        // Act
        var serviceBusMessage = WholesaleServicesRequestFactory.CreateServiceBusMessage(process);

        // Assert
        serviceBusMessage.Should().NotBeNull();
        serviceBusMessage.Body.Should().NotBeNull();
        serviceBusMessage.Subject.Should().Be(nameof(WholesaleServicesRequest));

        var chargeType = WholesaleServicesRequest.Parser.ParseFrom(serviceBusMessage.Body)
            .ChargeTypes
            .Should()
            .ContainSingle()
            .Subject;

        chargeType.ChargeCode.Should().Be(id ?? string.Empty);
        chargeType.ChargeType_.Should().Be(type is null ? string.Empty : "Tariff");
    }
}
