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

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;

namespace Energinet.DataHub.EDI.IntegrationTests.B2BApi.Mocks;

/// <summary>
/// A spy used for testing Azure Functions middleware, which registers if next() is called inside the middleware.
/// </summary>
[SuppressMessage("Style", "VSTHRD200", Justification = "Test class")]
internal sealed class NextSpy
{
    public bool NextWasCalled { get; private set; }

    public Task Next(FunctionContext ctx)
    {
        NextWasCalled = true;
        return Task.CompletedTask;
    }
}
