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
using RequestMoveIn = Energinet.DataHub.MarketRoles.Application.MoveIn.RequestMoveIn;

namespace Energinet.DataHub.MarketRoles.Infrastructure.Ingestion.Mappers
{
    public class RequestMoveInMapper : ProtobufOutboundMapper<RequestMoveIn>
    {
        protected override IMessage Convert(RequestMoveIn obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            return new MarketRolesEnvelope
            {
                RequestMoveIn = new Contracts.RequestMoveIn
                {
                    TransactionId = obj.TransactionId,
                    EnergySupplierGlnNumber = obj.EnergySupplierGlnNumber,
                    SocialSecurityNumber = obj.SocialSecurityNumber,
                    VatNumber = obj.VATNumber,
                    ConsumerName = obj.ConsumerName,
                    AccountingPointGsrnNumber = obj.AccountingPointGsrnNumber,
                    MoveInDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(DateTimeOffset.Parse(obj.MoveInDate, CultureInfo.InvariantCulture)),
                },
            };
        }
    }
}
