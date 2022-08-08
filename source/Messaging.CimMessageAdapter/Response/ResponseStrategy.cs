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
using Messaging.CimMessageAdapter.Messages;

namespace Messaging.CimMessageAdapter.Response;

public static class ResponseStrategy
{
    private static readonly IDictionary<string, Func<ResponseFactory>> _strategies = new Dictionary<string, Func<ResponseFactory>>()
    {
        { CimFormat.Xml.Name, () => new XmlResponseFactory() },
        { CimFormat.Json.Name, () => new JsonResponseFactory() },
    };

    public static ResponseFactory GetResponseFactory(string cimFormat)
    {
        var strategy = _strategies.FirstOrDefault(s => string.Equals(s.Key, cimFormat, StringComparison.OrdinalIgnoreCase));
        if (strategy.Key is null) throw new InvalidOperationException($"No response strategy found for CIM format {cimFormat}");
        return strategy.Value();
    }
}
