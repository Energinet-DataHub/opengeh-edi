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
using B2B.CimMessageAdapter.Errors;

namespace B2B.CimMessageAdapter.Messages
{
    public class MessageParserResult
    {
        private MessageParserResult(IReadOnlyCollection<ValidationError> errors)
        {
            Errors = errors;
        }

        private MessageParserResult(MessageHeader messageHeader, IReadOnlyCollection<MarketActivityRecord> marketActivityRecords)
        {
            MessageHeader = messageHeader;
            MarketActivityRecords = marketActivityRecords;
        }

        public IReadOnlyCollection<ValidationError> Errors { get; } = new List<ValidationError>();

        public bool Success => Errors.Count == 0;

        public MessageHeader? MessageHeader { get; }

        public IReadOnlyCollection<MarketActivityRecord> MarketActivityRecords { get; } =
            new List<MarketActivityRecord>();

        public static MessageParserResult Failure(params ValidationError[] errors)
        {
            return new MessageParserResult(errors);
        }

        public static MessageParserResult Succeeded(MessageHeader messageHeader, IReadOnlyCollection<MarketActivityRecord> marketActivityRecords)
        {
            return new MessageParserResult(messageHeader, marketActivityRecords);
        }
    }
}
