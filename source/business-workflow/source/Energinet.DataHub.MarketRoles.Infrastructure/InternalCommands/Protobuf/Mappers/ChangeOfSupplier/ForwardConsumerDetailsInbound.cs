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
using Energinet.DataHub.MarketRoles.Application.Common.Transport;
using Energinet.DataHub.MarketRoles.Contracts;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf;

namespace Energinet.DataHub.MarketRoles.Infrastructure.InternalCommands.Protobuf.Mappers.ChangeOfSupplier
{
    public class ForwardConsumerDetailsInbound : ProtobufInboundMapper<MarketRoles.Contracts.ForwardConsumerDetails>
    {
        protected override IInboundMessage Convert(ForwardConsumerDetails obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return new Application.ChangeOfSupplier.Processing.ConsumerDetails.ForwardConsumerDetails(
                Guid.Parse(obj.AccountingPointId),
                Guid.Parse(obj.BusinessProcessId),
                obj.Transaction);
        }
    }
}
