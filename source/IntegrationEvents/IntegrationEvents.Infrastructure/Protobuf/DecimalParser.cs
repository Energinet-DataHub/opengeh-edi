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
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Protobuf;

/// <summary>
/// This implementation is taken from https://learn.microsoft.com/en-us/dotnet/architecture/grpc-for-wcf-developers/protobuf-data-types#decimals
/// </summary>
public static class DecimalParser
{
    public static decimal Parse(DecimalValue input)
    {
        ArgumentNullException.ThrowIfNull(input);
        const decimal nanoFactor = 1_000_000_000;
        return input.Units + (input.Nanos / nanoFactor);
    }
}
