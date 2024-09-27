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

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

public class DocumentFormat : EnumerationType
{
    public static readonly DocumentFormat Xml = new(nameof(Xml));
    public static readonly DocumentFormat Json = new(nameof(Json));
    public static readonly DocumentFormat Ebix = new(nameof(Ebix));

    private DocumentFormat(string name)
        : base(name)
    {
    }

    public static DocumentFormat FromName(string name)
    {
        return GetAll<DocumentFormat>().FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException($"{name} is not a valid {typeof(DocumentFormat)} {nameof(name)}");
    }

    public string GetContentType()
    {
        if (Name.Equals(Xml.Name, StringComparison.OrdinalIgnoreCase))
        {
            return "application/xml";
        }

        if (Name.Equals(Json.Name, StringComparison.OrdinalIgnoreCase))
        {
            return "application/json";
        }

        if (Name.Equals(Ebix.Name, StringComparison.OrdinalIgnoreCase))
        {
            return "application/xml";
        }

        return "text/plain";
    }
}
