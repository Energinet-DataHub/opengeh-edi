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
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Messaging.PerformanceTest.MoveIn.Jwt;

namespace Messaging.PerformanceTest.MoveIn;

internal class MoveInService : IMoveInService
{
    private readonly string _hostname;
    private readonly string _hostport;

    public MoveInService(IConfiguration configuration)
    {
        _hostname = configuration["MessagingApi:Hostname"];
        _hostport = configuration["MessagingApi:Port"];
    }

    public async Task MoveInAsync(string? uniqueActorNumber)
    {
        ArgumentNullException.ThrowIfNull(uniqueActorNumber);
        var moveInPayload = GetMoveInPayload(uniqueActorNumber);
        var jwt = JwtBuilder.BuildToken(uniqueActorNumber);

        using StringContent body = new(
            moveInPayload,
            Encoding.UTF8,
            "application/xml");

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        await httpClient.PostAsync(new Uri($"http://{_hostname}:{_hostport}/api/RequestChangeOfSupplier"), body).ConfigureAwait(false);
    }

    private static string GetMoveInPayload(string uniqueActorNumber)
    {
        var xmlDocument = XDocument.Load($"MoveIn{Path.DirectorySeparatorChar}xml{Path.DirectorySeparatorChar}RequestChangeOfSupplier.xml");
        xmlDocument.DescendantNodes().OfType<XComment>().Remove();
        ReplaceElementValue(xmlDocument, "sender_MarketParticipant.mRID", uniqueActorNumber);
        ReplaceElementValue(xmlDocument, "mRID", RandomNumberGenerator.GetInt32(10000000).ToString(CultureInfo.InvariantCulture));
        ReplaceActivityRecordElementValue(xmlDocument, "mRID", RandomNumberGenerator.GetInt32(10000000).ToString(CultureInfo.InvariantCulture));

        var builder = new StringBuilder();
        using (TextWriter writer = new Utf8StringWriter(builder))
        {
            xmlDocument.Save(writer);
        }

        return builder.ToString();
    }

    private static void ReplaceElementValue(XDocument xmlDocument, string elementName, string value)
    {
        var element = xmlDocument.Root?.Elements()
            .Single(x => x.Name.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase));
        if (element is not null) element.Value = value;
    }

    private static void ReplaceActivityRecordElementValue(XDocument xmlDocument, string elementName, string value)
    {
        var element = xmlDocument.Root?.Elements()
            .Single(x => x.Name.LocalName.Equals("MktActivityRecord", StringComparison.OrdinalIgnoreCase))
            .Elements().Single(x => x.Name.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase));
        if (element is not null) element.Value = value;
    }
}
