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

using System.Collections.Generic;
using Messaging.Application.IncomingMessages;
using Messaging.CimMessageAdapter.Errors;

namespace Messaging.CimMessageAdapter.Messages
{
    public class MessageParserResult<TMarketActivityRecordType, TMarketTransactionType>
        where TMarketActivityRecordType : IMarketActivityRecord
        where TMarketTransactionType : IMarketTransaction<TMarketActivityRecordType>
    {
        public MessageParserResult(params ValidationError[] errors)
        {
            Errors = errors;
        }

        public MessageParserResult(
            IMarketDocument<TMarketActivityRecordType, TMarketTransactionType> marketDocument)
        {
            MarketDocument = marketDocument;
        }

        public IReadOnlyCollection<ValidationError> Errors { get; } = new List<ValidationError>();

        public bool Success => Errors.Count == 0;

        public IMarketDocument<TMarketActivityRecordType, TMarketTransactionType>? MarketDocument { get; }
    }
}
