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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Response;
using FluentAssertions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.Tests.CimMessageAdapter.Response;

[UnitTest]
public sealed class ResponseFactoryTests
{
    [Fact]
    public void Given_SucceededResult_When_ResponseCreated_Then_ResponseIsEmpty()
    {
        var responseFactory = new ResponseFactory([new XmlResponseFactory()]);
        var result = Result.Succeeded();

        var response = responseFactory.From(result, DocumentFormat.Xml);

        response.IsErrorResponse.Should().BeFalse();
        response.MessageBody.Should().BeEmpty();
    }

    [Fact]
    public void Given_AResponseFactory_When_InvokedWithUnknownDocumentType_Then_ThrowError()
    {
        var responseFactory = new ResponseFactory([]);
        var result = Result.Succeeded();

        var act = () => responseFactory.From(result, DocumentFormat.Json);
        act.Should().ThrowExactly<InvalidOperationException>();
    }
}
