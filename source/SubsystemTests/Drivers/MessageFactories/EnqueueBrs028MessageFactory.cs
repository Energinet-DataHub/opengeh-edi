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

using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Components.Abstractions.BusinessValidation;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028.V1.Model;
using Google.Protobuf;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers.MessageFactories;

public class EnqueueBrs028MessageFactory
{
    private static readonly string _orchestrationName = "BRS_028";

    public static ServiceBusMessage CreateAccept(
        Actor actor,
        string gridArea)
    {
        // TODO: Correct values, such that there exists data
        var accepted = new RequestCalculatedWholesaleServicesAcceptedV1(
            OriginalActorMessageId: Guid.NewGuid().ToString(),
            OriginalTransactionId: Guid.NewGuid().ToString(),
            RequestedForActorNumber: ProcessManager.Components.Abstractions.ValueObjects.ActorNumber.Create(actor.ActorNumber.Value),
            RequestedForActorRole: ProcessManager.Components.Abstractions.ValueObjects.ActorRole.FromName(actor.ActorRole.Name),
            RequestedByActorNumber: ProcessManager.Components.Abstractions.ValueObjects.ActorNumber.Create(actor.ActorNumber.Value),
            RequestedByActorRole: ProcessManager.Components.Abstractions.ValueObjects.ActorRole.FromName(actor.ActorRole.Name),
            BusinessReason: ProcessManager.Components.Abstractions.ValueObjects.BusinessReason.FromName(BusinessReason.WholesaleFixing.Name),
            Resolution: null,
            PeriodStart: InstantPattern.General.Parse("2023-01-31T23:00:00Z").GetValueOrThrow().ToDateTimeOffset(),
            PeriodEnd: InstantPattern.General.Parse("2023-01-31T23:00:00Z").GetValueOrThrow().ToDateTimeOffset(),
            GridAreas: [gridArea],
            EnergySupplierNumber: null,
            ChargeOwnerNumber: null,
            SettlementVersion: null,
            ChargeTypes: []);

        return CreateServiceBusMessage(accepted, actor);
    }

    public static ServiceBusMessage CreateReject(Actor actor)
    {
        var reject = new RequestCalculatedWholesaleServicesRejectedV1(
            OriginalTransactionId: Guid.NewGuid().ToString(),
            OriginalMessageId: Guid.NewGuid().ToString(),
            RequestedForActorNumber: ProcessManager.Components.Abstractions.ValueObjects.ActorNumber.Create(actor.ActorNumber.Value),
            RequestedForActorRole: ProcessManager.Components.Abstractions.ValueObjects.ActorRole.FromName(actor.ActorRole.Name),
            RequestedByActorNumber: ProcessManager.Components.Abstractions.ValueObjects.ActorNumber.Create(actor.ActorNumber.Value),
            RequestedByActorRole: ProcessManager.Components.Abstractions.ValueObjects.ActorRole.FromName(actor.ActorRole.Name),
            BusinessReason: ProcessManager.Components.Abstractions.ValueObjects.BusinessReason.FromName(BusinessReason.BalanceFixing.Name),
            ValidationErrors: new List<ValidationErrorDto>()
            {
                new ValidationErrorDto(Message: "Test Rejection", ErrorCode: "888"),
            });

        return CreateServiceBusMessage(reject, actor);
    }

    private static ServiceBusMessage CreateServiceBusMessage<TData>(
        TData data,
        Actor actor)
        where TData : class
    {
        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = _orchestrationName,
            OrchestrationVersion = 1,
            OrchestrationStartedByActorId = actor.ActorNumber.Value,
            OrchestrationInstanceId = Guid.NewGuid().ToString(),
        };

        enqueueActorMessages.SetData(data);

        var message = new ServiceBusMessage(JsonFormatter.Default.Format(enqueueActorMessages))
        {
            Subject = EnqueueActorMessagesV1.BuildServiceBusMessageSubject(_orchestrationName),
            MessageId = Guid.NewGuid().ToString(),
            ContentType = "application/json",
        };
        message.ApplicationProperties.Add("MajorVersion", enqueueActorMessages.GetType().Name);
        message.ApplicationProperties.Add("BodyFormat", "Json");

        return message;
    }
}
