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
using System.Reflection;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.MediatR;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.Telemetry;
using MediatR;
using MediatR.Pipeline;
using SimpleInjector;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Common.SimpleInjector
{
    public class MediatorBuilder
    {
        private readonly Container _container;

        internal MediatorBuilder(Container container)
        {
            _container = container;

            RegisterDefaults();
        }

        public MediatorBuilder WithPipeline(params Type[] pipelineBehaviors)
        {
            // Add built-in pipeline behaviors
            var builtInBehaviors = new[]
            {
                typeof(RequestHandlerTelemetryBehavior<,>),
                typeof(RequestExceptionProcessorBehavior<,>),
                typeof(RequestExceptionActionProcessorBehavior<,>),
                typeof(RequestPreProcessorBehavior<,>),
                typeof(RequestPostProcessorBehavior<,>),
            };

            // Register both built-in and custom pipeline
            _container.Collection.Register(typeof(IPipelineBehavior<,>), builtInBehaviors.Union(pipelineBehaviors));

            return this;
        }

        public MediatorBuilder WithRequestHandlers(params Type[] handlers)
        {
            _container.Register(typeof(IRequestHandler<,>), handlers);

            return this;
        }

        public MediatorBuilder WithNotificationHandlers(params Type[] handlers)
        {
            _container.Collection.Register(typeof(INotificationHandler<>), handlers);
            _container.RegisterDecorator(typeof(INotificationHandler<>), typeof(NotificationHandlerTelemetryDecorator<>));

            return this;
        }

        private void RegisterDefaults()
        {
            _container.RegisterSingleton<IMediator, Mediator>();

            _container.Collection.Register(typeof(IRequestPreProcessor<>), new[] { typeof(EmptyRequestPreProcessor<>) });
            _container.Collection.Register(typeof(IRequestPostProcessor<,>), new[] { typeof(EmptyRequestPostProcessor<,>) });

            var mediatrAssemblies = new[] { typeof(IMediator).GetTypeInfo().Assembly };
            RegisterHandlers(typeof(IRequestExceptionAction<,>), mediatrAssemblies);
            RegisterHandlers(typeof(IRequestExceptionHandler<,,>), mediatrAssemblies);

            _container.Register(() => new ServiceFactory(_container.GetInstance), Lifestyle.Singleton);
        }

        private void RegisterHandlers(Type collectionType, Assembly[] assemblies)
        {
            var handlerTypes = _container.GetTypesToRegister(collectionType, assemblies, new TypesToRegisterOptions
            {
                IncludeGenericTypeDefinitions = true,
                IncludeComposites = false,
            });
            _container.Collection.Register(collectionType, handlerTypes);
        }
    }
}
