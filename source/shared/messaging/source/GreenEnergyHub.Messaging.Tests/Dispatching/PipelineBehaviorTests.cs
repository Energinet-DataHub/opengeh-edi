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
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GreenEnergyHub.Messaging.Tests.Dispatching
{
    public class PipelineBehaviorTests
    {
        [Fact]
        public void PipelineBehavior_should_be_registered_within_service_container()
        {
            var services = new ServiceCollection();
            services.AddPipelineBehavior(pipeline => pipeline
                .Apply(Behaviors.UnitOfWork));

            services.Should().ContainSingle(descriptor => descriptor.ImplementationType == Behaviors.UnitOfWork);
        }

        [Fact]
        public void Behavior_registered_dependencies_should_be_resolved()
        {
            var services = new ServiceCollection();
            services.AddPipelineBehavior(pipeline => pipeline
                .Apply(Behaviors.UnitOfWork, depends => depends.On<UnitOfWork>()));

            services.Should().ContainSingle(descriptor => descriptor.ImplementationType == typeof(UnitOfWork));

            var sp = services.BuildServiceProvider();
            using (sp.CreateScope())
            {
                var uow = sp.GetService<UnitOfWork>();
                uow.Should().NotBeNull();
            }
        }

        [Fact]
        public void Multiple_behaviours_should_be_registered()
        {
            var services = new ServiceCollection();
            services.AddPipelineBehavior(pipeline => pipeline
                .Apply(Behaviors.Logging)
                .ContinueWith(Behaviors.UnitOfWork, depends => depends.On<UnitOfWork>())
                .ContinueWith(Behaviors.Authorization));

            services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).Should().HaveCount(3);
        }

        [Fact]
        public void Type_that_is_not_recognized_as_pipeline_behavior_throws_an_exception()
        {
            Assert.Throws<ArgumentException>(() => new ServiceCollection()
                .AddPipelineBehavior(pipeline => pipeline
                    .Apply(typeof(string))));
        }

        [Fact]
        public void Type_that_is_not_recognized_as_pipeline_behavior_in_continue_with_throws_an_exception()
        {
            Assert.Throws<ArgumentException>(() => new ServiceCollection()
                .AddPipelineBehavior(pipeline => pipeline
                    .Apply(Behaviors.Authorization)
                    .ContinueWith(typeof(string))));
        }
    }
}
