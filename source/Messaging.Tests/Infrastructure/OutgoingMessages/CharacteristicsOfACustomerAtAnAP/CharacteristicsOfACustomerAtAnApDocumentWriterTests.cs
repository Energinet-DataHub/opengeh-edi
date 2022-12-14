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
using System.Threading.Tasks;
using System.Xml.Schema;
using Messaging.Application.Configuration;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Application.SchemaStore;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;
using Messaging.Infrastructure.Common;
using Messaging.Infrastructure.Configuration;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.Infrastructure.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;
using Messaging.Tests.Infrastructure.OutgoingMessages.Asserts;
using NodaTime;
using Xunit;

namespace Messaging.Tests.Infrastructure.OutgoingMessages.CharacteristicsOfACustomerAtAnAP
{
    public class CharacteristicsOfACustomerAtAnApDocumentWriterTests
    {
        private const string NamespacePrefix = "cim";
        private readonly CharacteristicsOfACustomerAtAnApMessageWriter _messageWriter;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly IMessageRecordParser _messageRecordParser;
        private ISchemaProvider? _schemaProvider;

        public CharacteristicsOfACustomerAtAnApDocumentWriterTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProvider();
            _messageRecordParser = new MessageRecordParser(new Serializer());
            _messageWriter = new CharacteristicsOfACustomerAtAnApMessageWriter(_messageRecordParser);
        }

        [Fact]
        public async Task Document_is_valid()
        {
            var marketActivityRecords = new List<MarketActivityRecord>()
            {
                CreateMarketActivityRecord(null, null, _systemDateTimeProvider.Now()),
                CreateMarketActivityRecord(null, null, _systemDateTimeProvider.Now()),
            };

            var header = CreateHeader(MarketRole.EnergySupplier);
            var message = await WriteDocumentAsync(header, marketActivityRecords.ToArray()).ConfigureAwait(false);

            var schema = await GetSchema().ConfigureAwait(false);
            var assertDocument = await AssertXmlDocument
                .Document(message, NamespacePrefix)
                .HasValue("type", "E21")
                .HasValue("process.processType", header.ProcessType)
                .HasValue("businessSector.type", "23")
                .HasValue("sender_MarketParticipant.mRID", header.SenderId)
                .HasValue("sender_MarketParticipant.marketRole.type", header.SenderRole)
                .HasValue("receiver_MarketParticipant.mRID", header.ReceiverId)
                .HasValue("receiver_MarketParticipant.marketRole.type", header.ReceiverRole)
                .NumberOfMarketActivityRecordsIs(2)
                .HasValidStructureAsync(schema!).ConfigureAwait(false);
            AssertMarketActivityRecord(marketActivityRecords.First(), assertDocument);
        }

        [Fact]
        public async Task Eletrical_heating_date_is_excluded_when_no_date_is_specified()
        {
            var document = await WriteDocumentAsync(CreateHeader(MarketRole.GridOperator), CreateMarketActivityRecord(null, null, null)).ConfigureAwait(false);

            AssertXmlDocument.Document(document, NamespacePrefix)
                .IsNotPresent("MktActivityRecord[1]/MarketEvaluationPoint/eletricalHeating_DateAndOrTime.dateTime");
        }

        [Fact]
        public async Task Second_customer_id_is_not_allowed_when_receiver_is_a_grid_operator()
        {
            var message = await WriteDocumentAsync(CreateHeader(MarketRole.GridOperator), CreateMarketActivityRecord())
                .ConfigureAwait(false);

            AssertXmlDocument
                .Document(message, NamespacePrefix)
                .IsNotPresent("MktActivityRecord[1]/MarketEvaluationPoint/secondCustomer_MarketParticipant.mRID");
        }

        [Fact]
        public async Task Supply_start_is_not_allowed_when_receiver_is_a_grid_operator()
        {
            var message = await WriteDocumentAsync(CreateHeader(MarketRole.GridOperator), CreateMarketActivityRecord()).ConfigureAwait(false);

            AssertXmlDocument
                .Document(message, NamespacePrefix)
                .IsNotPresent("MktActivityRecord[1]/MarketEvaluationPoint/supplyStart_DateAndOrTime.dateTime");
        }

        [Fact]
        public async Task Customer_mrid_is_not_allowed_when_type_is_social_security_number()
        {
            var document =
                await WriteDocumentAsync(CreateHeader(MarketRole.EnergySupplier), CreateMarketActivityRecord(new MrId("1", "AAR"), new MrId("1", "AAR")))
                    .ConfigureAwait(false);

            AssertXmlDocument.Document(document, NamespacePrefix)
                .IsNotPresent("MktActivityRecord[1]/MarketEvaluationPoint/firstCustomer_MarketParticipant.mRID")
                .IsNotPresent("MktActivityRecord[1]/MarketEvaluationPoint/secondCustomer_MarketParticipant.mRID");
        }

