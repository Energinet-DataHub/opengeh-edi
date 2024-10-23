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
using Energinet.DataHub.EDI.SubsystemTests.Responses.xml;

namespace Energinet.DataHub.EDI.SubsystemTests.Tests.B2BErrors.Asserters;

[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Test code should not configure await")]
public static class ErrorAsserter
{
    public static Task AssertCorrectErrorIsReturnedAsync(string expectedErrorCode, string expectedErrorMessage, string response)
    {
        var responseError = SynchronousError.BuildB2BErrorResponse(response);

        if (responseError?.Details != null)
        {
            foreach (var error in responseError?.Details.InnerErrors!)
            {
                if (error.Code.Equals(expectedErrorCode, StringComparison.Ordinal))
                {
                    Assert.Equal(expectedErrorCode, error.Code);
                    Assert.Equal(expectedErrorMessage, error.Message);
                }
            }
        }
        else
        {
            Assert.Equal(expectedErrorCode, responseError?.Code);
            Assert.Equal(expectedErrorMessage, responseError?.Message);
        }

        return Task.CompletedTask;
    }
}
