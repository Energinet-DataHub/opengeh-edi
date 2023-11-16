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

using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Infrastructure.DocumentValidation;
using Energinet.DataHub.Edi.Requests;
using IncomingMessages.Infrastructure.BaseParsers;
using IncomingMessages.Infrastructure.Messages;
using IncomingMessages.Infrastructure.Messages.RequestAggregatedMeasureData;
using NodaTime;

namespace IncomingMessages.Infrastructure.RequestAggregatedMeasureDataParsers;

public class ProtoMessageParser : IMessageParser
{
    public ProtoMessageParser()
        : base()
    {
    }

    public DocumentFormat HandledFormat => DocumentFormat.Proto;

    public Task<RequestAggregatedMeasureDataMarketMessageParserResult> ParseAsync(
        Stream message,
        CancellationToken cancellationToken)
    {
        var requestAggregatedMeasureData = RequestAggregatedMeasureData.Parser.ParseFrom(message);
        var marketMessage = RequestAggregatedMeasureDataMarketMessageFactory.Create(
            requestAggregatedMeasureData,
            SystemClock.Instance.GetCurrentInstant());
        var mes = new RequestAggregatedMeasureDataMarketMessageParserResult(marketMessage);
        return Task.FromResult(mes);
    }
}
