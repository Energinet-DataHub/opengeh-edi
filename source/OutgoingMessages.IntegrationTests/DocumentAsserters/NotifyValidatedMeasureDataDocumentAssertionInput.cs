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
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM012;

namespace Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.DocumentAsserters;

public readonly record struct RequiredDocumentFields(
    string BusinessReasonCode,
    string ReceiverId,
    string ReceiverScheme,
    string SenderId,
    string SenderScheme,
    string SenderRole,
    string ReceiverRole,
    string Timestamp);

public class NotifyValidatedMeasureDataDocumentAssertionInput
{
    public NotifyValidatedMeasureDataDocumentAssertionInput(
        RequiredDocumentFields requiredDocumentFields,
        RequiredSeriesFields? requiredSeriesFields)
    {
        RequiredDocumentFields = requiredDocumentFields;
        RequiredSeriesFields = requiredSeriesFields;
    }

    public RequiredDocumentFields RequiredDocumentFields { get; }

    public RequiredSeriesFields? RequiredSeriesFields { get; }
}

public sealed record RequiredSeriesFields(
    TransactionId TransactionId,
    string MeteringPointNumber,
    string MeteringPointScheme,
    string MeteringPointType,
    string QuantityMeasureUnit,
    string Resolution,
    string StartedDateTime,
    string EndedDateTime,
    IReadOnlyList<(RequiredPointDocumentFields, OptionalPointDocumentFields?)> Points);
