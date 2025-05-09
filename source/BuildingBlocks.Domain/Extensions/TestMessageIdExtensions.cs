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

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Extensions;

public static class TestMessageIdExtensions
{
    public const string TestUuidPrefix = "test__";

    /// <summary>
    /// Converts the guid to a shorter version, prefixed with the <see cref="TestUuidPrefix"/>.
    /// See <see cref="ToShortUuid"/> for details on how the conversion is done.
    /// </summary>
    /// <returns>A string prefixed with <see cref="TestUuidPrefix"/>, that isn't longer than the default Guid length.</returns>
    public static string ToTestMessageUuid(this Guid guid)
    {
        return $"{TestUuidPrefix}{guid.ToShortUuid()}";
    }

    public static bool IsTestUuid(this string messageId)
    {
        return messageId.StartsWith(TestUuidPrefix);
    }

    /// <summary>
    /// Convert a guid to a shorter version, while still being unique.
    /// <remarks>
    /// Converts the Guid to a base64 string, and replaces special characters.
    /// See: https://stackoverflow.com/questions/9278909/net-short-unique-identifier
    /// </remarks>
    /// <returns>A string with 22 characters, that preserves the uniqueness of the Guid</returns>
    /// </summary>
    private static string ToShortUuid(this Guid guid)
    {
        return Convert.ToBase64String(guid.ToByteArray())[0..^2] // remove trailing == padding
            .Replace('+', '-') // escape special character
            .Replace('/', '_'); // escape special character
    }
}
