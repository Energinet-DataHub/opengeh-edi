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
using Energinet.DataHub.MarketData.EntryPoint;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Energinet.DataHub.MarketData.Tests.UnitOfWork
{
    [Trait("Category", "Unit")]
    public class UnitOfWorkServiceRegistrationTests
    {
        [Fact]
        public void Configuration_set_to_short_lived_should_register_short_lived()
        {
            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>(
                        UnitOfWorkServiceRegistration.UnitOfWorkType,
                        UnitOfWorkServiceRegistration.ShortLivedTransaction),
                });

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configBuilder.Build());
            services.AddUnitOfWork(string.Empty);
            var sp = services.BuildServiceProvider();

            sp.Should().NotBeNull();

            var unitOfWork = sp.GetService<IUnitOfWork>();
            unitOfWork.Should().NotBeNull().And.BeOfType<ShortLivedDbTransaction>();
        }

        [Fact]
        public void Configuration_set_to_long_lived_should_register_long_lived()
        {
            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>(
                        UnitOfWorkServiceRegistration.UnitOfWorkType,
                        UnitOfWorkServiceRegistration.LongLivedTransaction),
                });

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configBuilder.Build());
            services.AddUnitOfWork(string.Empty);
            var sp = services.BuildServiceProvider();

            sp.Should().NotBeNull();

            var unitOfWork = sp.GetService<IUnitOfWork>();
            unitOfWork.Should().NotBeNull().And.BeOfType<LongLivedDbTransaction>();
        }

        [Fact]
        public void No_unit_of_work_type_should_register_default()
        {
            var configBuilder = new ConfigurationBuilder();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configBuilder.Build());
            services.AddUnitOfWork(string.Empty);
            var sp = services.BuildServiceProvider();

            sp.Should().NotBeNull();

            var unitOfWork = sp.GetService<IUnitOfWork>();
            unitOfWork.Should().NotBeNull().And.BeOfType<ShortLivedDbTransaction>();
        }

        [Fact]
        public void Invalid_configuration_value_throws_invalid_operation_exception()
        {
            var unknownTransactionType = new KeyValuePair<string, string>(
                UnitOfWorkServiceRegistration.UnitOfWorkType,
                Guid.NewGuid().ToString("N"));

            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new[] { unknownTransactionType });

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configBuilder.Build());

            Assert.Throws<InvalidOperationException>(() => services.AddUnitOfWork(string.Empty));
        }
    }
}
