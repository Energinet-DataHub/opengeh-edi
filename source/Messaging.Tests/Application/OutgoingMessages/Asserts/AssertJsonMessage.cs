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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Json.Schema;
using Messaging.Application.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Messaging.Application.SchemaStore;
using Messaging.Domain.OutgoingMessages;
using Namotion.Reflection;
using Xunit;

namespace Messaging.Tests.Application.OutgoingMessages.Asserts;

public static class AssertJsonMessage
{
    public static void AssertConformsToSchema(JsonDocument document, JsonSchema schema)
    {
        if (schema == null) throw new InvalidOperationException("Schema not found for business process type ConfirmRequestChangeOfSupplier");

        var validationOptions = new ValidationOptions()
        {
            OutputFormat = OutputFormat.Detailed,
        };

        var validationResult = schema.Validate(document, validationOptions);

        Assert.True(validationResult.IsValid);
    }

    public static void AssertHeader(MessageHeader header, JsonDocument document)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (header == null) throw new ArgumentNullException(nameof(header));
        var root = document.RootElement.GetProperty("ConfirmRequestChangeOfSupplier_MarketDocument");
        Assert.Equal(header.MessageId, root.GetProperty("mRID").ToString());
        Assert.Equal(header.ProcessType, root.GetProperty("process.processType").GetProperty("value").ToString());
        Assert.Equal("23", root.GetProperty("businessSector.type").GetProperty("value").ToString());
        Assert.Equal(header.SenderId, root.GetProperty("sender_MarketParticipant.mRID").GetProperty("value").ToString());
        Assert.Equal(header.SenderRole, root.GetProperty("sender_MarketParticipant.marketRole.type").GetProperty("value").ToString());
        Assert.Equal(header.ReceiverId, root.GetProperty("receiver_MarketParticipant.mRID").GetProperty("value").ToString());
        Assert.Equal(header.ReceiverRole, root.GetProperty("receiver_MarketParticipant.marketRole.type").GetProperty("value").ToString());
        Assert.Equal(header.ReasonCode, root.GetProperty("reason.code").GetProperty("value").ToString());
    }
}
