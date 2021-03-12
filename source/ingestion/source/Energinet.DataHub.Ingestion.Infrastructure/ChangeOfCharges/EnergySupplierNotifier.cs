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
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges.Repositories;
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Infrastructure.PostOffice;
using Energinet.DataHub.Ingestion.Infrastructure.PostOffice.Factories;

namespace Energinet.DataHub.Ingestion.Infrastructure.ChangeOfCharges
{
    public class EnergySupplierNotifier : IEnergySupplierNotifier
    {
        private readonly IMarketParticipantRepository _marketParticipantRepository;
        private readonly IPostOfficeClient _postOfficeClient;
        private readonly IPostOfficeDocumentFactory _postOfficeDocumentFactory;

        public EnergySupplierNotifier(IMarketParticipantRepository marketParticipantRepository, IPostOfficeClient postOfficeClient, IPostOfficeDocumentFactory postOfficeDocumentFactory)
        {
            _marketParticipantRepository = marketParticipantRepository;
            _postOfficeClient = postOfficeClient;
            _postOfficeDocumentFactory = postOfficeDocumentFactory;
        }

        public async Task NotifyAboutChangeOfChargesAsync([NotNull] ChangeOfChargesMessage changeOfChargesMessage)
        {
            if (changeOfChargesMessage == null) throw new ArgumentNullException(nameof(changeOfChargesMessage));

            var energySuppliers = _marketParticipantRepository.GetEnergySuppliers();
            var documents = _postOfficeDocumentFactory.Create(energySuppliers, changeOfChargesMessage);
            await _postOfficeClient.SendAsync(documents);
        }
    }
}
