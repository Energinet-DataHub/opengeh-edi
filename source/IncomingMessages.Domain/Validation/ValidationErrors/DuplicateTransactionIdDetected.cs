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

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;

public class DuplicateTransactionIdDetected : ValidationError
{
    public DuplicateTransactionIdDetected(string transactionId)
        : base($"Transaction id '{transactionId}' is not unique and will not be processed.", "00102", "The provided Ids are not unique in the Business Message (e.g. same TransactionId or TimeseriesId used in the same message), or duplicate Ids in requests when calling the SendMessage operation in parallel.", "B2B-009", "TransactionId")
    {
    }

    public DuplicateTransactionIdDetected()
        : base("Duplicated transaction id found", "00102", "The provided Ids are not unique in the Business Message (e.g. same TransactionId or TimeseriesId used in the same message), or duplicate Ids in requests when calling the SendMessage operation in parallel.", "B2B-009", "TransactionId")
    {
    }
}
