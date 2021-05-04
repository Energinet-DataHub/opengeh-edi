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
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Rules.ChangeEnergySupplier;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.Tests.Domain.MeteringPoints.ChangeOfSupplier
{
    [UnitTest]
    public class AcceptTests
    {
        private SystemDateTimeProviderStub _systemDateTimeProvider;

        public AcceptTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProviderStub();
        }

        [Theory]
        [InlineData("exchange")]
        public void Accept_WhenMeteringPointTypeIsNotEligible_IsNotPossible(string meteringPointTypeName)
        {
            var meteringPointType = CreateMeteringPointTypeFromName(meteringPointTypeName);
            var meteringPoint = CreateMeteringPoint(meteringPointType);

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Errors, x => x.Rule == typeof(MeteringPointMustBeEnergySuppliableRule));
        }

        [Fact]
        public void Accept_WhenProductionMeteringPointIsNotObligated_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Production);

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Errors, x => x.Rule == typeof(ProductionMeteringPointMustBeObligatedRule));
        }

        [Fact]
        public void Accept_WhenMeteringPointIsClosedDown_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Production);
            meteringPoint.CloseDown();

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Errors, x => x.Rule == typeof(CannotBeInStateOfClosedDownRule));
        }

        [Fact]
        public void Accept_WhenNoEnergySupplierIsAssociated_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Production);

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Errors, x => x.Rule == typeof(MustHaveEnergySupplierAssociatedRule));
        }

        [Fact]
        public void Accept_WhenChangeOfSupplierIsRegisteredOnSameDate_IsNotPossible()
        {
            var consumerId = CreateConsumerId();
            var energySupplierId = CreateSupplierId();
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            var moveInDate = _systemDateTimeProvider.Now().Minus(Duration.FromDays(1));
            var moveInProcessId = CreateProcessId();

            meteringPoint.AcceptConsumerMoveIn(consumerId, energySupplierId, moveInDate, moveInProcessId);
            meteringPoint.EffectuateConsumerMoveIn(moveInProcessId, _systemDateTimeProvider);
            meteringPoint.AcceptChangeOfSupplier(CreateSupplierId(), _systemDateTimeProvider.Now(), CreateProcessId(), _systemDateTimeProvider);

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Errors, x => x.Rule == typeof(ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRule));
        }

        [Fact]
        public void Accept_WhenMoveInIsAlreadyRegisteredOnSameDate_IsNotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            var moveInDate = _systemDateTimeProvider.Now();
            meteringPoint.AcceptConsumerMoveIn(CreateConsumerId(), CreateSupplierId(), moveInDate, CreateProcessId());

            var result = CanChangeSupplier(meteringPoint);

            Assert.Contains(result.Errors, x => x.Rule == typeof(MoveInRegisteredOnSameDateIsNotAllowedRule));
        }

        // TODO: Ignore Move related rules until implementation is in scope
        // [Fact]
        // public void Register_WhenMoveOutIsAlreadyRegisteredOnSameDate_IsNotPossible()
        // {
        //     var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
        //
        //     var moveInDate = _systemDateTimeProvider.Now().Minus(Duration.FromDays(1));
        //     meteringPoint.AcceptConsumerMoveIn(CreateConsumerId(), CreateSupplierId(), moveInDate, CreateProcessId());
        //     meteringPoint.RegisterMoveOut(CreateCustomerId(), _systemDateTimeProvider.Now());
        //
        //     var result = CanChangeSupplier(meteringPoint);
        //
        //     Assert.Contains(result.Errors, x => x.Rule == typeof(MoveOutRegisteredOnSameDateIsNotAllowedRule));
        // }
        [Fact]
        public void Accept_WhenEffectuationDateIsInThePast_NotPossible()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            var effectuationDate = _systemDateTimeProvider.Now().Minus(Duration.FromDays(1));

            var result = CanChangeSupplier(meteringPoint, effectuationDate);

            Assert.Contains(result.Errors, x => x.Rule == typeof(EffectuationDateCannotBeInThePastRule));
        }

        [Fact]
        public void Accept_WhenAllRulesAreSatisfied_Success()
        {
            var meteringPoint = CreateMeteringPoint(MeteringPointType.Consumption);
            var consumerId = CreateConsumerId();
            var energySupplierId = CreateSupplierId();
            var moveInDate = _systemDateTimeProvider.Now().Minus(Duration.FromDays(1));
            var moveInprocessId = CreateProcessId();
            meteringPoint.AcceptConsumerMoveIn(consumerId, energySupplierId, moveInDate, moveInprocessId);
            meteringPoint.EffectuateConsumerMoveIn(moveInprocessId, _systemDateTimeProvider);

            meteringPoint.AcceptChangeOfSupplier(CreateSupplierId(), _systemDateTimeProvider.Now(), CreateProcessId(), _systemDateTimeProvider);

            Assert.Contains(meteringPoint.DomainEvents!, e => e is EnergySupplierChangeRegistered);
        }

        private static ProcessId CreateProcessId()
        {
            return new ProcessId(Guid.NewGuid().ToString());
        }

        private static ConsumerId CreateConsumerId()
        {
            return new ConsumerId(1);
        }

        private static EnergySupplierId CreateSupplierId()
        {
            return new EnergySupplierId(Guid.NewGuid());
        }

        private static AccountingPoint CreateMeteringPoint(MeteringPointType meteringPointType)
        {
            var meteringPointId = CreateGsrnNumber();
            return new AccountingPoint(meteringPointId, meteringPointType);
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

        private BusinessRulesValidationResult CanChangeSupplier(AccountingPoint accountingPoint)
        {
            return accountingPoint.ChangeSupplierAcceptable(CreateSupplierId(), _systemDateTimeProvider.Now(), _systemDateTimeProvider);
        }

        private BusinessRulesValidationResult CanChangeSupplier(AccountingPoint accountingPoint, Instant effectuationDate)
        {
            return accountingPoint.ChangeSupplierAcceptable(CreateSupplierId(), effectuationDate, _systemDateTimeProvider);
        }
    }
}
