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

using NodaTime;

namespace Messaging.Domain.Transactions.MoveIn;

public class TransactionNotFoundException : Exception
{
    private TransactionNotFoundException(string message)
        : base(message)
    {
    }

    private TransactionNotFoundException()
    {
    }

    private TransactionNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public static TransactionNotFoundException TransactionIdNotFound(string transactionId)
    {
        return new TransactionNotFoundException($"Move in transaction {transactionId} was not found");
    }

    public static TransactionNotFoundException TransactionForProcessIdNotFound(string processId)
    {
        return new TransactionNotFoundException($"Move in transaction for business process id {processId} was not found");
    }

    public static TransactionNotFoundException NotFound(string marketEvaluationPointNumber, Instant effectiveDate)
    {
        return new TransactionNotFoundException(
            $"No move in transaction found for market evaluation point number '{marketEvaluationPointNumber}' with effective date on '{effectiveDate.ToString()}'");
    }
}
