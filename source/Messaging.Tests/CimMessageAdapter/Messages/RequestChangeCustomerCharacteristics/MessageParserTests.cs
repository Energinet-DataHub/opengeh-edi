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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Messaging.Application.IncomingMessages.RequestChangeCustomerCharacteristics;
using Messaging.CimMessageAdapter.Messages;
using Messaging.CimMessageAdapter.Messages.RequestChangeCustomerCharacteristics;
using Messaging.Domain.OutgoingMessages;
using Messaging.Infrastructure.IncomingMessages.RequestChangeCustomerCharacteristics;
using Xunit;
using MessageHeader = Messaging.Application.IncomingMessages.MessageHeader;

namespace Messaging.Tests.CimMessageAdapter.Messages.RequestChangeCustomerCharacteristics;

public class MessageParserTests
{
    private readonly MessageParser _messageParser;

    public MessageParserTests()
    {
        _messageParser = new MessageParser(
            new IMessageParser<MarketActivityRecord, RequestChangeCustomerCharacteristicsTransaction>[]
            {
                new XmlMessageParser(),
            });
    }

    public static IEnumerable<object[]> CreateMessages()
    {
        return new List<object[]>
        {
            new object[] { CimFormat.Xml, CreateXmlMessage() },
        };
    }

    [Theory]
    [MemberData(nameof(CreateMessages))]
    public async Task Can_parse(CimFormat format, Stream message)
    {
        var result = await _messageParser.ParseAsync(message, format).ConfigureAwait(false);

        Assert.True(result.Success);
        AssertHeader(result.IncomingMarketDocument?.Header);
        AssertMarketActivityRecord(result.IncomingMarketDocument?.MarketActivityRecords.First());
    }

    private static Stream CreateXmlMessage()
    {
        var xmlDoc = XDocument.Load($"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}xml{Path.DirectorySeparatorChar}RequestChangeCustomerCharacteristics.xml");
        var stream = new MemoryStream();
        xmlDoc.Save(stream);

        return stream;
    }

    private static void AssertHeader(MessageHeader? header)
    {
        Assert.Equal("253659974", header?.MessageId);
        Assert.Equal("E34", header?.ProcessType);
        Assert.Equal("5799999933318", header?.SenderId);
        Assert.Equal("DDQ", header?.SenderRole);
        Assert.Equal("5790001330552", header?.ReceiverId);
        Assert.Equal("DDZ", header?.ReceiverRole);
        Assert.Equal("2022-12-17T09:30:47Z", header?.CreatedAt);
    }

    private static void AssertMarketActivityRecord(MarketActivityRecord? marketActivityRecord)
    {
        Assert.Equal("253698254", marketActivityRecord?.Id);
        Assert.Equal("2022-12-17T23:00:00Z", marketActivityRecord?.EffectiveDate);
        AssertMarketEvaluationPoint(marketActivityRecord?.MarketEvaluationPoint!);
    }

    private static void AssertMarketEvaluationPoint(MarketEvaluationPoint marketEvaluationPoint)
    {
        Assert.Equal("579999993331812345", marketEvaluationPoint.GsrnNumber);
        Assert.True(marketEvaluationPoint.ElectricalHeating);
        Assert.Equal("0212756369", marketEvaluationPoint.FirstCustomer.Id);
        Assert.Equal("Jan Hansen", marketEvaluationPoint.FirstCustomer.Name);
        Assert.Equal("0403751478", marketEvaluationPoint.SecondCustomer.Id);
        Assert.Equal("Gry Hansen", marketEvaluationPoint.SecondCustomer.Name);
        Assert.False(marketEvaluationPoint.ProtectedName);
        AssertPointLocation(marketEvaluationPoint.PointLocations[0], "D01");
        AssertPointLocation(marketEvaluationPoint.PointLocations[1], "D04");
    }

    private static void AssertPointLocation(PointLocation pointLocation, string type)
    {
        Assert.Equal(type, pointLocation.Type);
        Assert.Equal("f26f8678-6cd3-4e12-b70e-cf96290ada94", pointLocation.GeoInfoReference);
        AssertAddress(pointLocation.Address);
        Assert.False(pointLocation.ProtectedAddress);
        Assert.Equal("Jytte Larsen", pointLocation.Name);
        Assert.Equal("Hans Sørensen", pointLocation.AttnName);
    }

    private static void AssertAddress(Address address)
    {
        Assert.Equal("0403", address.StreetDetails.StreetCode);
        Assert.Equal("Vestergade", address.StreetDetails.StreetName);
        Assert.Equal("16", address.StreetDetails.BuildingNumber);
        Assert.Equal("2", address.StreetDetails.FloorIdentification);
        Assert.Equal("10", address.StreetDetails.RoomIdentification);
        Assert.Equal("0506", address.TownDetails.MunicipalityCode);
        Assert.Equal("Middelfart", address.TownDetails.CityName);
        Assert.Equal("Strib", address.TownDetails.CitySubDivisionName);
        Assert.Equal("DK", address.TownDetails.CountryCode);
        Assert.Equal("5500", address.PostalCode);
        Assert.Equal("Kassen", address.PoBox);
    }
}
