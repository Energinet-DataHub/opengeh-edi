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

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Messaging.PerformanceTest.Controllers;

namespace Messaging.PerformanceTest.MoveIn;

internal class MoveInService : IMoveInService
{
    public async Task MoveInAsync(string? uniqueActorNumber)
    {
        ArgumentNullException.ThrowIfNull(uniqueActorNumber);
        var moveInPayload = GetMoveInPayload(uniqueActorNumber);
        var jwt = JwtBuilder.BuildToken(uniqueActorNumber);

        using StringContent body = new(
            JsonSerializer.Serialize(moveInPayload),
            Encoding.UTF8,
            "application/xml");

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        await httpClient.PostAsync(new Uri($"http://localhost:7071/api/RequestChangeOfSupplier"), body).ConfigureAwait(false);
    }

    private static string GetMoveInPayload(string uniqueActorNumber)
    {
        var xmlDocument = XDocument.Load($"MoveIn{Path.DirectorySeparatorChar}xml{Path.DirectorySeparatorChar}RequestChangeOfSupplier.xml");
        // xmlDocument.DescendantNodes().OfType<XComment>().Remove();
        var actorIdElement = xmlDocument.Root?.Elements()
            .Single(x => x.Name.LocalName.Equals("receiver_MarketParticipant.mRID", StringComparison.OrdinalIgnoreCase));
        if (actorIdElement is not null) actorIdElement.Value = uniqueActorNumber;
        return $"{xmlDocument.Declaration}{xmlDocument}";
    }
}
