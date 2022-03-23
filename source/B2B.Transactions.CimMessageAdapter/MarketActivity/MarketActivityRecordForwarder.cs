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
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;

namespace B2B.CimMessageAdapter.MarketActivity
{
    public class MarketActivityRecordForwarder : IMarketActivityRecordForwarder, IDisposable, IAsyncDisposable
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly string _connectionString = "<Connection String>";
        private readonly string _queueName = "<Queue name";
        private bool _disposed;
        private TransactionScope? _transactionScope;
        private ServiceBusSender _serviceBusSender;

        public MarketActivityRecordForwarder(IJsonSerializer jsonSerializer, ServiceBusSender sender)
        {
            _transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            _serviceBusSender = sender
            _jsonSerializer = jsonSerializer;
        }

        public async Task AddAsync(MarketActivityRecord marketActivityRecord)
        {
            var message = CreateMessage(marketActivityRecord);
            if (_serviceBusSender != null) await _serviceBusSender.SendMessageAsync(message).ConfigureAwait(false);
        }

        public async Task CommitAsync()
        {
            await Task.Run(() => _transactionScope?.Complete()).ConfigureAwait(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            if (_client is not null)
            {
                await _client.DisposeAsync().ConfigureAwait(false);
            }

            if (_serviceBusSender is not null)
            {
                await _serviceBusSender.DisposeAsync().ConfigureAwait(false);
            }

            _client = null;
            _serviceBusSender = null;

            Dispose(false);

#pragma warning disable CA1816 // Dispose should call SurpressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transactionScope?.Dispose();
                    _transactionScope = null;
                }

                _disposed = true;
            }
        }

        private ServiceBusMessage CreateMessage(MarketActivityRecord item)
        {
            var json = _jsonSerializer.Serialize(item);
            var data = Encoding.UTF8.GetBytes(json);
            var message = new ServiceBusMessage(data);
            return message;
        }
    }
}
