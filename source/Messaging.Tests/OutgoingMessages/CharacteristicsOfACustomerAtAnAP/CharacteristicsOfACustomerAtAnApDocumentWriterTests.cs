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
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using Messaging.Application.Common;
using Messaging.Application.Configuration;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.CharacteristicsOfACustomerAtAnAP;
using Messaging.Application.SchemaStore;
using Messaging.Domain.OutgoingMessages;
using Messaging.Infrastructure.Common;
using Messaging.Infrastructure.Configuration;
using Messaging.Infrastructure.Configuration.Serialization;
using Xunit;

namespace Messaging.Tests.OutgoingMessages.CharacteristicsOfACustomerAtAnAP
{
    public class CharacteristicsOfACustomerAtAnApDocumentWriterTests
    {
        private readonly CharacteristicsOfACustomerAtAnApDocumentWriter _documentWriter;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly IMarketActivityRecordParser _marketActivityRecordParser;
        private ISchemaProvider? _schemaProvider;

        public CharacteristicsOfACustomerAtAnApDocumentWriterTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProvider();
            _marketActivityRecordParser = new MarketActivityRecordParser(new Serializer());
            _documentWriter = new CharacteristicsOfACustomerAtAnApDocumentWriter(_marketActivityRecordParser);
        }

        [Fact]
        public async Task Document_is_valid()
        {
            var header = new MessageHeader("E03", "SenderId", "DDZ", "ReceiverId", "DDQ", Guid.NewGuid().ToString(), _systemDateTimeProvider.Now());
            var marketActivityRecords = new List<MarketActivityRecord>()
            {
                CreateMarketActivityRecord(),
                CreateMarketActivityRecord(),
            };

            var message = await _documentWriter.WriteAsync(header, marketActivityRecords.Select(record => _marketActivityRecordParser.From(record)).ToList()).ConfigureAwait(false);

            var schema = await GetSchema().ConfigureAwait(false);
            var assertDocument = await AssertXmlDocument
                .Document(message)
                .HasHeader(header)
                .HasType("E21")
                .NumberOfMarketActivityRecordsIs(2)
                .HasValidStructureAsync(schema!).ConfigureAwait(false);
            AssertMarketActivityRecords(marketActivityRecords, assertDocument);
        }

