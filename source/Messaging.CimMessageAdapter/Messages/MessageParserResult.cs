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
    public class MessageParserResult<TMarketActivityRecordType>
     where TMarketActivityRecordType : IMarketActivityRecord
    {
        public MessageParserResult(params ValidationError[] errors)
        {
            Errors = errors;
        }

        public MessageParserResult(MessageHeader messageHeader, IReadOnlyCollection<TMarketActivityRecordType> marketActivityRecords)
        {
            MessageHeader = messageHeader;
            MarketActivityRecords = marketActivityRecords;
        }

        public IReadOnlyCollection<ValidationError> Errors { get; } = new List<ValidationError>();

        public bool Success => Errors.Count == 0;

        public MessageHeader? MessageHeader { get; }

        public IReadOnlyCollection<TMarketActivityRecordType> MarketActivityRecords { get; } =
            new List<TMarketActivityRecordType>();
        // public static MessageParserResult Failure(params ValidationError[] errors)
        // {
        //     return new MessageParserResult(errors);
        // }
        //
        // public static MessageParserResult Succeeded(MessageHeader messageHeader, IReadOnlyCollection<MarketActivityRecord> marketActivityRecords)
        // {
        //     return new MessageParserResult(messageHeader, marketActivityRecords);
        // }
    }

    // #pragma warning disable
    // public class MessageParserResult<TMarketActivityRecordType>
    // {
    //     private MessageParserResult(MessageHeader header, IReadOnlyCollection<TMarketActivityRecordType> marketActivityRecords)
    //     {
    //         Header = header;
    //         MarketActivityRecords = marketActivityRecords;
    //     }
    //
    //     public MessageHeader Header { get; }
    //
    //     public IReadOnlyCollection<TMarketActivityRecordType> MarketActivityRecords { get; }
    //
    //
    //     public MessageParserResult<TMarketActivityRecordType> Succeeded(MessageHeader header, IReadOnlyCollection<TMarketActivityRecordType> marketActivityRecords)
    //     {
    //         return new MessageParserResult<TMarketActivityRecordType>(header, marketActivityRecords);
    //     }
    // }
}
