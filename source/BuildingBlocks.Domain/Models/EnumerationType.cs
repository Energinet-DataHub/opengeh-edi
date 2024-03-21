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

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models
{
    public abstract class EnumerationType : IComparable
    {
        protected EnumerationType(string name)
        {
            Name = name;
        }

        public string Name { get;  }

        public static bool operator ==(EnumerationType? left, EnumerationType? right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(EnumerationType? left, EnumerationType? right)
        {
            return !(left == right);
        }

        public static bool operator <(EnumerationType left, EnumerationType right)
        {
            return ReferenceEquals(left, null) ? !ReferenceEquals(right, null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(EnumerationType left, EnumerationType right)
        {
            return ReferenceEquals(left, null) || left.CompareTo(right) <= 0;
        }

        public static bool operator >(EnumerationType left, EnumerationType right)
        {
            return !ReferenceEquals(left, null) && left.CompareTo(right) > 0;
        }

        public static bool operator >=(EnumerationType left, EnumerationType right)
        {
            return ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.CompareTo(right) >= 0;
        }

        public static IEnumerable<T> GetAll<T>()
            where T : EnumerationType
        {
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

            return fields.Select(f => f.GetValue(null)).Cast<T>();
        }

        public static T FromName<T>(string name)
            where T : EnumerationType
        {
            var matchingItem = Parse<T, string>(name, "name", item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return matchingItem;
        }

        public override string ToString() => Name;

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (obj is not EnumerationType otherValue)
            {
                return false;
            }

            var typeMatches = GetType() == obj.GetType();
            var valueMatches = ValueMatches(otherValue); // Name.Equals(otherValue.Name, StringComparison.OrdinalIgnoreCase);

            return typeMatches && valueMatches;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode(StringComparison.OrdinalIgnoreCase);
        }

        public int CompareTo(object? obj)
        {
            ArgumentNullException.ThrowIfNull(obj);

            return string.Compare(Name, ((EnumerationType)obj).Name, StringComparison.OrdinalIgnoreCase);
        }

        protected virtual bool ValueMatches(EnumerationType otherEnumerationType)
        {
            ArgumentNullException.ThrowIfNull(otherEnumerationType);

            return Name.Equals(otherEnumerationType.Name, StringComparison.OrdinalIgnoreCase);
        }

        private static T Parse<T, TValue>(TValue value, string description, Func<T, bool> predicate)
            where T : EnumerationType
        {
            var matchingItem = GetAll<T>().FirstOrDefault(predicate);

            return matchingItem ?? throw new InvalidOperationException($"'{value}' is not a valid {description} in {typeof(T)}");
        }
    }
}
