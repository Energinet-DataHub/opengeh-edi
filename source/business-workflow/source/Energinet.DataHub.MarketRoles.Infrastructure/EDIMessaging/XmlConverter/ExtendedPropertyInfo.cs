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
using System.Reflection;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDIMessaging.XmlConverter
{
    public class ExtendedPropertyInfo
    {
        public ExtendedPropertyInfo(IList<string> xmlHierarchy, PropertyInfo propertyInfo, Func<string, object> translatorFunc)
            : this(xmlHierarchy, propertyInfo)
        {
            XmlHierarchy = xmlHierarchy;
            PropertyInfo = propertyInfo;
            TranslatorFunc = translatorFunc;
        }

        public ExtendedPropertyInfo(IEnumerable<string> xmlHierarchy, PropertyInfo propertyInfo)
        {
            XmlHierarchy = xmlHierarchy;
            PropertyInfo = propertyInfo;
        }

        public IEnumerable<string> XmlHierarchy { get; }

        public PropertyInfo PropertyInfo { get; }

        public Func<string, object>? TranslatorFunc { get; }
    }
}
