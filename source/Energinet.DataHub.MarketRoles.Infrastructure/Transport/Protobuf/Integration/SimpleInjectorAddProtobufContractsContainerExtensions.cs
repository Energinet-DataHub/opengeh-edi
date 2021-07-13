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
using Google.Protobuf;
using SimpleInjector;

namespace Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf.Integration
{
    public static class SimpleInjectorAddProtobufContractsContainerExtensions
    {
        public static void SendProtobuf(this Container container, Assembly[] applicationAssemblies)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            var assemblies = GetAssemblies().Union(applicationAssemblies).ToArray();

            container.Register<MessageSerializer, ProtobufMessageSerializer>(Lifestyle.Scoped);
            container.Register<ProtobufOutboundMapperFactory>(Lifestyle.Scoped);

            ScanForMappers(container, typeof(ProtobufOutboundMapper<>), assemblies);
        }

        public static void ReceiveProtobuf<TProtoContract>(
            this Container container,
            Action<OneOfConfiguration<TProtoContract>> configuration)
            where TProtoContract : class, IMessage
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var config = new OneOfConfiguration<TProtoContract>();
            configuration.Invoke(config);
            container.Register<MessageExtractor>(Lifestyle.Scoped);
            container.Register<MessageDeserializer, ProtobufMessageDeserializer>(Lifestyle.Scoped);
            container.Register<ProtobufInboundMapperFactory>(Lifestyle.Scoped);
            container.Register(() => config.GetParser(), Lifestyle.Scoped);

            ScanForMappers(container, typeof(ProtobufInboundMapper<>), new[]
            {
                typeof(TProtoContract).Assembly,
            });
        }

        private static void ScanForMappers(Container container, Type collectionType, Assembly[] assemblies)
        {
            var types = container.GetTypesToRegister(collectionType, assemblies, new TypesToRegisterOptions
            {
                IncludeGenericTypeDefinitions = true,
                IncludeComposites = false,
            });

            container.Register(collectionType, types, Lifestyle.Scoped);
        }

        private static IEnumerable<Assembly> GetAssemblies()
        {
            yield return typeof(ProtobufOutboundMapper).GetTypeInfo().Assembly;
        }
    }
}
