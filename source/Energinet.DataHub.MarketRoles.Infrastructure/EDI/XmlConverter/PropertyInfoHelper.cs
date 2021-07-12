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
using System.Linq.Expressions;
using System.Reflection;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.XmlConverter
{
    public static class PropertyInfoHelper
    {
        /// <summary>
        /// Gets the corresponding <see cref="PropertyInfo" /> from an <see cref="Expression" />.
        /// </summary>
        /// <param name="property">The expression that selects the property to get info on.</param>
        /// <returns>The property info collected from the expression.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="property" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The expression doesn't indicate a valid property."</exception>
        public static PropertyInfo GetPropertyInfo<TProperty, T>(Expression<Func<T, TProperty>> property)
        {
            return InternalGetPropertyInfo(property);
        }

        public static PropertyInfo GetPropertyInfo<T>(Expression<Func<T>> property)
        {
            return InternalGetPropertyInfo(property);
        }

        private static PropertyInfo InternalGetPropertyInfo<T>(Expression<T> property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            return property.Body switch
            {
                UnaryExpression unaryExp when unaryExp.Operand is MemberExpression memberExp => (PropertyInfo)memberExp.Member,
                MemberExpression memberExp => (PropertyInfo)memberExp.Member,
                _ => throw new ArgumentException($"The expression doesn't indicate a valid property. [ {property} ]"),
            };
        }
    }
}
