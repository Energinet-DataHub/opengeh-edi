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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Messaging.Application.Common;
using Processing.Domain.SeedWork;

namespace Messaging.Application.OutgoingMessages.RejectRequestChangeOfSupplier
{
    public class RejectRequestChangeOfSupplierMessageFactory
    {
        private readonly IMarketActivityRecordParser _marketActivityRecordParser;

        public RejectRequestChangeOfSupplierMessageFactory(IMarketActivityRecordParser marketActivityRecordParser)
        {
            _marketActivityRecordParser = marketActivityRecordParser;
        }

        public Task<Stream> CreateFromAsync(MessageHeader messageHeader, IReadOnlyCollection<string> marketActivityPayloads)
        {
            if (messageHeader == null) throw new ArgumentNullException(nameof(messageHeader));
            if (marketActivityPayloads == null) throw new ArgumentNullException(nameof(marketActivityPayloads));
            var documentWriter = new RejectRequestChangeOfSupplierDocumentWriter();
            return documentWriter.WriteAsync(messageHeader, GetMarketActivityRecordsFrom(marketActivityPayloads));
        }

        private List<MarketActivityRecord> GetMarketActivityRecordsFrom(IReadOnlyCollection<string> marketActivityPayloads)
        {
            return marketActivityPayloads
                .Select(payload => _marketActivityRecordParser.From<MarketActivityRecord>(payload))
                .ToList();
        }
    }
}
