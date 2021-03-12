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
using System.Threading.Tasks;
using GreenEnergyHub.Messaging;
using GreenEnergyHub.Messaging.Validation;

namespace Energinet.DataHub.MarketData.Tests.InputValidation.Helpers
{
    public class RuleCollectionTester<TCollection, T>
        where TCollection : RuleCollection<T>
    {
        private FluentHybridRuleEngine<T> _engine;

        public RuleCollectionTester(TCollection collection)
        {
            _engine = new FluentHybridRuleEngine<T>(collection, ServiceProviderDelegate);
        }

        public RuleCollectionTester(TCollection collection, Dictionary<Type, object> objectMap)
        {
            object Creator(Type type)
            {
                if (objectMap.ContainsKey(type)) return objectMap[type];
                throw new InvalidOperationException("Unknown type in objectmap");
            }

            _engine = new FluentHybridRuleEngine<T>(collection, Creator);
        }

        public Task<RuleResultCollection> InvokeAsync(T objectToValidate)
        {
            return _engine.ValidateAsync(objectToValidate);
        }

        private object ServiceProviderDelegate(Type servicetype)
        {
            return Activator.CreateInstance(servicetype) ?? throw new InvalidOperationException("Can not create type");
        }
    }
}
