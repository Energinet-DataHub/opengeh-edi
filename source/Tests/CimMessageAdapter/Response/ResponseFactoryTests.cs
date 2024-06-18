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

using System;
using System.Collections.Generic;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Response;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.Tests.CimMessageAdapter.Response;

[UnitTest]
public class ResponseFactoryTests
{
    [Fact]
    public void Generate_empty_response_when_no_validation_errors_has_occurred()
    {
        var responseFactory = new ResponseFactory(new[] { new XmlResponseFactory() });
        var result = Result.Succeeded();

        var response = responseFactory.From(result, DocumentFormat.Xml);

        Assert.False(response.IsErrorResponse);
        Assert.Empty(response.MessageBody);
    }

    [Fact]
    public void Throw_if_requested_format_can_not_be_parsed()
    {
        var responseFactory = new ResponseFactory(new List<IResponseFactory>());
        var result = Result.Succeeded();

        Assert.Throws<InvalidOperationException>(() => responseFactory.From(result, DocumentFormat.Json));
    }
}
