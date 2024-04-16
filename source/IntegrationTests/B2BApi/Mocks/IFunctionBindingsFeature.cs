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

namespace Energinet.DataHub.EDI.IntegrationTests.B2BApi.Mocks;

/// <summary>
/// Used to simulate the IFunctionBindingsFeature that is internal in Microsofts Azure Functions package.
/// - Holds the InvocationResult which is typically (when using HTTP triggered functions) the returned HTTP response (an instance of HttpResponseData)
/// </summary>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Test class")]
internal interface IFunctionBindingsFeature
{
    object? InvocationResult { get; set; }
}
