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
using System.Linq;
using Messaging.CimMessageAdapter.Messages;
using Messaging.Domain.SeedWork;

namespace Messaging.Infrastructure.IncomingMessages;

public static class CimFormatParser
{
    public static CimFormat? ParseFromContentTypeHeaderValue(string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var contentType = ParseContentTypeName(value);

        return EnumerationType.GetAll
                <CimFormat>()
            .FirstOrDefault(v => v.Name.Equals(contentType, StringComparison.OrdinalIgnoreCase));
    }

    private static string ParseContentTypeName(string value)
    {
        var contentTypeValues = value.Split(";");
        var contentTypeValue = contentTypeValues[0].Trim();
        var contentType = contentTypeValue.Substring(contentTypeValue.IndexOf("/", StringComparison.OrdinalIgnoreCase) + 1);
        return contentType;
    }
}
