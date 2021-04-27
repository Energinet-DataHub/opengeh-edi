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
using System.Linq;
using System.Reflection;
using MediatR;
using MediatR.Pipeline;
using SimpleInjector;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Common.MediatR
{
    public static class SimpleInjectorMediatorContainerExtensions
    {
        public static Container BuildMediator(this Container container, params Assembly[] assemblies)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

            return BuildMediator(container, (IEnumerable<Assembly>)assemblies);
        }

        private static Container BuildMediator(this Container container, IEnumerable<Assembly> assemblies)
        {
            var allAssemblies = GetAssemblies(assemblies);

            container.RegisterSingleton<IMediator, Mediator>();
            container.Register(typeof(IRequestHandler<,>), allAssemblies);

            RegisterHandlers(container, typeof(INotificationHandler<>), allAssemblies);
            RegisterHandlers(container, typeof(IRequestExceptionAction<,>), allAssemblies);
            RegisterHandlers(container, typeof(IRequestExceptionHandler<,,>), allAssemblies);

            // Custom pipeline behaviors
            var pipelineBehaviors = container.GetTypesToRegister(typeof(IPipelineBehavior<,>), allAssemblies, new TypesToRegisterOptions
            {
                IncludeGenericTypeDefinitions = true,
                IncludeComposites = false,
            }).ToList();

            // Add built-in pipeline behaviors
            pipelineBehaviors.AddRange(new[]
            {
                typeof(RequestExceptionProcessorBehavior<,>),
                typeof(RequestExceptionActionProcessorBehavior<,>),
                typeof(RequestPreProcessorBehavior<,>),
                typeof(RequestPostProcessorBehavior<,>),
            });

            // Register pipeline
            container.Collection.Register(typeof(IPipelineBehavior<,>), pipelineBehaviors);

            container.Collection.Register(typeof(IRequestPreProcessor<>), new[] { typeof(EmptyRequestPreProcessor<>) });
            container.Collection.Register(typeof(IRequestPostProcessor<,>), new[] { typeof(EmptyRequestPostProcessor<,>) });

            container.Register(() => new ServiceFactory(container.GetInstance), Lifestyle.Singleton);

            return container;
        }

        private static void RegisterHandlers(Container container, Type collectionType, Assembly[] assemblies)
        {
            // we have to do this because by default, generic type definitions (such as the Constrained Notification Handler) won't be registered
            var handlerTypes = container.GetTypesToRegister(collectionType, assemblies, new TypesToRegisterOptions
            {
                IncludeGenericTypeDefinitions = true,
                IncludeComposites = false,
            });

            container.Collection.Register(collectionType, handlerTypes);
        }

        private static Assembly[] GetAssemblies(IEnumerable<Assembly> assemblies)
        {
            var allAssemblies = new List<Assembly> { typeof(IMediator).GetTypeInfo().Assembly };
            allAssemblies.AddRange(assemblies);

            return allAssemblies.ToArray();
        }
    }
}
