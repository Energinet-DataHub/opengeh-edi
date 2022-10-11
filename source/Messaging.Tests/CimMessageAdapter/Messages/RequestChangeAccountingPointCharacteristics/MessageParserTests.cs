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
using Messaging.Application.IncomingMessages.RequestChangeAccountPointCharacteristics;
using Messaging.CimMessageAdapter.Messages;
using Messaging.CimMessageAdapter.Messages.RequestChangeAccountingPointCharacteristics;
using Messaging.Domain.OutgoingMessages;
using Messaging.Infrastructure.IncomingMessages.RequestChangeAccountingPointCharacteristics;
using Xunit;
using MarketActivityRecord = Messaging.Application.IncomingMessages.RequestChangeAccountPointCharacteristics.MarketActivityRecord;
using MessageHeader = Messaging.Application.IncomingMessages.MessageHeader;

namespace Messaging.Tests.CimMessageAdapter.Messages.RequestChangeAccountingPointCharacteristics;

public class MessageParserTests
{
    private readonly MessageParser _messageParser;

    public MessageParserTests()
    {
        _messageParser = new MessageParser(
            new IMessageParser<MarketActivityRecord, RequestChangeAccountingPointCharacteristicsTransaction>[]
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
        var xmlDoc = XDocument.Load($"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}xml{Path.DirectorySeparatorChar}RequestChangeAccountingPointCharacteristics.xml");
        var stream = new MemoryStream();
        xmlDoc.Save(stream);

        return stream;
    }

    private static void AssertMarketActivityRecord(MarketActivityRecord? marketActivityRecord)
    {
        Assert.Equal("25361487", marketActivityRecord?.Id);
        Assert.Equal("2022-12-17T23:00:00Z", marketActivityRecord?.EffectiveDate);
        AssertMarketEvaluationPoint(marketActivityRecord?.MarketEvaluationPoint!);
        AssertAddress(marketActivityRecord?.MarketEvaluationPoint.Address!);
    }

    private static void AssertMarketEvaluationPoint(MarketEvaluationPoint marketEvaluationPoint)
    {
        Assert.Equal("579999993331812345", marketEvaluationPoint.GsrnNumber);
        Assert.Equal("E17", marketEvaluationPoint.TypeOfMeteringPoint);
        Assert.Equal("E02", marketEvaluationPoint.SettlementMethod);
        Assert.Equal("D01", marketEvaluationPoint.MeteringMethod);
        Assert.Equal("D03", marketEvaluationPoint.PhysicalStatusOfMeteringPoint);
        Assert.Equal("PT1H", marketEvaluationPoint.MeterReadingOccurence);
        Assert.Equal("6", marketEvaluationPoint.NetSettlementGroup);
        Assert.Equal("--12-17", marketEvaluationPoint.ScheduledMeterReadingDate);
        Assert.Equal("244", marketEvaluationPoint.MeteringGridArea);
        Assert.Equal("031", marketEvaluationPoint.InMeteringGridArea);
        Assert.Equal("244", marketEvaluationPoint.OutMeteringGridArea);
        Assert.Equal("579999993331812327", marketEvaluationPoint.PowerPlant);
        Assert.Equal("6000", marketEvaluationPoint.PhysicalConnectionCapacity);
        Assert.Equal("D01", marketEvaluationPoint.ConnectionType);
        Assert.Equal("D01", marketEvaluationPoint.DisconnectionType);
        Assert.Equal("D12", marketEvaluationPoint.AssetType);
        Assert.Equal("false", marketEvaluationPoint.ProductionObligation);
        Assert.Equal("230", marketEvaluationPoint.MaximumPower);
        Assert.Equal("32", marketEvaluationPoint.MaximumCurrent);
        Assert.Equal("2536258974", marketEvaluationPoint.MeterNumber);
        Assert.Equal("8716867000030", marketEvaluationPoint.Series.ProductType);
        Assert.Equal("KWH", marketEvaluationPoint.Series.MeasureUnitType);
        Assert.Equal("3. bygning til venstre", marketEvaluationPoint.LocationDescription);
        Assert.Equal("0a3f50b9-b942-32b8-e044-0003ba298018", marketEvaluationPoint.GeoInfoReference);
    }

    private static void AssertAddress(Address address)
    {
        Assert.Equal("0405", address.StreetCode);
        Assert.Equal("Vestergade", address.StreetName);
        Assert.Equal("10", address.BuildingNumber);
        Assert.Equal("1", address.FloorIdentification);
        Assert.Equal("12", address.RoomIdentification);
    }

    private static void AssertHeader(MessageHeader? header)
    {
        Assert.Equal("253698245", header?.MessageId);
        Assert.Equal("E02", header?.ProcessType);
        Assert.Equal("5799999933318", header?.SenderId);
        Assert.Equal("DDM", header?.SenderRole);
        Assert.Equal("5790001330552", header?.ReceiverId);
        Assert.Equal("DDZ", header?.ReceiverRole);
        Assert.Equal("2022-12-17T09:30:47Z", header?.CreatedAt);
    }
}
