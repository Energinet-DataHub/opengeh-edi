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
            AccountingPointId = new AccountingPointId();
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

#pragma warning disable 8618
        private AccountingPoint()
#pragma warning restore 8618
        {
            // EF Core only
        }

        public AccountingPointId AccountingPointId { get; }

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

            var rules = new List<IBusinessRule>()
            {
                new MeteringPointMustBeEnergySuppliableRule(_meteringPointType),
                new ProductionMeteringPointMustBeObligatedRule(_meteringPointType, _isProductionObligated),
                new CannotBeInStateOfClosedDownRule(_physicalState),
                new MustHaveEnergySupplierAssociatedRule(GetCurrentSupplier(systemDateTimeProvider)),
                new ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRule(_businessProcesses.AsReadOnly(), supplyStartDate),
                new MoveInRegisteredOnSameDateIsNotAllowedRule(_businessProcesses.AsReadOnly(), supplyStartDate),
                // TODO: Ignore move out process until implementation is in scope
                //new MoveOutRegisteredOnSameDateIsNotAllowedRule(_businessProcesses.AsReadOnly(), supplyStartDate),
                new EffectuationDateCannotBeInThePastRule(supplyStartDate, systemDateTimeProvider.Now()),
            };

            return new BusinessRulesValidationResult(rules);
        }

        public void AcceptChangeOfSupplier(EnergySupplierId energySupplierId, Instant supplyStartDate, Transaction transaction, ISystemDateTimeProvider systemDateTimeProvider)
        {
            if (!ChangeSupplierAcceptable(energySupplierId, supplyStartDate, systemDateTimeProvider).Success)
            {
                throw new BusinessProcessException(
                    "Cannot accept change of supplier request due to violation of one or more business rules.");
            }

            var businessProcess = CreateBusinessProcess(transaction, supplyStartDate, BusinessProcessType.ChangeOfSupplier);
            _businessProcesses.Add(businessProcess);
            _supplierRegistrations.Add(new SupplierRegistration(energySupplierId, businessProcess.BusinessProcessId));

            AddDomainEvent(new EnergySupplierChangeRegistered(GsrnNumber, transaction, supplyStartDate));
        }

        public void EffectuateChangeOfSupplier(Transaction transaction, ISystemDateTimeProvider systemDateTimeProvider)
        {
            if (transaction is null) throw new ArgumentNullException(nameof(transaction));
            if (systemDateTimeProvider == null) throw new ArgumentNullException(nameof(systemDateTimeProvider));

            var businessProcess = GetBusinessProcess(transaction, BusinessProcessType.ChangeOfSupplier);
            businessProcess.Effectuate(systemDateTimeProvider);

            DiscontinueCurrentSupplier(businessProcess, systemDateTimeProvider);
            StartOfSupplyForFutureSupplier(businessProcess, systemDateTimeProvider);

            AddDomainEvent(new EnergySupplierChanged(GsrnNumber.Value, transaction, businessProcess.EffectiveDate));
        }

        public void CloseDown()
        {
            if (_physicalState != PhysicalState.ClosedDown)
            {
                _physicalState = PhysicalState.ClosedDown;
                AddDomainEvent(new MeteringPointClosedDown(GsrnNumber));
            }
        }

        public BusinessRulesValidationResult ConsumerMoveInAcceptable(ConsumerId consumerId, EnergySupplierId energySupplierId, Instant moveInDate, Transaction transaction)
        {
            var rules = new List<IBusinessRule>()
            {
                new MoveInRegisteredOnSameDateIsNotAllowedRule(_businessProcesses.AsReadOnly(), moveInDate),
            };

            return new BusinessRulesValidationResult(rules);
        }

        public void AcceptConsumerMoveIn(ConsumerId consumerId, EnergySupplierId energySupplierId, Instant moveInDate, Transaction transaction)
        {
            if (!ConsumerMoveInAcceptable(consumerId, energySupplierId, moveInDate, transaction).Success)
            {
                throw new BusinessProcessException(
                    "Cannot accept move in request due to violation of one or more business rules.");
            }

            var businessProcess = CreateBusinessProcess(transaction, moveInDate, BusinessProcessType.MoveIn);
            _businessProcesses.Add(businessProcess);
            _consumerRegistrations.Add(new ConsumerRegistration(consumerId, businessProcess.BusinessProcessId));
            _supplierRegistrations.Add(new SupplierRegistration(energySupplierId, businessProcess.BusinessProcessId));
        }

        public void EffectuateConsumerMoveIn(Transaction transaction, ISystemDateTimeProvider systemDateTimeProvider)
        {
            var businessProcess = GetBusinessProcess(transaction, BusinessProcessType.MoveIn);

            businessProcess.Effectuate(systemDateTimeProvider);
            var newSupplier = _supplierRegistrations.Find(supplier => supplier.BusinessProcessId.Equals(businessProcess.BusinessProcessId))!;
            newSupplier.StartOfSupply(businessProcess.EffectiveDate);
        }

        public void CancelChangeOfSupplier(Transaction transaction)
        {
            if (transaction is null) throw new ArgumentNullException(nameof(transaction));

            var businessProcess = GetBusinessProcess(transaction, BusinessProcessType.ChangeOfSupplier);
            businessProcess.Cancel();
            AddDomainEvent(new ChangeOfSupplierCancelled(transaction));
        }

        private void StartOfSupplyForFutureSupplier(BusinessProcess businessProcess, ISystemDateTimeProvider systemDateTimeProvider)
        {
            var futureSupplier = _supplierRegistrations.Find(s => s.BusinessProcessId.Equals(businessProcess.BusinessProcessId));
            if (futureSupplier == null)
            {
                throw new BusinessProcessException(
                    $"Could find supplier registration of process id {businessProcess.Transaction.Value}.");
            }

            futureSupplier.StartOfSupply(businessProcess.EffectiveDate);
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

        private SupplierRegistration GetCurrentSupplier(ISystemDateTimeProvider systemDateTimeProvider)
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
