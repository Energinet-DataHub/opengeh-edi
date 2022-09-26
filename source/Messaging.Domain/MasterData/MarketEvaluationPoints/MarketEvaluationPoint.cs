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

namespace Messaging.Domain.MasterData.MarketEvaluationPoints;

public class MarketEvaluationPoint
{
    private MarketEvaluationPoint(string marketEvaluationPointNumber, string energySupplierNumber)
    {
        MarketEvaluationPointNumber = marketEvaluationPointNumber;
        EnergySupplierNumber = energySupplierNumber;
        Id = Guid.NewGuid();
    }

    private MarketEvaluationPoint(string marketEvaluationPointNumber, Guid gridOperatorId)
    {
        MarketEvaluationPointNumber = marketEvaluationPointNumber;
        GridOperatorId = gridOperatorId;
        Id = Guid.NewGuid();
    }

    public Guid Id { get; }

    public string MarketEvaluationPointNumber { get; }

    public string? EnergySupplierNumber { get; private set; }

    public Guid? GridOperatorId { get; private set; }

    public static MarketEvaluationPoint Create(string energySupplierNumber, string accountingPointNumber)
    {
        return new MarketEvaluationPoint(accountingPointNumber, energySupplierNumber);
    }

    public static MarketEvaluationPoint Create(Guid gridOperatorId, string accountingPointNumber)
    {
        return new MarketEvaluationPoint(accountingPointNumber, gridOperatorId);
    }

    public void SetEnergySupplier(string energySupplierNumber)
    {
        EnergySupplierNumber = energySupplierNumber;
    }

    public void SetGridOperatorId(Guid gridOperatorId)
    {
        GridOperatorId = gridOperatorId;
    }
}
