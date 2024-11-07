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

using System.Collections.ObjectModel;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.MeteredDateForMeasurementPointParsers;

public partial class MeteredDataForMeasurementPointMessageParser : IMessageParser
{
    private Collection<ValidationError> Errors { get; } = [];

    public Task<IncomingMarketMessageParserResult> ParseXmlAsync(IIncomingMarketMessageStream incomingMarketMessageStream, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<IncomingMarketMessageParserResult> ParseJsonAsync(IIncomingMarketMessageStream incomingMarketMessageStream, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }
}
