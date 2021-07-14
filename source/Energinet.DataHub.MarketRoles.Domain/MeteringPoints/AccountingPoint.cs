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
using System.Collections.ObjectModel;
using System.Linq;
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Rules.ChangeEnergySupplier;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketRoles.Domain.MeteringPoints
{
    public sealed class AccountingPoint : AggregateRootBase
    {
        private readonly MeteringPointType _meteringPointType;
        private readonly bool _isProductionObligated;
        private readonly List<BusinessProcess> _businessProcesses = new List<BusinessProcess>();
        private readonly List<ConsumerRegistration> _consumerRegistrations = new List<ConsumerRegistration>();
        private readonly List<SupplierRegistration> _supplierRegistrations = new List<SupplierRegistration>();
        private PhysicalState _physicalState;

        public AccountingPoint(GsrnNumber gsrnNumber, MeteringPointType meteringPointType)
        {
            Id = AccountingPointId.New();
            GsrnNumber = gsrnNumber;
            _meteringPointType = meteringPointType;
            _physicalState = PhysicalState.New;
            AddDomainEvent(new MeteringPointCreated(GsrnNumber, _meteringPointType));
        }

        private AccountingPoint(GsrnNumber gsrnNumber, MeteringPointType meteringPointType, bool isProductionObligated)
            : this(gsrnNumber, meteringPointType)
        {
            _isProductionObligated = isProductionObligated;
        }

        public AccountingPointId Id { get; }

        public GsrnNumber GsrnNumber { get; private set; }

        public static AccountingPoint CreateProduction(GsrnNumber gsrnNumber, bool isObligated)
        {
            return new AccountingPoint(gsrnNumber, MeteringPointType.Production, isObligated);
        }

        public BusinessRulesValidationResult ChangeSupplierAcceptable(EnergySupplierId energySupplierId, Instant supplyStartDate, ISystemDateTimeProvider systemDateTimeProvider)
        {
            if (energySupplierId is null)
            {
                throw new ArgumentNullException(nameof(energySupplierId));
            }

            if (systemDateTimeProvider == null)
            {
                throw new ArgumentNullException(nameof(systemDateTimeProvider));
            }

            var rules = new Collection<IBusinessRule>()
            {
                new MeteringPointMustBeEnergySuppliableRule(_meteringPointType),
                new ProductionMeteringPointMustBeObligatedRule(_meteringPointType, _isProductionObligated),
                new CannotBeInStateOfClosedDownRule(_physicalState),
                new MustHaveEnergySupplierAssociatedRule(GetCurrentSupplier(systemDateTimeProvider)),
                new ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRule(_businessProcesses.AsReadOnly(), supplyStartDate),
                new MoveInRegisteredOnSameDateIsNotAllowedRule(_businessProcesses.AsReadOnly(), supplyStartDate),
                // TODO: Ignore move out process until implementation is in scope
                //new MoveOutRegisteredOnSameDateIsNotAllowedRule(_businessProcesses.AsReadOnly(), supplyStartDate),
                new EffectiveDateCannotBeInThePastRule(supplyStartDate, systemDateTimeProvider.Now()),
            };

            return new BusinessRulesValidationResult(rules);
        }

        public void AcceptChangeOfSupplier(EnergySupplierId energySupplierId, Instant supplyStartDate, Transaction transaction, ISystemDateTimeProvider systemDateTimeProvider)
        {
            if (energySupplierId == null) throw new ArgumentNullException(nameof(energySupplierId));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (systemDateTimeProvider == null) throw new ArgumentNullException(nameof(systemDateTimeProvider));
            if (!ChangeSupplierAcceptable(energySupplierId, supplyStartDate, systemDateTimeProvider).Success)
            {
                throw new BusinessProcessException(
                    "Cannot accept change of supplier request due to violation of one or more business rules.");
            }

            var businessProcess = CreateBusinessProcess(transaction, supplyStartDate, BusinessProcessType.ChangeOfSupplier);
            _businessProcesses.Add(businessProcess);
            _supplierRegistrations.Add(new SupplierRegistration(energySupplierId, businessProcess.BusinessProcessId));

            AddDomainEvent(new EnergySupplierChangeRegistered(Id, GsrnNumber, businessProcess.BusinessProcessId, transaction, supplyStartDate));
        }

        public void EffectuateChangeOfSupplier(Transaction transaction, ISystemDateTimeProvider systemDateTimeProvider)
        {
            if (transaction is null) throw new ArgumentNullException(nameof(transaction));
            if (systemDateTimeProvider == null) throw new ArgumentNullException(nameof(systemDateTimeProvider));

            var businessProcess = GetBusinessProcess(transaction, BusinessProcessType.ChangeOfSupplier);
            businessProcess.Effectuate(systemDateTimeProvider);

            DiscontinueCurrentSupplier(businessProcess, systemDateTimeProvider);

            var futureSupplier = GetFutureSupplierRegistration(businessProcess);
            StartOfSupplyForFutureSupplier(businessProcess, futureSupplier);

            AddDomainEvent(new EnergySupplierChanged(Id.Value, GsrnNumber.Value, businessProcess.BusinessProcessId.Value, transaction.Value, futureSupplier.EnergySupplierId.Value, businessProcess.EffectiveDate));
        }

        public void CloseDown()
        {
            if (_physicalState != PhysicalState.ClosedDown)
            {
                _physicalState = PhysicalState.ClosedDown;
                AddDomainEvent(new MeteringPointClosedDown(GsrnNumber));
            }
        }

        public BusinessRulesValidationResult ConsumerMoveInAcceptable(Instant moveInDate)
        {
            var rules = new Collection<IBusinessRule>()
            {
                new MoveInRegisteredOnSameDateIsNotAllowedRule(_businessProcesses.AsReadOnly(), moveInDate),
            };

            return new BusinessRulesValidationResult(rules);
        }

        public void AcceptConsumerMoveIn(ConsumerId consumerId, EnergySupplierId energySupplierId, Instant moveInDate, Transaction transaction)
        {
            if (consumerId == null) throw new ArgumentNullException(nameof(consumerId));
            if (energySupplierId == null) throw new ArgumentNullException(nameof(energySupplierId));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            if (!ConsumerMoveInAcceptable(moveInDate).Success)
            {
                throw new BusinessProcessException(
                    "Cannot accept move in request due to violation of one or more business rules.");
            }

            var businessProcess = CreateBusinessProcess(transaction, moveInDate, BusinessProcessType.MoveIn);
            _businessProcesses.Add(businessProcess);
            _consumerRegistrations.Add(new ConsumerRegistration(consumerId, businessProcess.BusinessProcessId));
            _supplierRegistrations.Add(new SupplierRegistration(energySupplierId, businessProcess.BusinessProcessId));

            AddDomainEvent(new ConsumerMoveInAccepted(Id.Value, GsrnNumber.Value, businessProcess.BusinessProcessId.Value, businessProcess.Transaction.Value, consumerId.Value, energySupplierId.Value, moveInDate));
        }

        public void EffectuateConsumerMoveIn(Transaction transaction, ISystemDateTimeProvider systemDateTimeProvider)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            var businessProcess = GetBusinessProcess(transaction, BusinessProcessType.MoveIn);

            businessProcess.Effectuate(systemDateTimeProvider);
            var newSupplier = _supplierRegistrations.Find(supplier => supplier.BusinessProcessId.Equals(businessProcess.BusinessProcessId))!;
            newSupplier.StartOfSupply(businessProcess.EffectiveDate);

            var consumer = _consumerRegistrations.Find(consumerRegistration => consumerRegistration.BusinessProcessId.Equals(businessProcess.BusinessProcessId))!;

            AddDomainEvent(new ConsumerMovedIn(Id.Value, GsrnNumber.Value, businessProcess.BusinessProcessId.Value, consumer.ConsumerId.Value, consumer.MoveInDate));
            AddDomainEvent(new EnergySupplierChanged(Id.Value, GsrnNumber.Value, businessProcess.BusinessProcessId.Value, businessProcess.Transaction.Value, newSupplier.EnergySupplierId.Value, businessProcess.EffectiveDate));
        }

        public void CancelChangeOfSupplier(Transaction transaction)
        {
            if (transaction is null) throw new ArgumentNullException(nameof(transaction));

            var businessProcess = GetBusinessProcess(transaction, BusinessProcessType.ChangeOfSupplier);
            businessProcess.Cancel();
            AddDomainEvent(new ChangeOfSupplierCancelled(Id, GsrnNumber, businessProcess.BusinessProcessId, transaction));
        }

        private static void StartOfSupplyForFutureSupplier(BusinessProcess businessProcess, SupplierRegistration supplierRegistration)
        {
            supplierRegistration.StartOfSupply(businessProcess.EffectiveDate);
        }

        private SupplierRegistration GetFutureSupplierRegistration(BusinessProcess businessProcess)
        {
            var futureSupplier = _supplierRegistrations.Find(s => s.BusinessProcessId.Equals(businessProcess.BusinessProcessId));
            if (futureSupplier == null)
            {
                throw new BusinessProcessException(
                    $"Could find supplier registration of process id {businessProcess.Transaction.Value}.");
            }

            return futureSupplier;
        }

        private void DiscontinueCurrentSupplier(BusinessProcess businessProcess, ISystemDateTimeProvider systemDateTimeProvider)
        {
            var currentSupplier = GetCurrentSupplier(systemDateTimeProvider);
            if (currentSupplier == null)
            {
                throw new BusinessProcessException($"Could not find current energy supplier.");
            }

            currentSupplier.MarkEndOfSupply(businessProcess.EffectiveDate);
        }

        private SupplierRegistration? GetCurrentSupplier(ISystemDateTimeProvider systemDateTimeProvider)
        {
            return _supplierRegistrations.Find(supplier =>
                supplier.StartOfSupplyDate <= systemDateTimeProvider.Now() && supplier.EndOfSupplyDate == null);
        }

        private BusinessProcess GetBusinessProcess(Transaction transaction, BusinessProcessType businessProcessType)
        {
            var businessProcess =
                _businessProcesses.Find(p => p.Transaction.Equals(transaction) && p.ProcessType == businessProcessType);
            if (businessProcess == null)
            {
                throw new BusinessProcessException($"Business process ({businessProcessType.Name}) {transaction.ToString()} does not exist.");
            }

            return businessProcess;
        }

        private BusinessProcess CreateBusinessProcess(Transaction transaction, Instant effectiveDate, BusinessProcessType businessProcessType)
        {
            if (_businessProcesses.Any(p => p.Transaction.Equals(transaction)))
            {
                throw new BusinessProcessException($"Process id {transaction.Value} does already exist.");
            }

            return new BusinessProcess(BusinessProcessId.New(), transaction, effectiveDate, businessProcessType);
        }
    }
}
