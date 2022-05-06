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
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace Messaging.Application.Xml
{
    public static class MessageValidator
    {
        public static async Task<ValidationResult> ValidateAsync(Stream message, XmlSchema schema)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (schema == null) throw new ArgumentNullException(nameof(schema));

            var validationErrors = new List<string>();
            var settings = CreateXmlReaderSettings(schema);
            settings.ValidationEventHandler += (sender, arguments) =>
            {
                validationErrors.Add($"{arguments.Exception.LineNumber}, position {arguments.Exception.LinePosition}: {arguments.Message}");
            };

            using (var reader = XmlReader.Create(message, settings))
            {
                MoveToStartPosition(message);
                await ReadEntireMessageAsync(reader).ConfigureAwait(false);
                MoveToStartPosition(message);
            }

            return validationErrors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(validationErrors);
        }

        private static async Task ReadEntireMessageAsync(XmlReader reader)
        {
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
            }
        }

        private static void MoveToStartPosition(Stream message)
        {
            message.Position = 0;
        }

        private static XmlReaderSettings CreateXmlReaderSettings(XmlSchema xmlSchema)
        {
            var settings = new XmlReaderSettings
            {
                Async = true,
                ValidationType = ValidationType.Schema,
                ValidationFlags = XmlSchemaValidationFlags.ProcessInlineSchema |
                                  XmlSchemaValidationFlags.ReportValidationWarnings,
            };

            settings.Schemas.Add(xmlSchema);
            return settings;
        }
    }
}
