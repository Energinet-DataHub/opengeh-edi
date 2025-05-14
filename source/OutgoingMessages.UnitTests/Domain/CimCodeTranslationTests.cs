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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain;

public class CimCodeTests
{
    [Theory]
    [InlineData("1234567890123", "A10")]
    [InlineData("1234567890123456", "A01")]
    public void Given_ActorNumber_When_CodingSchemeOf_Then_CorrectCimCodingScheme(
        string actorNumber,
        string expectedCode)
    {
        Assert.Equal(expectedCode, CimCode.CodingSchemeOf(ActorNumber.Create(actorNumber)));
    }
}
