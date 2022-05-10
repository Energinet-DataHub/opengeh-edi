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

namespace Messaging.Application.Transactions.MoveIn;

public class MoveInRequest
{
    public MoveInRequest(string? consumerName, string energySupplierGlnNumber, string? socialSecurityNumber, string? vatNumber, string accountingPointGsrnNumber, string startDate, string transactionId)
    {
        ConsumerName = consumerName;
        EnergySupplierGlnNumber = energySupplierGlnNumber;
        SocialSecurityNumber = socialSecurityNumber;
        VATNumber = vatNumber;
        AccountingPointGsrnNumber = accountingPointGsrnNumber;
        StartDate = startDate;
        TransactionId = transactionId;
    }

    public string TransactionId { get; }

    public string? ConsumerName { get; }

    public string EnergySupplierGlnNumber { get; }

    public string? SocialSecurityNumber { get; }

    public string? VATNumber { get; }

    public string AccountingPointGsrnNumber { get; }

    public string StartDate { get; }
}
