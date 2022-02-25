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
using System.ComponentModel;
using System.Data;
using Dapper;
using NodaTime;

namespace Energinet.DataHub.MarketRoles.Infrastructure.DataAccess
{
    public class NodaTimeSqlMapper : SqlMapper.TypeHandler<Instant>
    {
        public static readonly NodaTimeSqlMapper Instance = new NodaTimeSqlMapper();

        private NodaTimeSqlMapper()
        {
        }

#pragma warning disable CA1003 // Generic Handlers
        public event EventHandler<IDbDataParameter>? OnSetValue;
#pragma warning restore CA1003

        public override void SetValue(IDbDataParameter parameter, Instant value)
        {
            if (parameter == null) throw new ArgumentNullException(nameof(parameter));

            parameter.Value = value.ToDateTimeUtc();

            OnSetValue?.Invoke(this, parameter);
        }

        public override Instant Parse(object value)
        {
            if (value == null || value is DBNull) return default;

            if (value is DateTime dateTime)
            {
                var dt = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                return Instant.FromDateTimeUtc(dt);
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                return Instant.FromDateTimeOffset(dateTimeOffset);
            }

            if (value is string s)
            {
                var conv = TypeDescriptor.GetConverter(typeof(Instant));

                if (conv?.CanConvertFrom(typeof(string)) == true)
                {
                    return (Instant)conv.ConvertFromString(s);
                }
            }

            throw new DataException("Cannot convert " + value.GetType() + " to NodaTime.Instant");
        }
    }
}
