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
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Messaging.Domain.MasterData.MarketEvaluationPoints;

namespace Messaging.Application.MasterData.MarketEvaluationPoints;

public class CreateMarketEvaluationPointHandler : IRequestHandler<CreateMarketEvalationPoint, Unit>
{
    private readonly IMarketEvaluationPointRepository _marketEvaluationPoints;

    public CreateMarketEvaluationPointHandler(IMarketEvaluationPointRepository marketEvaluationPoints)
    {
        _marketEvaluationPoints = marketEvaluationPoints;
    }

    public async Task<Unit> Handle(CreateMarketEvalationPoint request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var marketEvaluationPoint = await _marketEvaluationPoints
            .GetByNumberAsync(request.MarketEvaluationPointNumber)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(request.EnergySupplierNumber))
        {
            HandleGridOperatorId(request, marketEvaluationPoint);
        }
        else
        {
            HandleEnergySupplierNumber(request, marketEvaluationPoint);
        }

        return Unit.Value;
    }

    private void HandleGridOperatorId(CreateMarketEvalationPoint request, MarketEvaluationPoint? marketEvaluationPoint)
    {
        if (marketEvaluationPoint is null)
        {
            marketEvaluationPoint = MarketEvaluationPoint.Create(
                Guid.Parse(request.GridOperatorId),
                request.MarketEvaluationPointNumber);
            _marketEvaluationPoints.Add(marketEvaluationPoint);
        }
        else
        {
            marketEvaluationPoint.SetGridOperatorId(Guid.Parse(request.GridOperatorId));
        }
    }

    private void HandleEnergySupplierNumber(
        CreateMarketEvalationPoint request,
        MarketEvaluationPoint? marketEvaluationPoint)
    {
        if (marketEvaluationPoint is null)
        {
            marketEvaluationPoint = MarketEvaluationPoint.Create(
                request.EnergySupplierNumber,
                request.MarketEvaluationPointNumber);
            _marketEvaluationPoints.Add(marketEvaluationPoint);
        }
        else
        {
            marketEvaluationPoint.SetEnergySupplier(request.EnergySupplierNumber);
        }
    }
}
