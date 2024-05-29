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

using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.SystemTests.Logging;

public static class HttpResponseMessageExtensions
{
    public static async Task EnsureSuccessStatusCodeWithLogAsync(this HttpResponseMessage response, ITestOutputHelper logger)
    {
        if (!response.IsSuccessStatusCode)
        {
            logger.WriteLine("Error response status code: {0}", (int)response.StatusCode);
            logger.WriteLine("Error response reason phrase: {0}", response.ReasonPhrase);

            var content = await response.Content.ReadAsStringAsync();
            logger.WriteLine($"Error response content:{Environment.NewLine}{{0}}", content);
        }

        response.EnsureSuccessStatusCode();
    }
}
