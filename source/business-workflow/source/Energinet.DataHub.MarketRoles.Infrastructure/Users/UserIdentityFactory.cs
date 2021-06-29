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
using System.Linq;
using Energinet.DataHub.MarketRoles.Application.Common.Users;

namespace Energinet.DataHub.MarketRoles.Infrastructure.Users
{
    #pragma warning disable CA1822 // Could be static, but we keep it non-static for now.
    public class UserIdentityFactory
    {
        public UserIdentity FromDictionaryString(string inputText, string propertyKey)
        {
            if (string.IsNullOrWhiteSpace(inputText)) throw new ArgumentNullException(nameof(inputText));
            if (string.IsNullOrWhiteSpace(propertyKey)) throw new ArgumentNullException(nameof(propertyKey));

            var inputJsonDocument = System.Text.Json.JsonDocument.Parse(inputText);
            var resultJsonProperty = inputJsonDocument.RootElement
                .EnumerateObject()
                .FirstOrDefault(e => e.Name.Equals(propertyKey, StringComparison.OrdinalIgnoreCase));

            return FromString(resultJsonProperty.Value.ToString() ?? string.Empty);
        }

        public UserIdentity FromString(string userIdentity)
        {
            if (string.IsNullOrWhiteSpace(userIdentity)) throw new ArgumentNullException(nameof(userIdentity));
            return System.Text.Json.JsonSerializer.Deserialize<UserIdentity>(userIdentity) ?? throw new System.Text.Json.JsonException(nameof(userIdentity));
        }
    }
}
