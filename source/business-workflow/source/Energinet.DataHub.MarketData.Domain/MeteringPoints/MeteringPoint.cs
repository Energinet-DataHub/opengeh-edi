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
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Rules.ChangeEnergySupplier;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketData.Domain.MeteringPoints
{
    public sealed class MeteringPoint : AggregateRootBase
    {
        private readonly MeteringPointType _meteringPointType;
        private readonly List<Relationship> _relationships = new List<Relationship>();
        private readonly bool _isProductionObligated;
        private PhysicalState _physicalState;

        public MeteringPoint(GsrnNumber gsrnNumber, MeteringPointType meteringPointType)
        {
            GsrnNumber = gsrnNumber;
            _meteringPointType = meteringPointType;
            _physicalState = PhysicalState.New;
            AddDomainEvent(new MeteringPointCreated(GsrnNumber, _meteringPointType));
        }

        private MeteringPoint(GsrnNumber gsrnNumber, MeteringPointType meteringPointType, bool isProductionObligated)
            : this(gsrnNumber, meteringPointType)
        {
            _isProductionObligated = isProductionObligated;
        }

        private MeteringPoint(GsrnNumber gsrnNumber, MeteringPointType meteringPointType, bool isProductionObligated, List<Relationship> relationships, int id, int version, PhysicalState physicalState)
        {
            GsrnNumber = gsrnNumber;
            _meteringPointType = meteringPointType;
            _physicalState = physicalState;
            _isProductionObligated = isProductionObligated;
            _relationships = relationships;
            Id = id;
            Version = version;
        }

        public GsrnNumber GsrnNumber { get; private set; }

        public static MeteringPoint CreateProduction(GsrnNumber gsrnNumber, bool isObligated)
        {
            return new MeteringPoint(gsrnNumber, MeteringPointType.Production, isObligated);
        }

        public static MeteringPoint CreateFrom(MeteringPointSnapshot snapshot)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return new MeteringPoint(
                GsrnNumber.Create(snapshot.GsrnNumber),
                MeteringPointType.FromValue<MeteringPointType>(snapshot.MeteringPointType),
                snapshot.IsProductionObligated,
                snapshot.Relationships.Select(r => Relationship.CreateFrom(r)).ToList(),
                snapshot.Id,
                snapshot.Version,
                PhysicalState.FromValue<PhysicalState>(snapshot.PhysicalState));
        }

        public BusinessRulesValidationResult CanChangeSupplier(MarketParticipantMrid energySupplierMrid, Instant effectuationDate, ISystemDateTimeProvider systemDateTimeProvider)
        {
            if (energySupplierMrid is null)
            {
                throw new ArgumentNullException(nameof(energySupplierMrid));
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
                new MustHaveEnergySupplierAssociatedRule(_relationships.AsReadOnly()),
                new ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRule(_relationships.AsReadOnly(), effectuationDate),
                new MoveInRegisteredOnSameDateIsNotAllowedRule(_relationships.AsReadOnly(), effectuationDate),
                new MoveOutRegisteredOnSameDateIsNotAllowedRule(_relationships.AsReadOnly(), effectuationDate),
                new EffectuationDateCannotBeInThePastRule(effectuationDate, systemDateTimeProvider.Now()),
            };

            return new BusinessRulesValidationResult(rules);
        }

        public void RegisterChangeOfEnergySupplier(MarketParticipantMrid energySupplierMrid, Instant effectuationDate, ISystemDateTimeProvider systemDateTimeProvider)
        {
            if (!CanChangeSupplier(energySupplierMrid, effectuationDate, systemDateTimeProvider).Success)
            {
                throw new InvalidOperationException();
            }

            _relationships.Add(new Relationship(energySupplierMrid,  RelationshipType.EnergySupplier, effectuationDate));
            //TODO: Refactor along with new Comsumer/Supplier concepts
            AddDomainEvent(new EnergySupplierChangeRegistered(GsrnNumber, new ProcessId("TODO"), effectuationDate));
        }

        public void CloseDown()
        {
            if (_physicalState != PhysicalState.ClosedDown)
            {
                _physicalState = PhysicalState.ClosedDown;
                AddDomainEvent(new MeteringPointClosedDown(GsrnNumber));
            }
        }

        public void RegisterMoveIn(MarketParticipantMrid customerMrid, MarketParticipantMrid energySupplierMrid, Instant effectuationDate)
        {
            if (customerMrid is null)
            {
                throw new ArgumentNullException(nameof(customerMrid));
            }

            if (energySupplierMrid is null)
            {
                throw new ArgumentNullException(nameof(energySupplierMrid));
            }

            _relationships.Add(new Relationship(customerMrid, RelationshipType.Customer1, effectuationDate));
            _relationships.Add(new Relationship(energySupplierMrid, RelationshipType.EnergySupplier, effectuationDate));
        }

        public void ActivateMoveIn(MarketParticipantMrid customerMrid, MarketParticipantMrid energySupplierMrid)
        {
            if (customerMrid is null)
            {
                throw new ArgumentNullException(nameof(customerMrid));
            }

            if (energySupplierMrid is null)
            {
                throw new ArgumentNullException(nameof(energySupplierMrid));
            }

            var customerRelation = _relationships.First(r =>
                r.MarketParticipantMrid.Equals(customerMrid) && r.Type == RelationshipType.Customer1);
            customerRelation.Activate();

            var energySupplierRelation = _relationships.First(r =>
                r.MarketParticipantMrid.Equals(energySupplierMrid) && r.Type == RelationshipType.EnergySupplier);
            energySupplierRelation.Activate();
        }

        public void RegisterMoveOut(MarketParticipantMrid customerMrid, Instant effectuationDate)
        {
            if (customerMrid is null)
            {
                throw new ArgumentNullException(nameof(customerMrid));
            }

            _relationships.Add(new Relationship(customerMrid, RelationshipType.MoveOut, effectuationDate));
        }

        public MeteringPointSnapshot GetSnapshot()
        {
            var relationShips = _relationships.Select(r => r.GetSnapshot()).ToList();
            return new MeteringPointSnapshot(Id, GsrnNumber.Value, _meteringPointType.Id, relationShips, _isProductionObligated, _physicalState.Id, Version);
        }
    }
}
