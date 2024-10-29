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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Response;

public class ResponseFactory(IEnumerable<IResponseFactory> factories)
{
    private readonly IEnumerable<IResponseFactory> _factories = factories;

    public ResponseMessage From(Result result, DocumentFormat format)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(format);

        var factory = _factories.FirstOrDefault(factory => factory.HandledFormat.Equals(format))
                      ?? throw new InvalidOperationException(
                          $"Could not generate response message in format {format.Name}");

        return factory.From(result);
    }
}
