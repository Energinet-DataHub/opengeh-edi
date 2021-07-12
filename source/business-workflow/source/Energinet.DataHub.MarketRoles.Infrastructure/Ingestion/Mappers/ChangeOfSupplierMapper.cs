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
using System.Globalization;
using Energinet.DataHub.MarketRoles.Contracts;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf;
using Google.Protobuf;
using RequestChangeOfSupplier = Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.RequestChangeOfSupplier;

namespace Energinet.DataHub.MarketRoles.Infrastructure.Ingestion.Mappers
{
    public class ChangeOfSupplierMapper : ProtobufOutboundMapper<RequestChangeOfSupplier>
    {
        protected override IMessage Convert(RequestChangeOfSupplier obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            return new MarketRolesEnvelope
            {
                RequestChangeOfSupplier = new Contracts.RequestChangeOfSupplier
                {
                    TransactionId = obj.TransactionId,
                    EnergySupplierGlnNumber = obj.EnergySupplierGlnNumber,
                    SocialSecurityNumber = obj.SocialSecurityNumber,
                    VATNumber = obj.VATNumber,
                    AccountingPointGsrnNumber = obj.AccountingPointGsrnNumber,
                    StartDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(DateTimeOffset.Parse(obj.StartDate, CultureInfo.InvariantCulture)),
                },
            };
        }
    }
}
