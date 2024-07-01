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

using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks;

public sealed record QueryResult<TResult>
    where TResult : OutgoingMessageDto
{
    private QueryResult(bool isSuccess, TResult? result)
    {
        IsSuccess = isSuccess;
        Result = result;
    }

    public bool IsSuccess { get; }

    public TResult? Result { get; }

    internal static QueryResult<TResult> Success(TResult result)
    {
        return new(true, result);
    }

    internal static QueryResult<TResult> Error()
    {
        return new(false, default);
    }
}
