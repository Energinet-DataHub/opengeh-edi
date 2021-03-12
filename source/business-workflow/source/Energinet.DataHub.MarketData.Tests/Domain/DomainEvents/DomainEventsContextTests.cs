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
using FluentAssertions;
using Moq;
using Xunit;

namespace Energinet.DataHub.MarketData.Tests.Domain.DomainEvents
{
    [Trait("Category", "Unit")]
    public class DomainEventsContextTests
    {
        [Fact]
        public async Task Null_guard_checks()
        {
            var sut = new MarketData.Domain.SeedWork.DomainEventsContext();

            Assert.Throws<ArgumentNullException>(() => sut.RecordDomainEvent(null!));
            Assert.Throws<ArgumentNullException>(() => sut.RecordDomainEvents(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => sut.PublishEventsAsync(null!));
        }

        [Fact]
        public async Task Events_should_be_published()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());
            var publishDelegate = new Mock<PublishDomainEvent>();
            var events = fixture.CreateMany<IDomainEvent>().ToList();

            var sut = new MarketData.Domain.SeedWork.DomainEventsContext(events);
            await sut.PublishEventsAsync(publishDelegate.Object);

            publishDelegate.Verify(
                p =>
                p.Invoke(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(events.Count));
        }

        [Fact]
        public void Added_domain_event_should_be_stored()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());
            var domainEvent = fixture.Create<IDomainEvent>();

            var sut = new MarketData.Domain.SeedWork.DomainEventsContext();
            sut.RecordDomainEvent(domainEvent);

            sut.DomainEvents.Should().Contain(domainEvent);
        }

        [Fact]
        public void Recorded_domain_events_should_be_stored()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            var domainEvents = fixture.CreateMany<IDomainEvent>().ToArray();

            var sut = new MarketData.Domain.SeedWork.DomainEventsContext();
            sut.RecordDomainEvents(domainEvents);

            sut.DomainEvents.Should().ContainInOrder(domainEvents);
        }
    }
}
