﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM012;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM012;

namespace Energinet.DataHub.EDI.Tests.Factories;

public class MeteredDataForMeteringPointBuilder
{
    public OutgoingMessageHeader BuildHeader()
    {
        return new OutgoingMessageHeader(
            SampleData.BusinessReason.Name,
            SampleData.SenderActorNumber,
            SampleData.SenderActorRole,
            SampleData.ReceiverActorNumber,
            SampleData.ReceiverActorRole,
            SampleData.MessageId,
            null,
            SampleData.TimeStamp);
    }

    public MeteredDataForMeteringPointMarketActivityRecord BuildMeteredDataForMeteringPoint(
        TransactionId? transactionId = null,
        IReadOnlyList<PointActivityRecord>? points = null)
    {
        return new MeteredDataForMeteringPointMarketActivityRecord(
            transactionId ?? SampleData.TransactionId,
            SampleData.MeteringPointNumber,
            SampleData.MeteringPointType,
            SampleData.OriginalTransactionIdReferenceId,
            SampleData.Product,
            SampleData.QuantityMeasureUnit,
            SampleData.RegistrationDateTime,
            SampleData.Resolution,
            SampleData.StartedDateTime,
            SampleData.EndedDateTime,
            points ?? SampleData.Points);
    }

    public MeteredDataForMeteringPointMarketActivityRecord BuildMinimalMeteredDataForMeteringPoint() =>
        new(
            SampleData.TransactionId,
            SampleData.MeteringPointNumber,
            SampleData.MeteringPointType,
            null,
            null,
            SampleData.QuantityMeasureUnit,
            SampleData.RegistrationDateTime,
            SampleData.Resolution,
            SampleData.StartedDateTime,
            SampleData.EndedDateTime,
            SampleData.MinimalPoints);
}
