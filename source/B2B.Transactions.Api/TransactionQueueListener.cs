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
using System.Text;
using System.Threading.Tasks;
using B2B.Transactions.Api.Servicebus;
using B2B.Transactions.Infrastructure.Configuration.Correlation;
using B2B.Transactions.Infrastructure.Serialization;
using B2B.Transactions.Transactions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace B2B.Transactions.Api
{
    public class TransactionQueueListener
    {
        private readonly ILogger _logger;
        private readonly ICorrelationContext _correlationContext;
        private readonly RegisterTransaction _registerTransaction;
        private readonly ISerializer _jsonSerializer;
        private readonly ServiceBusMessageMetadataExtractor _serviceBusMessageMetadataExtractor;

        public TransactionQueueListener(
            ILogger logger,
            ICorrelationContext correlationContext,
            RegisterTransaction registerTransaction,
            ISerializer jsonSerializer,
            ServiceBusMessageMetadataExtractor serviceBusMessageMetadataExtractor)
        {
            _logger = logger;
            _correlationContext = correlationContext;
            _registerTransaction = registerTransaction;
            _jsonSerializer = jsonSerializer;
            _serviceBusMessageMetadataExtractor = serviceBusMessageMetadataExtractor;
        }

        [Function(nameof(TransactionQueueListener))]
        public async Task RunAsync(
            [ServiceBusTrigger("%TRANSACTIONS_QUEUE_NAME%", Connection = "TRANSACTIONS_QUEUE_LISTENER_CONNECTION_STRING")] byte[] data,
            FunctionContext context)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (context == null) throw new ArgumentNullException(nameof(context));

            _serviceBusMessageMetadataExtractor.SetCorrelationId(context);

            var byteAsString = Encoding.UTF8.GetString(data);

            await _registerTransaction.HandleAsync(
                    _jsonSerializer.Deserialize<B2BTransaction>(byteAsString))
                .ConfigureAwait(false);

            _logger.LogInformation("B2B transaction dequeued with correlation id: {correlationId}", _correlationContext.Id);
        }
    }
}
