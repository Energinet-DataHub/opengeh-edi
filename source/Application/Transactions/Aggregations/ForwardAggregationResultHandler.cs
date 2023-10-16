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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Energinet.DataHub.EDI.Application.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.Transactions;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Application.Transactions.Aggregations;

public class ForwardAggregationResultHandler : IRequestHandler<ForwardAggregationResult, Unit>
{
    private readonly IOutgoingMessageRepository _outgoingMessageRepository;
    private readonly ILogger<ForwardAggregationResultHandler> _logger;

    public ForwardAggregationResultHandler(IOutgoingMessageRepository outgoingMessageRepository, ILogger<ForwardAggregationResultHandler> logger)
    {
        _outgoingMessageRepository = outgoingMessageRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(ForwardAggregationResult request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sw = new Stopwatch();
        sw.Start();
        var message = AggregationResultMessageFactory.CreateMessage(request.Result, ProcessId.New());
        var connstring = Environment.GetEnvironmentVariable("Storage_Account_Container_Connection_String");

        var blobServiceClient = new BlobServiceClient(connstring);

        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("edi");

        var blobClient = containerClient.GetBlobClient(message.Id.ToString());

        using var memoryStream = new MemoryStream();
        using var streamWriter = new StreamWriter(memoryStream);
        await streamWriter.WriteAsync(message.MessageRecord).ConfigureAwait(false);
        await streamWriter.FlushAsync().ConfigureAwait(false);

        memoryStream.Position = 0;

        await blobClient.UploadAsync(memoryStream, cancellationToken).ConfigureAwait(false);

        message.MessageRecord = string.Empty;

        _outgoingMessageRepository.Add(message);
        sw.Stop();
        _logger.LogInformation($"ForwardAggregationResultHandler took {sw.ElapsedMilliseconds} ms");
        return Unit.Value;
    }
}
