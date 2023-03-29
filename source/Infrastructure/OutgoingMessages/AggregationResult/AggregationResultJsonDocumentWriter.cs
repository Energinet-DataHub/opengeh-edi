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
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DocumentValidation;
using Domain.OutgoingMessages;
using Infrastructure.OutgoingMessages.Common.Json;
using DocumentFormat = Domain.OutgoingMessages.DocumentFormat;

namespace Infrastructure.OutgoingMessages.AggregationResult;

public class AggregationResultJsonDocumentWriter : IMessageWriter
{
    private const string DocumentType = "NotifyAggregatedMeasureData_MarketDocument";
    private const string TypeCode = "blabla";

    public bool HandlesFormat(DocumentFormat format)
    {
        return format == DocumentFormat.Json;
    }

    public bool HandlesType(MessageType messageType)
    {
        return messageType == MessageType.NotifyAggregatedMeasureData;
    }

    public async Task<Stream> WriteAsync(MessageHeader header, IReadOnlyCollection<string> marketActivityRecords)
    {
        var stream = new MemoryStream();
        var options = new JsonWriterOptions() { Indented = true };
        using var writer = new Utf8JsonWriter(stream, options);

        JsonHeaderWriter.Write(header, DocumentType, TypeCode, "A02", writer);
        //WriteMarketActitivyRecords(marketActivityRecords, writer);
        //writer.WriteEndObject();
        await writer.FlushAsync().ConfigureAwait(false);
        stream.Position = 0;
        return stream;
    }
}
