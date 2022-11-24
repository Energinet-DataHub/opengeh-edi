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

namespace Messaging.IntegrationTests.Factories;

internal static class ConsumerFactory
{
    #pragma warning disable CA5394 //Accept insecure Random call
    internal static string CreateConsumerId(bool isSocialSecurityNumber = true)
    {
        var random = new Random();
        if (isSocialSecurityNumber)
        {
            return random.Next(111111111, 999999999).ToString(CultureInfo.InvariantCulture);
        }

        return random.Next(11111111, 99999999).ToString(CultureInfo.InvariantCulture);
    }
    #pragma warning restore

    internal static string CreateConsumerName()
    {
        return Guid.NewGuid().ToString();
    }
}
