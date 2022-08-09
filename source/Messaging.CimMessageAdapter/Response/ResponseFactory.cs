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
using Messaging.CimMessageAdapter.Messages;

namespace Messaging.CimMessageAdapter.Response;

public class ResponseFactory
{
    private readonly Dictionary<CimFormat, IResponseFactory> _factories = new()
    {
        { CimFormat.Xml, new XmlResponseFactory() },
        { CimFormat.Json, new JsonResponseFactory() },
    };

    public ResponseMessage From(Result result, CimFormat format)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        var factory = _factories.GetValueOrDefault(format);
        return factory!.From(result);
    }
}
