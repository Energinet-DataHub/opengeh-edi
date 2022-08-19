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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using Json.Schema;
using Messaging.Application.Common;
using Messaging.Application.Configuration;
using Messaging.Application.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Messaging.Application.SchemaStore;
using Messaging.Domain.OutgoingMessages;
using Messaging.Infrastructure.Common;
using Messaging.Infrastructure.Configuration;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.Tests.Application.OutgoingMessages.Asserts;
using Xunit;

namespace Messaging.Tests.Application.OutgoingMessages.ConfirmRequestChangeOfSupplier;

public class ConfirmRequestChangeOfSupplierJsonDocumentWriterTests
{
    private readonly ConfirmChangeOfSupplierJsonDocumentWriter _documentWriter;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IMarketActivityRecordParser _marketActivityRecordParser;
    private JsonSchemaProvider _schemaProvider;

    public ConfirmRequestChangeOfSupplierJsonDocumentWriterTests()
    {
        _systemDateTimeProvider = new SystemDateTimeProvider();
        _marketActivityRecordParser = new MarketActivityRecordParser(new Serializer());
        _documentWriter = new ConfirmChangeOfSupplierJsonDocumentWriter("ConfirmRequestChangeOfSupplier_MarketDocument", "E44", _marketActivityRecordParser);
        _schemaProvider = new JsonSchemaProvider();
    }

    [Fact]
    public async Task Document_is_valid()
    {
        var header = new MessageHeader("E03", "SenderId", "DDZ", "ReceiverId", "DDQ", Guid.NewGuid().ToString(), _systemDateTimeProvider.Now(), "A01");
        var marketActivityRecords = new List<MarketActivityRecord>()
        {
            new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "FakeMarketEvaluationPointId"),
        };

        var message = await _documentWriter.WriteAsync(header, marketActivityRecords.Select(record => _marketActivityRecordParser.From(record)).ToList()).ConfigureAwait(false);

        await AssertConformsToSchema(message).ConfigureAwait(false);
    }

    private async Task AssertConformsToSchema(Stream message)
    {
        _schemaProvider = new JsonSchemaProvider();
        var schema = await _schemaProvider.GetSchemaAsync<JsonSchema>("confirmrequestchangeofsupplier", "0").ConfigureAwait(false);
        if (schema == null) throw new InvalidOperationException("Schema not found for business process type ConfirmRequestChangeOfSupplier");

        var jsonDocument = await JsonDocument.ParseAsync(message).ConfigureAwait(false);

        var validationOptions = new ValidationOptions()
        {
            OutputFormat = OutputFormat.Detailed,
        };

        var validationResult = schema.Validate(jsonDocument, validationOptions);

        Assert.True(validationResult.IsValid);
    }
}
