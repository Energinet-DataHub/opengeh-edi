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

using Messaging.CimMessageAdapter.Messages;
using Messaging.CimMessageAdapter.Response;
using Messaging.Domain.SeedWork;
using Xunit;
using Xunit.Categories;

namespace Messaging.CimMessageAdapter.Tests;

[UnitTest]
public class ResponseFactoryTests
{
    [Theory]
    [InlineData(nameof(CimFormat.Xml))]
    public void Generate_empty_response_when_no_validation_errors_has_occurred(string format)
    {
        var responseFactory = new ResponseFactory();
        var result = Result.Succeeded();

        var response = responseFactory.From(result, EnumerationType.FromName<CimFormat>(format));

        Assert.False(response.IsErrorResponse);
        Assert.Empty(response.MessageBody);
    }
}
