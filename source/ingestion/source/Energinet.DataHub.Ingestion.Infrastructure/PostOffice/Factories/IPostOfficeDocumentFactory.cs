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
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Domain.Common;
using Energinet.DataHub.Ingestion.Infrastructure.PostOffice.Dtos.ChangeOfCharges;

namespace Energinet.DataHub.Ingestion.Infrastructure.PostOffice.Factories
{
    /// <summary>
    /// Factory for creating post office documents.
    /// </summary>
    public interface IPostOfficeDocumentFactory
    {
        /// <summary>
        /// Create post office document from message.
        /// </summary>
        /// <param name="energySuppliers"></param>
        /// <param name="message"></param>
        /// <returns>A Post Office document.</returns>
        IEnumerable<ChargeChangeNotificationDocument> Create(IEnumerable<MarketParticipant> energySuppliers, ChangeOfChargesMessage message);
    }
}
