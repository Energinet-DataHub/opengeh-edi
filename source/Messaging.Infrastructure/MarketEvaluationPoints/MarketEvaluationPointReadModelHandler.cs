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
using System.Threading.Tasks;
using Energinet.DataHub.EnergySupplying.IntegrationEvents;
using Energinet.DataHub.MeteringPoints.IntegrationEvents.CreateMeteringPoint;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Messaging.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Messaging.Infrastructure.MarketEvaluationPoints;

public class MarketEvaluationPointReadModelHandler
{
    private readonly B2BContext _context;

    public MarketEvaluationPointReadModelHandler(B2BContext context)
    {
        _context = context;
    }

    public async Task WhenAsync(MeteringPointCreated @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var marketEvaluationPoint =
            await GetOrCreateAsync(Guid.Parse(@event.Id), @event.GsrnNumber).ConfigureAwait(false);

        marketEvaluationPoint.SetGridOperatorId(Guid.Parse(@event.GridOperatorId));
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task WhenAsync(EnergySupplierChanged @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var marketEvaluationPoint = await _context
            .MarketEvaluationPoints
            .FirstOrDefaultAsync(e => e.Id.Equals(@event.AccountingpointId))
            .ConfigureAwait(false);

        if (marketEvaluationPoint == null)
        {
            marketEvaluationPoint = new MarketEvaluationPoint(Guid.Parse(@event.Id), @event.GsrnNumber);
            _context.MarketEvaluationPoints.Add(marketEvaluationPoint);
        }

        marketEvaluationPoint.SetEnergySupplier(@event.EnergySupplierGln);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    private async Task<MarketEvaluationPoint> GetOrCreateAsync(Guid id, string marketEvaluationPointNumber)
    {
        var marketEvaluationPoint = await _context
            .MarketEvaluationPoints
            .FirstOrDefaultAsync(e => e.Id.Equals(id))
            .ConfigureAwait(false);

        if (marketEvaluationPoint == null)
        {
            marketEvaluationPoint = new MarketEvaluationPoint(id, marketEvaluationPointNumber);
            _context.MarketEvaluationPoints.Add(marketEvaluationPoint);
        }

        return marketEvaluationPoint;
    }
}
