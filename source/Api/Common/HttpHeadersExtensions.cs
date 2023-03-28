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
using System.Net.Http.Headers;

namespace Api.Common;

internal static class HttpHeadersExtensions
{
    internal static string GetContentType(this HttpHeaders headers)
    {
        var contentHeader = headers.GetValues("Content-Type").FirstOrDefault();
        if (contentHeader == null) throw new InvalidOperationException("No Content-Type found in request headers");
        return contentHeader;
    }
}
