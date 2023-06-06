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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CimMessageAdapter.ValidationErrors;

namespace CimMessageAdapter.Messages.RequestAggregatedMeasureData;

public class ProcessTypeValidator : IProcessTypeValidator
{
    private static readonly IReadOnlyCollection<string> _whiteList = new[] { "D03", "D04", "D05", "D09", "D32" };

    public async Task<Result> ValidateAsync(string processType, CancellationToken cancellationToken)
    {
        return await Task.FromResult(!_whiteList.Contains(processType) ?
            Result.Failure(new NotSupportedProcessType(processType)) : Result.Succeeded()).ConfigureAwait(false);
    }
}
