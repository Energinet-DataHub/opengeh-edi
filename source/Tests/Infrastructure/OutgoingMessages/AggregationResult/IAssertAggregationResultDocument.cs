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

namespace Tests.Infrastructure.OutgoingMessages.AggregationResult;

#pragma warning disable
public interface IAssertAggregationResultDocument
{
    IAssertAggregationResultDocument HasMessageId(string expectedMessageId);
    IAssertAggregationResultDocument HasSenderId(string expectedSenderId);
    IAssertAggregationResultDocument HasReceiverId(string expectedReceiverId);
    IAssertAggregationResultDocument HasTimestamp(string expectedTimestamp);
    IAssertAggregationResultDocument HasTransactionId(Guid expectedTransactionId);
    IAssertAggregationResultDocument HasGridAreaCode(string expectedGridAreaCode);
    IAssertAggregationResultDocument HasBalanceResponsibleNumber(string expectedBalanceResponsibleNumber);
    IAssertAggregationResultDocument HasEnergySupplierNumber(string expectedEnergySupplierNumber);
    IAssertAggregationResultDocument HasProductCode(string expectedProductCode);
    IAssertAggregationResultDocument HasPeriod(string expectedStartOfPeriod, string expectedEndOfPeriod);
    IAssertAggregationResultDocument HasPoint(int position, int quantity);
    Task<IAssertAggregationResultDocument> DocumentIsValidAsync();
    IAssertAggregationResultDocument SettlementMethodIsNotPresent();
    IAssertAggregationResultDocument EnergySupplierNumberIsNotPresent();
    IAssertAggregationResultDocument BalanceResponsibleNumberIsNotPresent();
    IAssertAggregationResultDocument QuantityIsNotPresentForPosition(int position);
    IAssertAggregationResultDocument QualityIsNotPresentForPosition(int position);
}