        private static void AssertMarketActivityRecord(MarketActivityRecord marketActivityRecord, AssertXmlDocument assertDocument)
        {
            var usagePointLocations = marketActivityRecord.MarketEvaluationPoint.UsagePointLocation.ToList();
            var firstUsagePointLocation = usagePointLocations.First();

            assertDocument
                    .IsNotPresent("MktActivityRecord[1]/originalTransactionIDReference_MktActivityRecord.mRID")
                    .HasValue("MktActivityRecord[1]/validityStart_DateAndOrTime.dateTime", marketActivityRecord.ValidityStart.ToString())
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/mRID", marketActivityRecord.MarketEvaluationPoint.MarketEvaluationPointId)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/serviceCategory.ElectricalHeating", marketActivityRecord.MarketEvaluationPoint.ElectricalHeating.ToStringValue())
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/eletricalHeating_DateAndOrTime.dateTime", marketActivityRecord.MarketEvaluationPoint.ElectricalHeatingStart.ToString() ?? string.Empty)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/firstCustomer_MarketParticipant.mRID", marketActivityRecord.MarketEvaluationPoint.FirstCustomerId.Id)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/firstCustomer_MarketParticipant.name", marketActivityRecord.MarketEvaluationPoint.FirstCustomerName)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/secondCustomer_MarketParticipant.mRID", marketActivityRecord.MarketEvaluationPoint.SecondCustomerId!.Id)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/secondCustomer_MarketParticipant.name", marketActivityRecord.MarketEvaluationPoint.SecondCustomerName!)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/protectedName", marketActivityRecord.MarketEvaluationPoint.ProtectedName.ToStringValue())
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/hasEnergySupplier", marketActivityRecord.MarketEvaluationPoint.HasEnergySupplier.ToStringValue())
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/supplyStart_DateAndOrTime.dateTime", marketActivityRecord.MarketEvaluationPoint.SupplyStart.ToString())
                    .NumberOfUsagePointLocationsIs(2)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/UsagePointLocation[1]/type", firstUsagePointLocation.Type)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/UsagePointLocation[1]/geoInfoReference", firstUsagePointLocation.GeoInfoReference)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/UsagePointLocation[1]/mainAddress/streetDetail/code", firstUsagePointLocation.MainAddress.StreetDetail.Code)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/UsagePointLocation[1]/mainAddress/streetDetail/name", firstUsagePointLocation.MainAddress.StreetDetail.Name)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/UsagePointLocation[1]/mainAddress/streetDetail/number", firstUsagePointLocation.MainAddress.StreetDetail.Number)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/UsagePointLocation[1]/mainAddress/streetDetail/floorIdentification", firstUsagePointLocation.MainAddress.StreetDetail.FloorIdentification)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/UsagePointLocation[1]/mainAddress/streetDetail/suiteNumber", firstUsagePointLocation.MainAddress.StreetDetail.SuiteNumber)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/UsagePointLocation[1]/mainAddress/townDetail/name", firstUsagePointLocation.MainAddress.TownDetail.Name)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/UsagePointLocation[1]/mainAddress/townDetail/country", firstUsagePointLocation.MainAddress.TownDetail.Country)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/UsagePointLocation[1]/mainAddress/townDetail/code", firstUsagePointLocation.MainAddress.TownDetail.Code)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/UsagePointLocation[1]/mainAddress/townDetail/section", firstUsagePointLocation.MainAddress.TownDetail.Section)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/UsagePointLocation[1]/name", firstUsagePointLocation.Name)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/UsagePointLocation[1]/attn_Names.name", firstUsagePointLocation.AttnName)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/UsagePointLocation[1]/phone1/ituPhone", firstUsagePointLocation.Phone1)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/UsagePointLocation[1]/phone2/ituPhone", firstUsagePointLocation.Phone2)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/UsagePointLocation[1]/electronicAddress/email1", firstUsagePointLocation.EmailAddress)
                    .HasValue("MktActivityRecord[1]/MarketEvaluationPoint/UsagePointLocation[1]/protectedAddress", firstUsagePointLocation.ProtectedAddress.ToStringValue());
        }

        private MarketActivityRecord CreateMarketActivityRecord(MrId? firstCustomerId = null, MrId? secondCustomerId = null, Instant? electricalHeatingStartDate = null)
        {
            return new(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                _systemDateTimeProvider.Now(),
                new MarketEvaluationPoint(
                    "579999993331812345",
                    true,
                    electricalHeatingStartDate,
                    firstCustomerId ?? new MrId("Consumer1Id", "VAT"),
                    "Consumer1",
                    secondCustomerId ?? new MrId("Consumer2Id", "VAT"),
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
            _schemaProvider = new XmlSchemaProvider();
            return _schemaProvider.GetSchemaAsync<XmlSchema>("characteristicsofacustomeratanap", "0.1");
        }

        private MessageHeader CreateHeader(MarketRole messageReceiverRole)
        {
            return new MessageHeader("E03", "SenderId", "DDZ", "ReceiverId", messageReceiverRole.Name, Guid.NewGuid().ToString(), _systemDateTimeProvider.Now());
        }

        private Task<Stream> WriteDocumentAsync(MessageHeader header, params MarketActivityRecord[] marketActivityRecords)
        {
            return _messageWriter.WriteAsync(header, marketActivityRecords.Select(record => _messageRecordParser.From(record)).ToList());
        }
    }
}
