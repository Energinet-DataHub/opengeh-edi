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

namespace Energinet.DataHub.EDI.Domain.Common
{
    public abstract class EnumerationCodeType : EnumerationType
    {
        protected EnumerationCodeType(int id, string name, string code)
            : base(id, name)
        {
            Code = code;
        }

        public string Code { get; }

        public static T FromCode<T>(string code)
            where T : EnumerationCodeType
        {
            var matchingItem = Parse<T, string>(code, "code", item => item.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            return matchingItem;
        }

        private static T Parse<T, TValue>(TValue value, string description, Func<T, bool> predicate)
            where T : EnumerationType
        {
            var matchingItem = GetAll<T>().FirstOrDefault(predicate);

            return matchingItem ?? throw new InvalidOperationException($"'{value}' is not a valid {description} in {typeof(T)}");
        }
    }
}
