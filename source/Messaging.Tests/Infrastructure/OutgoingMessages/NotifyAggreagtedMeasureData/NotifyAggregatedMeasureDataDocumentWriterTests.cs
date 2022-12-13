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
using System.Threading.Tasks;
using System.Xml.Schema;
using Messaging.Application.SchemaStore;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Infrastructure.Common;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.Infrastructure.OutgoingMessages.NotifyAggregatedMeasureData;
using Messaging.Tests.Infrastructure.OutgoingMessages.Asserts;
using NodaTime;
using Xunit;

namespace Messaging.Tests.Infrastructure.OutgoingMessages.NotifyAggreagtedMeasureData;

public class NotifyAggregatedMeasureDataDocumentWriterTests
{
    private const string NamespacePrefix = "cim";
    private readonly IDocumentWriter _documentWriter;
    private ISchemaProvider _schemaProvider;

    public NotifyAggregatedMeasureDataDocumentWriterTests()
    {
        _schemaProvider = new XmlSchemaProvider();
        _documentWriter = new NotifyAggregatedMeasureDataDocumentWriter(new MarketActivityRecordParser(new Serializer()));
    }

    [Fact]
    public async Task Can_create_document()
    {
        var header = new MessageHeader(
            ProcessType.BalanceFixing.Code,
            "1234567890123",
            MarketRole.MeteringPointAdministrator.Name,
            "1234567890321",
            MarketRole.GridOperator.Name,
            Guid.NewGuid().ToString(),
            SystemClock.Instance.GetCurrentInstant());

        var message = await _documentWriter.WriteAsync(header, new List<string>()).ConfigureAwait(false);

        AssertXmlDocument
            .Document(message, NamespacePrefix)
            .HasValue("type", "E31");
    }

    private Task<XmlSchema?> GetSchema()
    {
        _schemaProvider = new XmlSchemaProvider();
        return _schemaProvider.GetSchemaAsync<XmlSchema>("notifyaggregatedmeasuredata", "0.1");
    }
}
