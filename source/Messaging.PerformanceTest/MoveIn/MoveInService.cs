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
        var actorIdElement = xmlDocument.Root?.Elements()
            .Single(x => x.Name.LocalName.Equals("sender_MarketParticipant.mRID", StringComparison.OrdinalIgnoreCase));
        if (actorIdElement is not null) actorIdElement.Value = uniqueActorNumber;
        var messageIdElement = xmlDocument.Root?.Elements()
            .Single(x => x.Name.LocalName.Equals("mRID", StringComparison.OrdinalIgnoreCase));
        if (messageIdElement is not null) messageIdElement.Value = RandomNumberGenerator.GetInt32(10000000).ToString(CultureInfo.InvariantCulture);
        var transactionIdElement = xmlDocument.Root?.Elements()
            .Single(x => x.Name.LocalName.Equals("MktActivityRecord", StringComparison.OrdinalIgnoreCase)).Elements().Single(x => x.Name.LocalName.Equals("mRID", StringComparison.OrdinalIgnoreCase));
        if (transactionIdElement is not null) transactionIdElement.Value = RandomNumberGenerator.GetInt32(10000000).ToString(CultureInfo.InvariantCulture);

        var builder = new StringBuilder();
        using (TextWriter writer = new Utf8StringWriter(builder))
        {
            xmlDocument.Save(writer);
        }

        return builder.ToString();
    }
}

public class Utf8StringWriter : StringWriter
{
    public Utf8StringWriter(StringBuilder builder)
        : base(builder, CultureInfo.InvariantCulture)
    {
    }

    // Use UTF8 encoding but write no BOM to the wire
    public override Encoding Encoding
    {
        get { return new UTF8Encoding(false); } // in real code I'll cache this encoding.
    }
}
