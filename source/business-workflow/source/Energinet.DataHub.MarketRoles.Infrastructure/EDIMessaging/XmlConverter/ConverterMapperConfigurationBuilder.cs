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
using System.Data;
using System.Linq;
using System.Linq.Expressions;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDIMessaging.XmlConverter
{
    public class ConverterMapperConfigurationBuilder<T>
    {
        private readonly string _xmlElementName;
        private readonly Dictionary<string, ExtendedPropertyInfo?> _properties;

        public ConverterMapperConfigurationBuilder(string xmlElementName)
        {
            _xmlElementName = xmlElementName;
            _properties = new Dictionary<string, ExtendedPropertyInfo?>();

            var constructor = typeof(T).GetConstructors().FirstOrDefault() ?? throw new InvalidOperationException("Target type must be a record with a single constructor");

            foreach (var parameterInfo in constructor.GetParameters())
            {
                _properties.Add(parameterInfo.Name ?? throw new NoNullAllowedException(), null);
            }
        }

        public ConverterMapperConfigurationBuilder<T> AddProperty<TProperty>(Expression<Func<T, string>> selector, Func<XmlElementInfo, TProperty> translatorFunc, params string[] xmlHierarchy)
        {
            return AddPropertyInternal(selector, CastFunc(translatorFunc), xmlHierarchy);
        }

        public ConverterMapperConfigurationBuilder<T> AddProperty<TProperty>(Expression<Func<T, TProperty>> selector, Func<XmlElementInfo, TProperty> translatorFunc, params string[] xmlHierarchy)
            where TProperty : struct, IComparable, IComparable<TProperty>, IEquatable<TProperty>
        {
            return AddPropertyInternal(selector, CastFunc(translatorFunc), xmlHierarchy);
        }

        public ConverterMapperConfigurationBuilder<T> AddProperty(Expression<Func<T, string>> selector, params string[] xmlHierarchy)
        {
            return AddPropertyInternal(selector, xmlHierarchy);
        }

        public ConverterMapperConfigurationBuilder<T> AddProperty<TProperty>(Expression<Func<T, TProperty>> selector, params string[] xmlHierarchy)
            where TProperty : struct, IComparable, IComparable<TProperty>, IEquatable<TProperty>
        {
            return AddPropertyInternal(selector, xmlHierarchy);
        }

        public ConverterMapperConfiguration Build()
        {
            return new(typeof(T), _xmlElementName, _properties);
        }

        private static Func<XmlElementInfo, object> CastFunc<TProperty>(Func<XmlElementInfo, TProperty> translatorFunc)
        {
            return p => translatorFunc(p) ?? throw new InvalidOperationException($"Type '{typeof(TProperty)}' could not be casted to object");
        }

        private ConverterMapperConfigurationBuilder<T> AddPropertyInternal<TProperty>(Expression<Func<T, TProperty>> selector, Func<XmlElementInfo, object> translatorFunc, params string[] xmlHierarchy)
        {
            var propertyInfo = PropertyInfoHelper.GetPropertyInfo(selector);
            _properties[propertyInfo.Name] = new ExtendedPropertyInfo(xmlHierarchy, propertyInfo, translatorFunc);
            return this;
        }

        private ConverterMapperConfigurationBuilder<T> AddPropertyInternal<TProperty>(Expression<Func<T, TProperty>> selector, string[] xmlHierarchy)
        {
            var propertyInfo = PropertyInfoHelper.GetPropertyInfo(selector);
            _properties[propertyInfo.Name] = new ExtendedPropertyInfo(xmlHierarchy, propertyInfo);
            return this;
        }
    }
}
