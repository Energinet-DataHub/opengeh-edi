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
using Microsoft.Azure.Functions.Worker;

namespace B2B.Transactions.ArchitectureTests
{
    public static class FunctionsReflectionHelper
    {
        private static readonly Type _functionAttribute = typeof(FunctionAttribute);

        public static Func<Type, IEnumerable<Type>> FindAllTypes()
            => t => t.Assembly.GetTypes();

        public static Func<IEnumerable<Type>, IEnumerable<Type>> FindAllFunctionTypes()
        {
            return types => types.Where(type => type.GetMethods().Any(MethodIsAnnotatedWithFunctionAttribute));
        }

        public static Func<IEnumerable<Type>, IEnumerable<Type>> FindAllConstructorDependencies()
        {
            return types => types
                .Select(GetOnePublicConstructor)
                .SelectMany(GetConstructorParameters);
        }

        public static Func<Type, IEnumerable<Type>, IEnumerable<Type>> FindAllTypesThatImplementType()
        {
            return (targetType, types) =>
                types.Where(targetType.IsAssignableFrom);
        }

        private static ConstructorInfo? GetOnePublicConstructor(Type? type)
        {
            return type?.GetConstructors().SingleOrDefault(ci => ci.IsPublic && ci.IsStatic == false);
        }

        private static bool MethodIsAnnotatedWithFunctionAttribute(MethodInfo methodInfo)
        {
            return methodInfo.GetCustomAttributes(_functionAttribute).Any();
        }

        private static IEnumerable<Type> GetConstructorParameters(ConstructorInfo? ci)
        {
            return ci == null ? Array.Empty<Type>() : ci.GetParameters().Select(p => p.ParameterType);
        }
    }
}
