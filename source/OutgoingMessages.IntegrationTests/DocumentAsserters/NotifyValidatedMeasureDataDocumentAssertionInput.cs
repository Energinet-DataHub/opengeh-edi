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

public readonly record struct RequiredHeaderDocumentFields(
    string BusinessReasonCode,
    string ReceiverId,
    string ReceiverScheme,
    string SenderId,
    string SenderScheme,
    string SenderRole,
    string ReceiverRole,
    string Timestamp);

public sealed record OptionalHeaderDocumentFields(
    string? BusinessSectorType,
    AssertSeriesDocumentFieldsInput? AssertSeriesDocumentFieldsInput);

public sealed record NotifyValidatedMeasureDataDocumentAssertionInput(
    RequiredHeaderDocumentFields RequiredHeaderDocumentFields,
    OptionalHeaderDocumentFields OptionalHeaderDocumentFields);

public sealed record AssertSeriesDocumentFieldsInput(
    RequiredSeriesFields RequiredSeriesFields,
    OptionalSeriesFields OptionalSeriesFields);

public sealed record RequiredSeriesFields(
    TransactionId TransactionId,
    string MeteringPointNumber,
    string MeteringPointScheme,
    string MeteringPointType,
    string QuantityMeasureUnit,
    RequiredPeriodDocumentFields RequiredPeriodDocumentFields);

public sealed record OptionalSeriesFields(
    string? OriginalTransactionIdReferenceId,
    string? RegistrationDateTime,
    string? InDomain,
    string? OutDomain,
    string? Product);

public sealed record RequiredPeriodDocumentFields(
    string Resolution,
    string StartedDateTime,
    string EndedDateTime,
    IReadOnlyCollection<AssertPointDocumentFieldsInput> Points);
