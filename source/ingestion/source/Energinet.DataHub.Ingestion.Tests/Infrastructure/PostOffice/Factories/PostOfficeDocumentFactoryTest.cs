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
using System.Linq;
using AutoFixture.Xunit2;
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Domain.Common;
using Energinet.DataHub.Ingestion.Infrastructure.PostOffice.Factories;
using FluentAssertions;
using GreenEnergyHub.TestHelpers;
using GreenEnergyHub.TestHelpers.FluentAssertionsExtensions;
using GreenEnergyHub.TestHelpers.Traits;
using Moq;
using Xunit;
using MarketParticipant = Energinet.DataHub.Ingestion.Domain.Common.MarketParticipant;

namespace Energinet.DataHub.Ingestion.Tests.Infrastructure.PostOffice.Factories
{
    [Trait(TraitNames.Category, TraitValues.UnitTest)]
    public class PostOfficeDocumentFactoryTest
    {
        [Theory]
        [InlineAutoDomainData]
        public void Ensure_all_types_of_MarketParticipantRole_are_handled_by_Create(
            PostOfficeDocumentFactory sut,
            ChangeOfChargesMessage changeOfChargesMessage,
            MarketParticipant marketParticipant)
        {
            // Arrange
            MakeMessageValid(changeOfChargesMessage);
            var receivers = new List<MarketParticipant> { marketParticipant };
            var roles = Enum.GetValues(typeof(MarketParticipantRole)).Cast<MarketParticipantRole>();

            foreach (var role in roles)
            {
                changeOfChargesMessage.MarketDocument!.SenderMarketParticipant!.Role = role;

                // Act & Assert
                // Create throws a NotImplemented exception if a value in MarketParticipantRole is not handled.
                _ = sut.Create(receivers, changeOfChargesMessage).ToList();
            }
        }

        [Theory]
        [InlineAutoDomainData]
        public void Ensure_all_properties_are_mapped(
            [Frozen] Mock<IPostOfficeDocumentFactorySettings> settings,
            ChangeOfChargesMessage changeOfChargesMessage,
            MarketParticipant marketParticipant,
            PostOfficeDocumentFactory sut)
        {
            // Arrange
            settings.Setup(x => x.GetHubMRid()).Returns("MRidFromSettings");
            MakeMessageValid(changeOfChargesMessage);
            var receivers = new List<MarketParticipant> { marketParticipant };

            // Act
            var actual = sut.Create(receivers, changeOfChargesMessage)
                .ToList();

            // Assert
            actual.ForEach(d => d.Should().NotContainNullsOrEmptyEnumerables());
        }

        private static void MakeMessageValid(ChangeOfChargesMessage changeOfChargesMessage)
        {
            changeOfChargesMessage.MarketDocument!.SenderMarketParticipant!.MRid = "ads";
            changeOfChargesMessage.Type = "D01";
            changeOfChargesMessage.MktActivityRecord!.ChargeType!.VATPayer = "D01";
        }
    }
}
