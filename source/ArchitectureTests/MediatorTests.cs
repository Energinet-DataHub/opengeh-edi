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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Energinet.DataHub.EDI.Api;
using Energinet.DataHub.EDI.Application.Configuration.Commands.Commands;
using Energinet.DataHub.EDI.Application.Configuration.Queries;
using Energinet.DataHub.EDI.Infrastructure.Configuration;
using MediatR;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Energinet.DataHub.EDI.ArchitectureTests;

public class MediatorTests
{
    public static TheoryData<Type> GetAllMediatorRequestTypes()
    {
        var allTypes = GetAllTypes();
        var requestTypes = GetAllMediatorRequests(allTypes);

        var data = new TheoryData<Type>();

        foreach (var type in requestTypes)
        {
            data.Add(type);
        }

        return data;
    }

    public static TheoryData<Type[], Type> GetAllMediatorRequestTypesAndHandlers()
    {
        var allTypes = GetAllTypes();

        var mediatorHandlerBaseType1 = typeof(IRequestHandler<>);
        var mediatorHandlerBaseType2 = typeof(IRequestHandler<,>);
        var getAllTypesOfGeneric = ReflectionHelper.FindAllTypesThatImplementGenericInterface();

        var requestHandlerTypes = getAllTypesOfGeneric(mediatorHandlerBaseType1, allTypes).ToList();
        requestHandlerTypes.AddRange(getAllTypesOfGeneric(mediatorHandlerBaseType2, allTypes));

        var requestTypes = GetAllMediatorRequests(allTypes);

        var data = new TheoryData<Type[], Type>();

        var requestHandlerTypesArray = requestHandlerTypes.ToArray();
        foreach (var type in requestTypes)
        {
            data.Add(requestHandlerTypesArray, type);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(GetAllMediatorRequestTypesAndHandlers))]
    public void RequestHandler_exists_for_all_mediator_requests(Type[] allHandlers, Type requestType)
    {
        var handlersForRequest = allHandlers.Where(h =>
        {
            var baseGenericInterface = h.GetInterfaces().Single(InterfaceIsMediatorHandler);
            var arguments = baseGenericInterface!.GetGenericArguments();
            return arguments[0] == requestType;
        });

        Assert.Single(handlersForRequest);
    }

    [Theory]
    [MemberData(nameof(GetAllMediatorRequestTypes))]
    public void Request_naming_is_correct(Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        var expectedSuffix = "Request";

        if (requestType.IsAssignableTo(typeof(InternalCommand)))
            expectedSuffix = "Command";
        else if (requestType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)))
            expectedSuffix = "Command";
        else if (requestType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>)))
            expectedSuffix = "Query";

        Assert.EndsWith(expectedSuffix, requestType.Name, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(GetAllMediatorRequestTypesAndHandlers))]
    public void RequestHandler_naming_is_correct(Type[] allHandlers, Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        var handlerForRequest = allHandlers.Single(h =>
        {
            var baseGenericInterface = h.GetInterfaces().Single(InterfaceIsMediatorHandler);
            var arguments = baseGenericInterface!.GetGenericArguments();
            return arguments[0] == requestType;
        });

        var expectedHandlerName = $"{requestType.Name}Handler";

        Assert.Equal(expectedHandlerName, handlerForRequest.Name);
    }

    private static bool InterfaceIsMediatorHandler(Type i) => i.IsGenericType &&
                                                              (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                                                               i.GetGenericTypeDefinition() == typeof(IRequestHandler<>));

    private static IEnumerable<Type> GetAllMediatorRequests(IEnumerable<Type> allTypes)
    {
        var mediatorBaseType = typeof(IBaseRequest);
        var getAllTypesOfType = ReflectionHelper.FindAllTypesThatImplementType();

        var requestTypes = getAllTypesOfType(mediatorBaseType, allTypes);
        return requestTypes;
    }

    private static List<Type> GetAllTypes()
    {
        var assemblies = new[]
        {
            ApplicationAssemblies.Application,
            ApplicationAssemblies.Domain,
            ApplicationAssemblies.Infrastructure,
        };
        var getAllTypes = ReflectionHelper.FindAllTypesInAssemblies();
        var allTypes = getAllTypes(assemblies).ToList();
        return allTypes;
    }
}
