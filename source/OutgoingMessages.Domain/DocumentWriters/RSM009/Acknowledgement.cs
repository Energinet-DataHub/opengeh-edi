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

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM009;

public sealed record Acknowledgement(
    DateTimeOffset? ReceivedMarketDocumentCreatedDateTime,
    string? ReceivedMarketDocumentTransactionId,
    string? ReceivedMarketDocumentProcessProcessType,
    string? ReceivedMarketDocumentRevisionNumber,
    string? ReceivedMarketDocumentTitle,
    string? ReceivedMarketDocumentType,
    IReadOnlyCollection<Reason> Reason,
    IReadOnlyCollection<TimePeriod> InErrorPeriod,
    IReadOnlyCollection<Series> Series,
    IReadOnlyCollection<MktActivityRecord> OriginalMktActivityRecord,
    IReadOnlyCollection<TimeSeries> RejectedTimeSeries);

public sealed record Reason(string Code, string? Text);

public sealed record TimePeriod(Interval TimeInterval, IReadOnlyCollection<Reason> Reason);

public sealed record Series(string MRID, IReadOnlyCollection<Reason> Reason);

public sealed record MktActivityRecord(string MRID, IReadOnlyCollection<Reason> Reason);

public sealed record TimeSeries(
    string MRID,
    string Version,
    IReadOnlyCollection<TimePeriod> InErrorPeriod,
    IReadOnlyCollection<Reason> Reason);
