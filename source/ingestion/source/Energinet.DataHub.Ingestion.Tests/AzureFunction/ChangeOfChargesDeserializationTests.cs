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
using System.Threading.Tasks;
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Domain.Common;
using Energinet.DataHub.Ingestion.Infrastructure;
using FluentAssertions;
using GreenEnergyHub.Json;
using GreenEnergyHub.Messaging.MessageTypes.Common;
using GreenEnergyHub.TestHelpers.Traits;
using Microsoft.Extensions.Logging;
using NodaTime;
using NSubstitute;
using Xunit;
using MarketDocument = Energinet.DataHub.Ingestion.Domain.Common.MarketDocument;
using MarketParticipant = Energinet.DataHub.Ingestion.Domain.Common.MarketParticipant;

namespace Energinet.DataHub.Ingestion.Tests.AzureFunction
{
    [Trait(TraitNames.Category, TraitValues.UnitTest)]
    public class ChangeOfChargesDeserializationTests
    {
        [Fact]
        public async Task Can_deserialize_incoming_json()
        {
            var logger = Substitute.For<ILogger<JsonMessageDeserializer>>();
            var serializer = new JsonMessageDeserializer(logger, new JsonSerializer());
            var targetType = typeof(ChangeOfChargesMessage);
            var expectedMessage = CreateChangeOfFeeMessageEquivalentToJsonAssetFile();

            await using var fs = File.OpenRead("Assets/ChangeOfFee.json");

            var request = await serializer.RehydrateAsync(fs, targetType).ConfigureAwait(false);
            var actualMessage = request as ChangeOfChargesMessage;

            actualMessage.Should().BeEquivalentTo(expectedMessage);
        }

        /// <summary>
        /// An <see cref="ChangeOfChargesMessage"/> object equivalent to the file "Assets/ChangeOfFee.json.
        /// </summary>
        private static ChangeOfChargesMessage CreateChangeOfFeeMessageEquivalentToJsonAssetFile()
        {
            return new ChangeOfChargesMessage
            {
                CorrelationId = "CorrelationId",
                Period =
                    new ChargeTypePeriod
                    {
                        Resolution = "PT1H",
                        Points = new List<Point>
                        {
                            new Point
                            {
                                Position = 42,
                                PriceAmount = 42.42m,
                                Time = Instant.FromUtc(1982, 7, 3, 1, 2),
                            },
                        },
                    },
                Transaction = new Transaction { MRID = "Transaction/mRID" },
                Type = "Type",
                RequestDate = Instant.FromUtc(1975, 2, 22, 12, 34),
                LastUpdatedBy = "LastUpdatedBy",
                ChargeTypeMRid = "ChargeType_mRID",
                ChargeTypeOwnerMRid = "ChargeTypeOwner_mRID",
                MarketDocument = new MarketDocument
                {
                    MRid = "MarketDocument/mRID",
                    CreatedDateTime = Instant.FromUtc(2021, 03, 14, 23, 12),
                    ProcessType = ProcessType.UpdateChargeInformation,
                    MarketServiceCategoryKind = ServiceCategoryKind.Electricity,
                    SenderMarketParticipant = new MarketParticipant
                    {
                        Id = 57,
                        Name = "SenderMarketParticipant_name",
                        Role = MarketParticipantRole.EnergySupplier,
                        MRid = "SenderMarketParticipant_mRID",
                    },
                    ReceiverMarketParticipant = new MarketParticipant
                    {
                        Id = 57,
                        Name = "ReceiverMarketParticipant_name",
                        Role = MarketParticipantRole.EnergySupplier,
                        MRid = "ReceiverMarketParticipant_mRID",
                    },
                },
                MktActivityRecord = new MktActivityRecord
                {
                    Status = MktActivityRecordStatus.Deletion,
                    MRid = "MktActivityRecord_mRID",
                    ChargeType = new ChargeType
                    {
                        Description = "d",
                        Name = "n",
                        TaxIndicator = true,
                        TransparentInvoicing = true,
                        VATPayer = "VATPayer",
                    },
                },
            };
        }
    }
}
