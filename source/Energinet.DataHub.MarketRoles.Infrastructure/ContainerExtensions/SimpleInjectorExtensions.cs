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
using Energinet.DataHub.MarketRoles.Application.EDI;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using SimpleInjector;

namespace Energinet.DataHub.MarketRoles.Infrastructure.ContainerExtensions
{
    public static class SimpleInjectorExtensions
    {
        public static void AddValidationErrorConversion(this Container container, bool validateRegistrations, params Assembly[] assemblies)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));

            var validationErrorTypes = container.GetTypesToRegister(typeof(ValidationError), assemblies).ToList();
            var errorConverterTypes = container.GetTypesToRegister(typeof(ErrorConverter<>), assemblies).ToList();
            container.Register(typeof(ErrorConverter<>), errorConverterTypes, Lifestyle.Singleton);
            container.Register<ErrorMessageFactory>(Lifestyle.Singleton);

            var converterRegistrations = errorConverterTypes
                .Select(container.GetErrorConverterRegistration)
                .ToList();
            container.Collection.Register(typeof(ErrorConverterRegistration), converterRegistrations);

            if (!validateRegistrations)
            {
                return;
            }

            var isAllValidationErrorsCoveredByConverters = validationErrorTypes
                .All(error => converterRegistrations
                    .Find(converter => error == converter.Error) != null);

            if (!isAllValidationErrorsCoveredByConverters)
            {
                var missingRegistrations = validationErrorTypes
                    .Except(converterRegistrations.Select(converterRegistration => converterRegistration.Error))
                    .ToList();
                throw new InvalidOperationException(
                    $"Not all validation errors are covered by error converters. Missing:{Environment.NewLine}{string.Join(Environment.NewLine, missingRegistrations)}");
            }

            var hasDuplicateRegistrations = converterRegistrations
                .GroupBy(registration => registration.Error)
                .Any(group => group.Count() > 1);

            if (hasDuplicateRegistrations)
            {
                var duplicateRegistrations = converterRegistrations
                    .GroupBy(registration => registration.Error)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .ToList();
                throw new InvalidOperationException(
                    $"There should not be any duplicate error converter registrations, but we found these duplicates:{Environment.NewLine}{string.Join(Environment.NewLine, duplicateRegistrations)}");
            }
        }

        private static ErrorConverterRegistration GetErrorConverterRegistration(this Container container, Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (type.BaseType?.GenericTypeArguments.SingleOrDefault() == null) throw new InvalidOperationException("ErrorConverterRegistration not found for type: " + type);

            return new ErrorConverterRegistration(
                type.BaseType.GenericTypeArguments.Single(),
                () => (ErrorConverter)container.GetInstance(type.BaseType));
        }
    }
}
