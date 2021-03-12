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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using Energinet.DataHub.MarketData.Infrastructure.UseCaseProcessing;
using MediatR;
using Moq;
using Xunit;

namespace Energinet.DataHub.MarketData.Tests.Domain.DomainEvents
{
    [Trait("Category", "Unit")]
    public class DomainEventPublisherBehaviorTests
    {
        [Fact]
        public async Task Recorded_events_should_be_published()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());
            var context = fixture.Freeze<DomainEventsContext>();
            var mediator = fixture.Freeze<Mock<IMediator>>();
            var sut = fixture.Create<DomainEventPublisherBehavior<StringRequest, string>>();

            context.RecordDomainEvents(fixture.CreateMany<IDomainEvent>().ToArray());

            await sut.Handle(
                new StringRequest(),
                CancellationToken.None,
                fixture.Create<RequestHandlerDelegate<string>>());

            mediator.Verify(
                p =>
                    p.Publish(
                        It.IsAny<IDomainEvent>(),
                        It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task If_pipeline_throws_an_exception_nothing_is_published()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());
            var context = fixture.Freeze<DomainEventsContext>();
            var mediator = fixture.Freeze<Mock<IMediator>>();
            var sut = fixture.Create<DomainEventPublisherBehavior<StringRequest, string>>();

            context.RecordDomainEvents(fixture.CreateMany<IDomainEvent>().ToArray());

            await Assert.ThrowsAsync<Exception>(() => sut.Handle(new StringRequest(), CancellationToken.None, Next));

            mediator.Verify(p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private static Task<string> Next()
        {
            throw new Exception("Unhandled exception");
        }
    }
}
