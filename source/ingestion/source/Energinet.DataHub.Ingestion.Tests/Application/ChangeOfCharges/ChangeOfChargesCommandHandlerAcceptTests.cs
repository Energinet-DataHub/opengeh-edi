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
using System.Linq;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges.Repositories;
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using GreenEnergyHub.Iso8601;
using GreenEnergyHub.TestHelpers;
using GreenEnergyHub.TestHelpers.Traits;
using Moq;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.Ingestion.Tests.Application.ChangeOfCharges
{
    [Trait(TraitNames.Category, TraitValues.UnitTest)]
    public class ChangeOfChargesCommandHandlerAcceptTests
    {
        [Theory]
        [InlineAutoDomainData(1)]
        [InlineAutoDomainData(5)]
        public async Task AcceptAsync_WhenAddingDuration_ShouldInvokeStorageProviderWithCalculatedTimeProperty(int numberOfDurations, [Frozen] Mock<IIso8601Durations> iso8601Durations, [Frozen] Mock<IChargeRepository> storageProvider, ChangeOfChargesCommandHandlerTestable sut)
        {
            // Arrange
            var startTime = SystemClock.Instance.GetCurrentInstant();
            var message = new ChangeOfChargesMessage
            {
                MktActivityRecord = new MktActivityRecord
                { ValidityStartDate = startTime },
                Period = new ChargeTypePeriod
                {
                    Resolution = "PT1H",
                    Points = new List<Point>
                    {
                        new Point { Position = numberOfDurations, PriceAmount = 1 },
                    },
                },
            };

            var calculatedInstant =
                message.MktActivityRecord.ValidityStartDate.Plus(Duration.FromHours(numberOfDurations));
            iso8601Durations.Setup(i => i.AddDuration(It.IsAny<Instant>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(calculatedInstant);

            // Act
            await sut.CallAcceptAsync(message).ConfigureAwait(false);

            // Assert
            storageProvider.Verify(s => s.StoreChargeAsync(It.Is<ChangeOfChargesMessage>(msg =>
                msg.Period != null && msg.Period.Points != null &&
                msg.Period.Points.First().Time == calculatedInstant)));
        }
    }
}
