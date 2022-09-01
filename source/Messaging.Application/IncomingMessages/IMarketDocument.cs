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
using System.Collections.ObjectModel;

namespace Messaging.Application.IncomingMessages;

/// <summary>
/// Incoming EDI market document
/// </summary>
/// <typeparam name="TMarketActivityRecordType">Type of market activity record</typeparam>
/// <typeparam name="TMarketTransactionType">Type that represent a single market transaction. Contains the header and a single market activity record</typeparam>
public interface IMarketDocument<out TMarketActivityRecordType, TMarketTransactionType>
    where TMarketActivityRecordType : IMarketActivityRecord
    where TMarketTransactionType : IMarketTransaction<TMarketActivityRecordType>
{
    /// <summary>
    /// Contains message metadata
    /// </summary>
    MessageHeader Header { get; }

    /// <summary>
    /// List of market activity records (market transaction)
    /// </summary>
    IReadOnlyCollection<TMarketActivityRecordType> MarketActivityRecords { get; }

    /// <summary>
    /// Return a list of market transactions
    /// </summary>
    ReadOnlyCollection<TMarketTransactionType> ToTransactions();
}
