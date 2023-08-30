﻿// Copyright 2020 Energinet DataHub A/S
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

using AcceptanceTest.Drivers;

namespace AcceptanceTest.Dsl;

internal sealed class AggregatedMeasureDataDsl
{
    private readonly EdiDriver _edi;
    private readonly WholeSaleDriver _wholesale;

#pragma warning disable CA1822
#pragma warning disable VSTHRD200 // Since this is a DSL we don't want to suffix tasks with 'Async' since it is not part of the ubiquitous language

    internal AggregatedMeasureDataDsl(EdiDriver ediDriver, WholeSaleDriver wholesaleDriver)
    {
        _edi = ediDriver;
        _wholesale = wholesaleDriver;
    }

    internal Task SendAggregatedMeasureDataToInbox()
    {
        return _wholesale.SendAggregatedMeasureDataAcceptedToInboxAsync();
    }

    internal Task ConfirmResultIsAvailableFor(string actorNumber, string actorRole)
    {
        return _edi.PeekMessageAsync(actorNumber, new[] { actorRole, });
    }
}
