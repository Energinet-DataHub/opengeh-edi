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

using Energinet.DataHub.EDI.ArchivedMessages.Application.Mapping;
using Energinet.DataHub.EDI.ArchivedMessages.Domain;
using Energinet.DataHub.EDI.ArchivedMessages.Domain.Models;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.ArchivedMessages.Application;

public class ArchivedMessagesClient(
    IArchivedMessageRepository archivedMessageRepository,
    IMeteringPointArchivedMessageRepository meteringPointArchivedMessageRepository) : IArchivedMessagesClient
{
    private readonly IArchivedMessageRepository _archivedMessageRepository = archivedMessageRepository;
    private readonly IMeteringPointArchivedMessageRepository _meteringPointArchivedMessageRepository = meteringPointArchivedMessageRepository;

    private readonly IReadOnlyCollection<DocumentType> _meteringPointDocumentTypes = new[]
    {
        DocumentType.NotifyValidatedMeasureData,
        DocumentType.Acknowledgement,
        DocumentType.ReminderOfMissingMeasureData,
    };

    public async Task<IArchivedFile> CreateAsync(ArchivedMessageDto message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (IsMeteringPointMessage(message.DocumentType))
        {
            var meteringPointArchivedMessage = MeteringPointArchivedMessageMapper.Map(message);
            await _meteringPointArchivedMessageRepository.AddAsync(meteringPointArchivedMessage, cancellationToken).ConfigureAwait(false);
            return new ArchivedFile(meteringPointArchivedMessage.FileStorageReference, meteringPointArchivedMessage.ArchivedMessageStream);
        }

        var mappedArchivedMessage = ArchivedMessageMapper.Map(message);
        await _archivedMessageRepository.AddAsync(mappedArchivedMessage, cancellationToken).ConfigureAwait(false);
        return new ArchivedFile(mappedArchivedMessage.FileStorageReference, mappedArchivedMessage.ArchivedMessageStream);
    }

    public async Task<ArchivedMessageStreamDto?> GetAsync(ArchivedMessageIdDto id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);

        var archivedMessageId = new ArchivedMessageId(id.Value);

        var tasks = new[]
        {
            _archivedMessageRepository.GetAsync(archivedMessageId, cancellationToken),
            _meteringPointArchivedMessageRepository.GetAsync(archivedMessageId, cancellationToken),
        };

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        var result = results.FirstOrDefault(r => r != null);

        return result != null ? new ArchivedMessageStreamDto(result.Stream) : null;
    }

    public async Task<MessageSearchResultDto> SearchAsync(GetMessagesQueryDto queryInputDto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryInputDto);
        var result = await _archivedMessageRepository.SearchAsync(GetMessagesQueryMapper.Map(queryInputDto), cancellationToken).ConfigureAwait(false);

        return MessagesSearchResultMapper.Map(result);
    }

    public async Task<MessageSearchResultDto> SearchAsync(GetMeteringPointMessagesQueryDto queryInputDto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryInputDto);
        var result = await _meteringPointArchivedMessageRepository.SearchAsync(GetMeteringPointMessagesQueryMapper.Map(queryInputDto), cancellationToken).ConfigureAwait(false);

        return MessagesSearchResultMapper.Map(result);
    }

    private bool IsMeteringPointMessage(DocumentType messageDocumentType)
    {
        return _meteringPointDocumentTypes
            .Any(x => x == messageDocumentType);
    }
}
