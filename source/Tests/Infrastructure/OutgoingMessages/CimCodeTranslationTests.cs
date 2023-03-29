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

using Domain.OutgoingMessages;
using Infrastructure.OutgoingMessages.Common;
using Xunit;

namespace Tests.Infrastructure.OutgoingMessages;

public class CimCodeTests
{
    [Theory]
    [InlineData(nameof(ProcessType.BalanceFixing), "D04")]
    [InlineData(nameof(ProcessType.MoveIn), "E65")]
    public void Translate_process_type(string processType, string expectedCode)
    {
        Assert.Equal(expectedCode, CimCode.Of(ProcessType.From(processType)));
    }
}
