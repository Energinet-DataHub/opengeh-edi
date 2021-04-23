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
using Energinet.DataHub.MarketData.Domain.BusinessProcesses;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses.Exceptions;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Rules.ChangeEnergySupplier;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketData.Domain.MeteringPoints
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

        private AccountingPoint(
            GsrnNumber gsrnNumber,
            MeteringPointType meteringPointType,
            bool isProductionObligated,
            Guid id,
            int version,
            PhysicalState physicalState,
            List<BusinessProcess> businessProcesses,
            List<ConsumerRegistration> consumerRegistrations,
            List<SupplierRegistration> supplierRegistrations)
        {
            GsrnNumber = gsrnNumber;
            _meteringPointType = meteringPointType;
            _physicalState = physicalState;
            _isProductionObligated = isProductionObligated;
            _businessProcesses = businessProcesses;
            _consumerRegistrations = consumerRegistrations;
            _supplierRegistrations = supplierRegistrations;
            Id = id;
            Version = version;
        }

        public GsrnNumber GsrnNumber { get; private set; }

        public static AccountingPoint CreateProduction(GsrnNumber gsrnNumber, bool isObligated)
        {
            return new AccountingPoint(gsrnNumber, MeteringPointType.Production, isObligated);
        }

        public static AccountingPoint CreateFrom(MeteringPointSnapshot snapshot)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return new AccountingPoint(
                GsrnNumber.Create(snapshot.GsrnNumber),
                EnumerationType.FromValue<MeteringPointType>(snapshot.MeteringPointType),
                snapshot.IsProductionObligated,
                snapshot.Id,
                snapshot.Version,
                EnumerationType.FromValue<PhysicalState>(snapshot.PhysicalState),
                snapshot.BusinessProcesses.Select(r => BusinessProcess.CreateFrom(r)).ToList(),
                snapshot.ConsumerRegistrations.Select(c => ConsumerRegistration.CreateFrom(c)).ToList(),
                snapshot.SupplierRegistrations.Select(s => SupplierRegistration.CreateFrom(s)).ToList());
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

        public void AcceptChangeOfSupplier(EnergySupplierId energySupplierId, Instant supplyStartDate, ProcessId processId, ISystemDateTimeProvider systemDateTimeProvider)
        {
            if (!ChangeSupplierAcceptable(energySupplierId, supplyStartDate, systemDateTimeProvider).Success)
            {
                throw new BusinessProcessException(
                    "Cannot accept change of supplier request due to violation of one or more business rules.");
            }

            AddBusinessProcess(processId, supplyStartDate, BusinessProcessType.ChangeOfSupplier);
            _supplierRegistrations.Add(new SupplierRegistration(energySupplierId, processId));

            AddDomainEvent(new EnergySupplierChangeRegistered(GsrnNumber, processId, supplyStartDate));
        }

        public void EffectuateChangeOfSupplier(ProcessId processId, ISystemDateTimeProvider systemDateTimeProvider)
        {
            if (processId is null) throw new ArgumentNullException(nameof(processId));
            if (systemDateTimeProvider == null) throw new ArgumentNullException(nameof(systemDateTimeProvider));

            var businessProcess = GetBusinessProcess(processId, BusinessProcessType.ChangeOfSupplier);
            businessProcess.Effectuate(systemDateTimeProvider);

            DiscontinueCurrentSupplier(businessProcess, systemDateTimeProvider);
            StartOfSupplyForFutureSupplier(businessProcess, systemDateTimeProvider);

            AddDomainEvent(new EnergySupplierChanged(GsrnNumber.Value, processId.Value!, businessProcess.EffectiveDate));
        }

        public void CloseDown()
        {
            if (_physicalState != PhysicalState.ClosedDown)
            {
                _physicalState = PhysicalState.ClosedDown;
                AddDomainEvent(new MeteringPointClosedDown(GsrnNumber));
            }
        }

        public MeteringPointSnapshot GetSnapshot()
        {
            var businessProcesses = _businessProcesses.Select(p => p.GetSnapshot()).ToList();
            var consumerRegistrations = _consumerRegistrations.Select(c => c.GetSnapshot()).ToList();
            var supplierRegistrations = _supplierRegistrations.Select(s => s.GetSnapshot()).ToList();

            return new MeteringPointSnapshot(
                Id,
                GsrnNumber.Value,
                _meteringPointType.Id,
                _isProductionObligated,
                _physicalState.Id,
                Version,
                businessProcesses,
                consumerRegistrations,
                supplierRegistrations);
        }

        public BusinessRulesValidationResult ConsumerMoveInAcceptable(ConsumerId consumerId, EnergySupplierId energySupplierId, Instant moveInDate, ProcessId processId)
        {
            var rules = new List<IBusinessRule>()
            {
                new MoveInRegisteredOnSameDateIsNotAllowedRule(_businessProcesses.AsReadOnly(), moveInDate),
            };

            return new BusinessRulesValidationResult(rules);
        }

        public void AcceptConsumerMoveIn(ConsumerId consumerId, EnergySupplierId energySupplierId, Instant moveInDate, ProcessId processId)
        {
            if (!ConsumerMoveInAcceptable(consumerId, energySupplierId, moveInDate, processId).Success)
            {
                throw new BusinessProcessException(
                    "Cannot accept move in request due to violation of one or more business rules.");
            }

            AddBusinessProcess(processId, moveInDate, BusinessProcessType.MoveIn);
            _consumerRegistrations.Add(new ConsumerRegistration(consumerId, processId));
            _supplierRegistrations.Add(new SupplierRegistration(energySupplierId, processId));
        }

        public void EffectuateConsumerMoveIn(ProcessId processId, ISystemDateTimeProvider systemDateTimeProvider)
        {
            var businessProcess = GetBusinessProcess(processId, BusinessProcessType.MoveIn);

            businessProcess.Effectuate(systemDateTimeProvider);
            var newSupplier = _supplierRegistrations.Find(supplier => supplier.ProcessId.Equals(processId)) !;
            newSupplier.StartOfSupply(businessProcess.EffectiveDate);
        }

        public void CancelChangeOfSupplier(ProcessId processId)
        {
            if (processId is null) throw new ArgumentNullException(nameof(processId));

            var businessProcess = GetBusinessProcess(processId, BusinessProcessType.ChangeOfSupplier);
            businessProcess.Cancel();
            AddDomainEvent(new ChangeOfSupplierCancelled(processId));
        }

        private void StartOfSupplyForFutureSupplier(BusinessProcess businessProcess, ISystemDateTimeProvider systemDateTimeProvider)
        {
            var futureSupplier = _supplierRegistrations.Find(s => s.ProcessId.Equals(businessProcess.ProcessId));
            if (futureSupplier == null)
            {
                throw new BusinessProcessException(
                    $"Could find supplier registration of process id {businessProcess.ProcessId.Value}.");
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

        private SupplierRegistration? GetCurrentSupplier(ISystemDateTimeProvider systemDateTimeProvider)
        {
            return _supplierRegistrations.Find(supplier =>
                supplier.StartOfSupplyDate <= systemDateTimeProvider.Now() && supplier.EndOfSupplyDate == null);
        }

        private BusinessProcess GetBusinessProcess(ProcessId processId, BusinessProcessType businessProcessType)
        {
            var businessProcess =
                _businessProcesses.Find(p => p.ProcessId.Equals(processId) && p.ProcessType == businessProcessType);
            if (businessProcess == null)
            {
                throw new BusinessProcessException($"Business process ({businessProcessType.Name}) {processId.ToString()} does not exist.");
            }

            return businessProcess;
        }

        private void AddBusinessProcess(ProcessId processId, Instant effectiveDate, BusinessProcessType businessProcessType)
        {
            if (_businessProcesses.Any(p => p.ProcessId.Equals(processId)))
            {
                throw new BusinessProcessException($"Process id {processId.Value} does already exist.");
            }

            _businessProcesses.Add(new BusinessProcess(processId, effectiveDate, businessProcessType));
        }
    }
}
