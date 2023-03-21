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

using Domain.SeedWork;

namespace Domain.OutgoingMessages;

public class MeteringPointType : EnumerationType
{
    public static readonly MeteringPointType Consumption = new(0, nameof(Consumption));
    public static readonly MeteringPointType Production = new(1, nameof(Production));

    private MeteringPointType(int id, string name)
        : base(id, name)
    {
    }

    public static MeteringPointType From(string valueToParse)
    {
        var meteringPointType = GetAll<MeteringPointType>().FirstOrDefault(type => type.Name.Equals(valueToParse, StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidOperationException($"{valueToParse} is not a valid metering point type");

        return meteringPointType;
    }
}
