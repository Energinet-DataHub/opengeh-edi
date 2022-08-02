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

namespace Messaging.Application.Common;

public class DocumentDetails
{
    public DocumentDetails(string type, string schemaLocation, string xmlNamespace, string prefix, string typeCode)
    {
        Type = type;
        SchemaLocation = schemaLocation;
        XmlNamespace = xmlNamespace;
        Prefix = prefix;
        TypeCode = typeCode;
    }

    public string Type { get; }

    public string SchemaLocation { get; }

    public string XmlNamespace { get; }

    public string Prefix { get; }

    public string TypeCode { get; }
}
