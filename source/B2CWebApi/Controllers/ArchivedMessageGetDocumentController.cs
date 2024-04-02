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

using Asp.Versioning;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.EDI.B2CWebApi.Controllers;

[ApiVersion(1.0)]
[ApiController]
[Route("[controller]")]
public class ArchivedMessageGetDocumentController : ControllerBase
{
    private readonly IArchivedMessagesClient _archivedMessagesClient;

    public ArchivedMessageGetDocumentController(IArchivedMessagesClient archivedMessagesClient)
    {
        _archivedMessagesClient = archivedMessagesClient;
    }

    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [Produces("text/plain")]
    public async Task<ActionResult> RequestAsync(Guid id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);

        var archivedMessageId = new ArchivedMessageId(id);
        var archivedMessageStream = await _archivedMessagesClient.GetAsync(archivedMessageId, cancellationToken).ConfigureAwait(false);

        return archivedMessageStream is null ? NoContent() : Ok(archivedMessageStream.Stream);
    }
}
