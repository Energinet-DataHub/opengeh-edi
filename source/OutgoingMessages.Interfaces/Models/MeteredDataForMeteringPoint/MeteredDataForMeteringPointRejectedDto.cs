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

using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;

public sealed record MeteredDataForMeteringPointRejectedDto(
    string EventId,
    string BusinessReason,
    string ReceiverId,
    string ReceiverRole,
    Guid ProcessId,
    Guid ExternalId,
    AcknowledgementDto AcknowledgementDto);

public sealed record AcknowledgementDto(
    DateTimeOffset? ReceivedMarketDocumentCreatedDateTime,
    string? ReceivedMarketDocumentTransactionId,
    string? ReceivedMarketDocumentProcessProcessType,
    string? ReceivedMarketDocumentRevisionNumber,
    string? ReceivedMarketDocumentTitle,
    string? ReceivedMarketDocumentType,
    IReadOnlyCollection<ReasonDto> Reason,
    IReadOnlyCollection<TimePeriodDto> InErrorPeriod,
    IReadOnlyCollection<SeriesDto> Series,
    IReadOnlyCollection<MktActivityRecordDto> OriginalMktActivityRecord,
    IReadOnlyCollection<TimeSeriesDto> RejectedTimeSeries);

public sealed record ReasonDto(string Code, string? Text);

public sealed record TimePeriodDto(Interval TimeInterval, IReadOnlyCollection<ReasonDto> Reason);

public sealed record SeriesDto(string MRID, IReadOnlyCollection<ReasonDto> Reason);

public sealed record MktActivityRecordDto(string MRID, IReadOnlyCollection<ReasonDto> Reason);

public sealed record TimeSeriesDto(
    string MRID,
    string Version,
    IReadOnlyCollection<TimePeriodDto> InErrorPeriod,
    IReadOnlyCollection<ReasonDto> Reason);
