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
using System.Threading.Tasks;
using Messaging.Application.OutgoingMessages.Peek;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages.Peek;

namespace Messaging.IntegrationTests.TestDoubles;

public class BundleStoreStub : IBundleStore
{
    private readonly Dictionary<string, Stream?> _documents = new();

    public Stream? GetBundleOf(
        MessageCategory messageCategory,
        ActorNumber messageReceiverNumber,
        MarketRole roleOfReceiver)
    {
        return _documents.SingleOrDefault(m => m.Key == GenerateKey(messageCategory, messageReceiverNumber, roleOfReceiver)).Value;
    }

    public void SetBundleFor(
        string key,
        MessageCategory messageCategory,
        ActorNumber messageReceiverNumber,
        MarketRole roleOfReceiver,
        Stream document)
    {
        ArgumentNullException.ThrowIfNull(messageCategory);
        ArgumentNullException.ThrowIfNull(messageReceiverNumber);
        ArgumentNullException.ThrowIfNull(roleOfReceiver);
        _documents[GenerateKey(messageCategory, messageReceiverNumber, roleOfReceiver)] = document;
    }

    public Task<bool> TryRegisterBundleAsync(
        MessageCategory messageCategory,
        ActorNumber messageReceiverNumber,
        MarketRole roleOfReceiver)
    {
        ArgumentNullException.ThrowIfNull(messageCategory);
        ArgumentNullException.ThrowIfNull(messageReceiverNumber);
        ArgumentNullException.ThrowIfNull(roleOfReceiver);
        try
        {
            _documents.Add(GenerateKey(messageCategory, messageReceiverNumber, roleOfReceiver), null);
            return Task.FromResult(true);
        }
        catch (ArgumentException)
        {
            return Task.FromResult(false);
        }
    }

    private static string GenerateKey(
        MessageCategory messageCategory,
        ActorNumber messageReceiverNumber,
        MarketRole roleOfReceiver)
    {
        return messageCategory.Name + messageReceiverNumber.Value + roleOfReceiver.Name;
    }
}
