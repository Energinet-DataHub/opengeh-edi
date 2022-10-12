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

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Messaging.Application.IncomingMessages.RequestChangeCustomerCharacteristics;
using Messaging.Application.SchemaStore;
using Messaging.CimMessageAdapter.Errors;
using Messaging.CimMessageAdapter.Messages;
using Messaging.Domain.OutgoingMessages;

namespace Messaging.Infrastructure.IncomingMessages.RequestChangeCustomerCharacteristics;

public class XmlMessageParser : IMessageParser<MarketActivityRecord, RequestChangeCustomerCharacteristicsTransaction>
{
    private const string MarketActivityRecordElementName = "MktActivityRecord";
    private const string HeaderElementName = "RequestChangeOfSupplier_MarketDocument";
    private readonly List<ValidationError> _errors = new();
    private readonly ISchemaProvider _schemaProvider;

    public XmlMessageParser()
    {
        _schemaProvider = new XmlSchemaProvider();
    }

    public CimFormat HandledFormat => CimFormat.Xml;

    public Task<MessageParserResult<MarketActivityRecord, RequestChangeCustomerCharacteristicsTransaction>> ParseAsync(Stream message)
    {
        throw new System.NotImplementedException();
    }
}
