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

using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.DurableTask;

public static class DurableOrchestrationStatusExtensions
{
    public static List<HistoryItem> GetOrderedHistory(this DurableOrchestrationStatus orchestrationStatus)
    {
        var history = orchestrationStatus.History
            .OrderBy(item => item["Timestamp"])
            .Select(item =>
            {
                dynamic? historyItem = item.ToObject<dynamic>();

                return new HistoryItem(
                    historyItem!.Timestamp as string,
                    historyItem.FunctionName as string,
                    historyItem.Result);
            })
            .ToList();

        return history;
    }
}

public record HistoryItem(
    string? Timestamp,
    string? FunctionName,
    dynamic? Result);
