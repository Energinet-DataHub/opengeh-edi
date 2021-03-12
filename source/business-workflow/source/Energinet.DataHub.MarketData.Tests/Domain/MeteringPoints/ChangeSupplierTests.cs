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

using Energinet.DataHub.MarketData.Domain.MeteringPoints;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Rules.ChangeEnergySupplier;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using GreenEnergyHub.TestHelpers.Traits;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.MarketData.Tests.Domain.MeteringPoints
{
    [Trait(TraitNames.Category, TraitValues.UnitTest)]
    public class ChangeSupplierTests
    {
        private SystemDateTimeProviderStub _systemDateTimeProvider;

        public ChangeSupplierTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProviderStub();
        }

        [Theory]
        [InlineData("exchange")]
        public void Register_WhenMeteringPointTypeIsNotEligible_IsNotPossible(string meteringPointTypeName)
        {
            var meteringPointType = CreateMeteringPointTypeFromName(meteringPointTypeName);
            var meteringPoint = CreateMeteringPoint(meteringPointType);

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Rules, x => x is MeteringPointMustBeEnergySuppliableRule && x.IsBroken);
        }

        [Fact]
        public void Register_WhenProductionMeteringPointIsNotObligated_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Production);

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Rules, x => x is ProductionMeteringPointMustBeObligatedRule && x.IsBroken);
        }

        [Fact]
        public void Register_WhenMeteringPointIsClosedDown_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Production);
            meteringPoint.CloseDown();

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Rules, x => x is CannotBeInStateOfClosedDownRule && x.IsBroken);
        }

        [Fact]
        public void Register_WhenNoEnergySupplierIsAssociated_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Production);

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Rules, x => x is MustHaveEnergySupplierAssociatedRule && x.IsBroken);
        }

        [Fact]
        public void Register_WhenChangeOfSupplierIsRegisteredOnSameDate_IsNotPossible()
        {
            var customerId = CreateCustomerId();
            var energySupplierId = CreateEnergySupplierId();
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            meteringPoint.RegisterMoveIn(customerId, energySupplierId, GetFakeEffectuationDate().Minus(Duration.FromDays(1)));
            meteringPoint.ActivateMoveIn(customerId, energySupplierId);
            meteringPoint.RegisterChangeOfEnergySupplier(CreateEnergySupplierId(), _systemDateTimeProvider.Now(), _systemDateTimeProvider);

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Rules, x => x is ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRule && x.IsBroken);
        }

        [Fact]
        public void Register_WhenMoveInIsAlreadyRegisteredOnSameDate_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            meteringPoint.RegisterMoveIn(CreateCustomerId(), CreateEnergySupplierId(), _systemDateTimeProvider.Now());

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Rules, x => x is MoveInRegisteredOnSameDateIsNotAllowedRule && x.IsBroken);
        }

        [Fact]
        public void Register_WhenMoveOutIsAlreadyRegisteredOnSameDate_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            meteringPoint.RegisterMoveIn(CreateCustomerId(), CreateEnergySupplierId(), GetFakeEffectuationDate().Minus(Duration.FromDays(1)));
            meteringPoint.RegisterMoveOut(CreateCustomerId(), _systemDateTimeProvider.Now());

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Rules, x => x is MoveOutRegisteredOnSameDateIsNotAllowedRule && x.IsBroken);
        }

        [Fact]
        public void Register_WhenEffectuationDateIsInThePast_NotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            var effectuationDate = _systemDateTimeProvider.Now().Minus(Duration.FromDays(1));

            var result = CanChangeSupplier(meteringPoint, effectuationDate);

            Assert.Contains(result.Rules, x => x is EffectuationDateCannotBeInThePastRule && x.IsBroken);
        }

        [Fact]
        public void Register_WhenAllRulesAreSatisfied_Success()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            var customerId = CreateCustomerId();
            var energySupplierId = CreateEnergySupplierId();
            meteringPoint.RegisterMoveIn(customerId, energySupplierId, GetFakeEffectuationDate().Minus(Duration.FromDays(1)));
            meteringPoint.ActivateMoveIn(customerId, energySupplierId);

            meteringPoint.RegisterChangeOfEnergySupplier(CreateEnergySupplierId(), _systemDateTimeProvider.Now(), _systemDateTimeProvider);

            Assert.Contains(meteringPoint.DomainEvents!, e => e is EnergySupplierChangeRegistered);
        }

        private static MarketParticipantMrid CreateCustomerId()
        {
            return new MarketParticipantMrid("1");
        }

        private static MarketParticipantMrid CreateEnergySupplierId()
        {
            return new MarketParticipantMrid("FakeId");
        }

        private static MeteringPoint CreateMeteringPoint(MeteringPointType meteringPointType)
        {
            var meteringPointId = CreateGsrnNumber();
            return new MeteringPoint(meteringPointId, meteringPointType);
        }

        private static GsrnNumber CreateGsrnNumber()
        {
            return GsrnNumber.Create("571234567891234568");
        }

        private static MeteringPointType CreateMeteringPointTypeFromName(string meteringPointTypeName)
        {
            return MeteringPointType.FromName<MeteringPointType>(meteringPointTypeName);
        }

        private static Instant GetFakeEffectuationDate()
        {
            return Instant.FromUtc(2000, 1, 1, 0, 0);
        }

        private BusinessRulesValidationResult CanChangeSupplier(MeteringPoint meteringPoint)
        {
            return meteringPoint.CanChangeSupplier(CreateEnergySupplierId(), _systemDateTimeProvider.Now(), _systemDateTimeProvider);
        }

        private BusinessRulesValidationResult CanChangeSupplier(MeteringPoint meteringPoint, Instant effectuationDate)
        {
            return meteringPoint.CanChangeSupplier(CreateEnergySupplierId(), effectuationDate, _systemDateTimeProvider);
        }
    }
}
