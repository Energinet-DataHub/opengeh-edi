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
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

/// <summary>
/// Used when a DataHubType can have an unknown value, typically used when a schema allows for values we no longer use
/// </summary>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Used for converting our DataHubTypes")]
public abstract class DataHubTypeWithUnknown<T> : DataHubType<T>
    where T : DataHubType<T>
{
    protected DataHubTypeWithUnknown(string name, string code, bool isUnknown)
        : base(name, code)
    {
        IsUnknown = isUnknown;
    }

    public bool IsUnknown { get; }

    /// <summary>
    /// Create a DataHubType from a code or returns an UNKNOWN type if the code is not recognized
    /// </summary>
    public static T FromCodeOrUnknown(string code)
    {
        return TryFromCode(code) ?? CreateUnknown(code);
    }

    /// <summary>
    /// Creates an instance of T. DataHubTypeWithUnknownTests verifies that all implementations can be created like this
    /// </summary>
    private static T CreateUnknown(string code)
    {
        return (T)Activator.CreateInstance(typeof(T), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { code, code, true }, null)!;
    }
}
