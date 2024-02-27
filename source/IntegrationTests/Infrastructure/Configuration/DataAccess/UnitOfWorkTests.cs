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
using System.IO;
using System.Linq;
using System.Reflection;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Configuration.DataAccess;

public sealed class UnitOfWorkTests : TestBase
{
    public UnitOfWorkTests(IntegrationTestFixture integrationTestFixture)
        : base(integrationTestFixture)
    {
    }

    public static IEnumerable<object[]> FindAllImplementationsOfUnitOfWorkDbContext()
    {
        var unitOfWorkDbContextType = typeof(UnitOfWorkDbContext);

        var rootFolder = string.Join(
            "\\",
            AppDomain.CurrentDomain.BaseDirectory.Split("\\").Reverse().Skip(5).Reverse());

        var assemblyPaths = Directory.GetFiles(rootFolder, "*.dll", SearchOption.AllDirectories)
            .Where(p => p.Contains("net8.0", StringComparison.Ordinal))
            .Where(p => p.Contains("Energinet.DataHub.EDI", StringComparison.Ordinal))
            .DistinctBy(s => s.Split("\\").Last());

        var assemblies = assemblyPaths.Select(Assembly.LoadFrom);

        var types = assemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => unitOfWorkDbContextType.IsAssignableFrom(t) && !t.IsAbstract)
            .ToArray();

        return types.Select(t => new object[] { t });
    }

    [Theory]
    [MemberData(nameof(FindAllImplementationsOfUnitOfWorkDbContext))]
    public void METHOD(Type typeOfSpecificUnitOfWorkDbContext)
    {
        var getServiceMethodInfo = typeof(TestBase).GetMethod(
            "GetService",
            BindingFlags.Instance | BindingFlags.NonPublic);

        var getServiceMethod = getServiceMethodInfo!.MakeGenericMethod(typeOfSpecificUnitOfWorkDbContext);

        var someSpecificUnitOfWorkDbContext = getServiceMethod.Invoke(this, null);
        var unitOfWorkDbContexts = GetServices<UnitOfWorkDbContext>();
        var unitOfWorkDbContextOfSameTypeAsTheSpecificOne =
            unitOfWorkDbContexts.Single(dbc => dbc.GetType() == typeOfSpecificUnitOfWorkDbContext);

        someSpecificUnitOfWorkDbContext
            .Should()
            .BeSameAs(unitOfWorkDbContextOfSameTypeAsTheSpecificOne);
    }
}
