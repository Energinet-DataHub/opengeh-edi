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
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using B2B.Transactions.Xml.SchemaStore;

namespace B2B.Transactions.Xml.Outgoing
{
    public class MessageValidator
    {
        private readonly ISchemaProvider _schemaProvider;
        private readonly List<string> _errors = new();

        public MessageValidator(ISchemaProvider schemaProvider)
        {
            _schemaProvider = schemaProvider ?? throw new ArgumentNullException(nameof(schemaProvider));
        }

        public bool Success => _errors.Count == 0;

        public string Errors() => string.Join(",", _errors);

        public async Task ParseAsync(string message, string businessProcessType, string version)
        {
            var xmlSchema = await _schemaProvider.GetSchemaAsync(businessProcessType, version).ConfigureAwait(true);
            if (xmlSchema is null)
            {
                _errors.Add(
                    $"{businessProcessType} version {version} could not be found in internal schema store");
                return;
            }

            var messageStream = CreateStreamFromString(message);
            using (var reader = XmlReader.Create(messageStream, CreateXmlReaderSettings(xmlSchema)))
            {
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                }
            }

            await messageStream.DisposeAsync().ConfigureAwait(false);
        }

        private static Stream CreateStreamFromString(string input)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(input));
        }

        private void OnValidationError(object? sender, ValidationEventArgs arguments)
        {
            _errors.Add($"XML schema validation error at line {arguments.Exception.LineNumber}, position {arguments.Exception.LinePosition}: {arguments.Message}");
        }

        private XmlReaderSettings CreateXmlReaderSettings(XmlSchema xmlSchema)
        {
            var settings = new XmlReaderSettings
            {
                Async = true,
                ValidationType = ValidationType.Schema,
                ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema |
                                  XmlSchemaValidationFlags.ReportValidationWarnings,
            };

            settings.Schemas.Add(xmlSchema);
            settings.ValidationEventHandler += OnValidationError;
            return settings;
        }
    }
}
