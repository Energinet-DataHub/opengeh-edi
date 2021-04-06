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
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process.Commands;

namespace Energinet.DataHub.MarketData.Infrastructure.InternalCommand
{
    public static class MessageTypeFactory
    {
        public static Type GetType(string type)
        {
            switch (type)
            {
                case nameof(ChangeSupplier):
                    return typeof(ChangeSupplier);
                case nameof(NotifyCurrentSupplier):
                    return typeof(NotifyCurrentSupplier);
                case nameof(NotifyGridOperator):
                    return typeof(NotifyGridOperator);
                case nameof(SendConfirmationMessage):
                    return typeof(SendConfirmationMessage);
                case nameof(SendConsumerDetails):
                    return typeof(SendConsumerDetails);
                case nameof(SendMeteringPointDetails):
                    return typeof(SendMeteringPointDetails);
                default: throw new ArgumentException();
            }
        }
    }
}