        private static void AssertMarketActivityRecords(List<MarketActivityRecord> marketActivityRecords, AssertXmlDocument assertDocument)
        {
            foreach (var marketActivityRecord in marketActivityRecords)
            {
                assertDocument
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "cim:originalTransactionIDReference_MktActivityRecord.mRID", marketActivityRecord.OriginalTransactionId)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "cim:validityStart_DateAndOrTime.dateTime", marketActivityRecord.ValidityStart.ToString())
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "cim:MarketEvaluationPoint/cim:mRID", marketActivityRecord.MarketEvaluationPoint.MarketEvaluationPointId)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "cim:MarketEvaluationPoint/cim:serviceCategory.ElectricalHeating", marketActivityRecord.MarketEvaluationPoint.ElectricalHeating.ToStringValue())
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "cim:MarketEvaluationPoint/cim:eletricalHeating_DateAndOrTime.dateTime", marketActivityRecord.MarketEvaluationPoint.ElectricalHeatingStart.ToString())
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "cim:MarketEvaluationPoint/cim:firstCustomer_MarketParticipant.mRID", marketActivityRecord.MarketEvaluationPoint.FirstCustomerId.Id)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "cim:MarketEvaluationPoint/cim:firstCustomer_MarketParticipant.name", marketActivityRecord.MarketEvaluationPoint.FirstCustomerName)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "cim:MarketEvaluationPoint/cim:secondCustomer_MarketParticipant.mRID", marketActivityRecord.MarketEvaluationPoint.SecondCustomerId.Id)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "cim:MarketEvaluationPoint/cim:secondCustomer_MarketParticipant.name", marketActivityRecord.MarketEvaluationPoint.SecondCustomerName)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "cim:MarketEvaluationPoint/cim:protectedName", marketActivityRecord.MarketEvaluationPoint.ProtectedName.ToStringValue())
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "cim:MarketEvaluationPoint/cim:hasEnergySupplier", marketActivityRecord.MarketEvaluationPoint.HasEnergySupplier.ToStringValue())
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "cim:MarketEvaluationPoint/cim:supplyStart_DateAndOrTime.dateTime", marketActivityRecord.MarketEvaluationPoint.SupplyStart.ToString());

                var usagePointLocations = marketActivityRecord.MarketEvaluationPoint.UsagePointLocation.ToList();
                var firstUsagePointLocation = usagePointLocations[0];
                assertDocument
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "/cim:UsagePointLocation[1]/cim:type", firstUsagePointLocation.Type)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "/cim:UsagePointLocation[1]/cim:geoInfoReference", firstUsagePointLocation.GeoInfoReference)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "/cim:UsagePointLocation[1]/cim:mainAddress/cim:streetDetail/cim:code", firstUsagePointLocation.MainAddress.StreetDetail.Code)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "/cim:UsagePointLocation[1]/cim:mainAddress/cim:streetDetail/cim:name", firstUsagePointLocation.MainAddress.StreetDetail.Name)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "/cim:UsagePointLocation[1]/cim:mainAddress/cim:streetDetail/cim:number", firstUsagePointLocation.MainAddress.StreetDetail.Number)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "/cim:UsagePointLocation[1]/cim:mainAddress/cim:streetDetail/cim:floorIdentification", firstUsagePointLocation.MainAddress.StreetDetail.FloorIdentification)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "/cim:UsagePointLocation[1]/cim:mainAddress/cim:streetDetail/cim:suiteNumber", firstUsagePointLocation.MainAddress.StreetDetail.SuiteNumber)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "/cim:UsagePointLocation[1]/cim:mainAddress/cim:townDetail/cim:name", firstUsagePointLocation.MainAddress.TownDetail.Name)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "/cim:UsagePointLocation[1]/cim:mainAddress/cim:townDetail/cim:country", firstUsagePointLocation.MainAddress.TownDetail.Country)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "/cim:UsagePointLocation[1]/cim:mainAddress/cim:townDetail/cim:code", firstUsagePointLocation.MainAddress.TownDetail.Code)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "/cim:UsagePointLocation[1]/cim:mainAddress/cim:townDetail/cim:section", firstUsagePointLocation.MainAddress.TownDetail.Section)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "/cim:UsagePointLocation[1]/cim:name", firstUsagePointLocation.Name)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "/cim:UsagePointLocation[1]/cim:attn_Names.name", firstUsagePointLocation.AttnName)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "/cim:UsagePointLocation[1]/cim:phone1/cim:ituPhone", firstUsagePointLocation.Phone1)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "/cim:UsagePointLocation[1]/cim:phone2/cim:ituPhone", firstUsagePointLocation.Phone2)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "/cim:UsagePointLocation[1]/cim:electronicAddress/cim:email1", firstUsagePointLocation.EmailAddress)
                    .HasMarketActivityRecordValue1(marketActivityRecord.Id, "/cim:UsagePointLocation[1]/cim:protectedAddress", firstUsagePointLocation.ProtectedAddress.ToStringValue());
            }
        }

        private MarketActivityRecord CreateMarketActivityRecord()
        {
            return new(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                _systemDateTimeProvider.Now(),
                new MarketEvaluationPoint(
                    "579999993331812345",
                    true,
                    _systemDateTimeProvider.Now(),
                    new MrId("Consumer1Id", "ARR"),
                    "Consumer1",
                    new MrId("Consumer2Id", "ARR"),
                    "Consumer2",
                    false,
                    false,
                    _systemDateTimeProvider.Now(),
                    new List<UsagePointLocation>()
                    {
                        new UsagePointLocation(
                            "D01",
                            Guid.NewGuid().ToString(),
                            new MainAddress(
                                new StreetDetail("001", "StreetName", "1", "1", "1"),
                                new TownDetail("001", "TownName", "TownSection", "DK"),
                                "8000",
                                "40"),
                            "MainAddressName",
                            "AttnName",
                            "Phone1Number",
                            "Phone2Number",
                            "SomeEmailAddress",
                            false),
                    }));
        }

        private Task<XmlSchema?> GetSchema()
        {
            _schemaProvider = SchemaProviderFactory.GetProvider(MediaTypeNames.Application.Xml);
            return _schemaProvider.GetSchemaAsync<XmlSchema>("characteristicsofacustomeratanap", "0.1");
        }
    }
}
