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

using System.Reflection;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain;

public abstract class ValueObject : IEquatable<ValueObject>
{
    private List<PropertyInfo>? _properties;
    private List<FieldInfo>? _fields;

    public static bool operator ==(ValueObject? obj1, ValueObject? obj2)
    {
        return obj1?.Equals(obj2) ?? Equals(obj2, null);
    }

    public static bool operator !=(ValueObject? obj1, ValueObject? obj2)
    {
        return !(obj1 == obj2);
    }

    public bool Equals(ValueObject? other)
    {
        return other is not null && Equals(other as object);
    }

    public override bool Equals(object? obj)
    {
        return obj != null && GetType() == obj.GetType() && GetProperties().All(p => PropertiesAreEqual(obj, p))
                                                             && GetFields().All(f => FieldsAreEqual(obj, f));
    }

    public override int GetHashCode()
    {
        var hash = GetProperties().Select(prop => prop.GetValue(this, null)).Aggregate(17, (current, value) => HashValue(current, value!));

        return GetFields().Select(field => field.GetValue(this)).Aggregate(hash, (current, value) => HashValue(current, value!));
    }

    private static int HashValue(int seed, object value)
    {
        var currentHash = value?.GetHashCode() ?? 0;

        return (seed * 23) + currentHash;
    }

    private bool PropertiesAreEqual(object obj, PropertyInfo p)
    {
        return Equals(p.GetValue(this, null), p.GetValue(obj, null));
    }

    private bool FieldsAreEqual(object obj, FieldInfo f)
    {
        return Equals(f.GetValue(this), f.GetValue(obj));
    }

    private IEnumerable<PropertyInfo> GetProperties()
    {
        return _properties ??= GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToList();
    }

    private IEnumerable<FieldInfo> GetFields()
    {
        return _fields ??= GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .ToList();
    }
}
