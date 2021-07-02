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
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Application.Common;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDIMessaging.XmlConverter
{
    public static class ConverterMapperConfigurations
    {
        public static void AssertConfigurationValid()
        {
            var businessRequests = GetBusinessRequests();

            var configurations = GetAllConfigurations();

            foreach (var type in businessRequests)
            {
                var configForType = configurations.SingleOrDefault(x => x.Configuration.GetType() == type);

                if (configForType == null) throw new Exception($"Missing XmlMappingConfiguration for type: {type.Name}");

                var propertiesInConfig = configForType.Configuration.GetProperties();
                var propertiesInType = type.GetProperties();

                foreach (var propertyInfo in propertiesInType)
                {
                    if (!propertiesInConfig.TryGetValue(propertyInfo.Name, out var propertyInConfig) || propertyInConfig is null)
                    {
                        throw new Exception($"Property {propertyInfo.Name} missing in XmlMappingConfiguration for type: {type.Name}");
                    }
                }

                if (propertiesInType.Length != propertiesInConfig.Count) throw new Exception($"Properties mismatch in XmlMappingConfiguration for type: {type.Name}");
            }
        }

        private static List<XmlMappingConfigurationBase> GetAllConfigurations()
        {
            return typeof(XmlMappingConfigurationBase).Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(XmlMappingConfigurationBase)) && !t.IsAbstract)
                .Select(Activator.CreateInstance)
                .Cast<XmlMappingConfigurationBase>()
                .ToList();
        }

        private static IEnumerable<Type> GetBusinessRequests()
        {
            return typeof(RequestChangeOfSupplier).Assembly.GetTypes()
                .Where(p => typeof(IBusinessRequest).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract)
                .ToList();
        }
    }
}
