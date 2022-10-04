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

using Messaging.Domain.Actors;
using Xunit;

namespace Messaging.Tests.Domain.Actors;

public class ActorNumberTests
{
    [Fact]
    public void Create_test()
    {
        var number = "1234567890123";

        var sut = ActorNumber.Create(number);

        Assert.Equal(number, sut.Value);
    }
}
