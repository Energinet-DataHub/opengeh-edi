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
using System.Linq.Expressions;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.XmlConverter
{
    public class ConverterMapperConfiguration
    {
        private readonly ConstructorDelegate _cachedConstructor;

        public ConverterMapperConfiguration(Type type, string xmlElementName, Dictionary<string, ExtendedPropertyInfo?> properties)
        {
            Type = type;
            XmlElementName = xmlElementName;
            Properties = properties;
            _cachedConstructor = CreateConstructor();
        }

        private delegate object ConstructorDelegate(params object?[] args);

        public Dictionary<string, ExtendedPropertyInfo?> Properties { get; }

        public string XmlElementName { get; }

        public Type Type { get; }

        public object CreateInstance(params object?[] parameters)
        {
            return _cachedConstructor(parameters);
        }

        private ConstructorDelegate CreateConstructor()
        {
            var constructorInfo = Type.GetConstructors().SingleOrDefault() ??
                                  throw new InvalidOperationException("No constructor found for type");
            var parameters = constructorInfo.GetParameters().Select(x => x.ParameterType);
            var paramExpr = Expression.Parameter(typeof(object[]));
            var constructorParameters = parameters.Select((paramType, index) =>
                    Expression.Convert(Expression.ArrayAccess(paramExpr, Expression.Constant(index)), paramType))
                .ToArray<Expression>();
            var body = Expression.New(constructorInfo, constructorParameters);
            return Expression.Lambda<ConstructorDelegate>(body, paramExpr).Compile();
        }
    }
}
