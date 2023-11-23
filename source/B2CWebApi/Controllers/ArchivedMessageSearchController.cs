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

using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.Common.Serialization;
using Microsoft.AspNetCore.Mvc;
using NodaTime;

namespace Energinet.DataHub.EDI.B2CWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ArchivedMessageSearchController : ControllerBase
{
    private readonly ISerializer _serializer;
    private readonly IArchivedMessagesClient _archivedMessagesClient;

    public ArchivedMessageSearchController(
        ISerializer serializer,
        IArchivedMessagesClient archivedMessagesClient)
    {
        _serializer = serializer;
        _archivedMessagesClient = archivedMessagesClient;
    }

    [HttpPost]
    public async Task<ActionResult> RequestAsync(SearchArchivedMessagesCriteria request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var query = new GetMessagesQuery
        {
            CreationPeriod = request.CreatedDuringPeriod is not null
                ? new EDI.ArchivedMessages.Interfaces.MessageCreationPeriod(
                    request.CreatedDuringPeriod.Start,
                    request.CreatedDuringPeriod.End)
                : null,
            MessageId = request.MessageId,
            SenderNumber = request.SenderNumber,
            ReceiverNumber = request.ReceiverNumber,
            DocumentTypes = request.DocumentTypes,
            BusinessReasons = request.BusinessReasons,
        };
        var result = await _archivedMessagesClient.SearchAsync(query, cancellationToken).ConfigureAwait(false);

        return Ok(_serializer.Serialize(result.Messages));
    }
}

[Serializable]
public record SearchArchivedMessagesCriteria(
    MessageCreationPeriod? CreatedDuringPeriod,
    string? MessageId,
    string? SenderNumber,
    string? ReceiverNumber,
    IReadOnlyList<string>? DocumentTypes,
    IReadOnlyList<string>? BusinessReasons);

[Serializable]
public record MessageCreationPeriod(Instant Start, Instant End);
