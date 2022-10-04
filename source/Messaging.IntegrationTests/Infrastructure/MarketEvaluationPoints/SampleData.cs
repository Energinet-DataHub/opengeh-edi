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

namespace Messaging.IntegrationTests.Infrastructure.MarketEvaluationPoints;

public static class SampleData
{
    public static string MeteringPointId => "A6E2367B-E555-43B1-9F04-C43FC02BA470";

    public static string MeteringPointNumber => "571234567891234568";

    public static Guid GridOperatorId => Guid.Parse("08358C14-F1E8-4202-A98E-B1CB94576C74");
}
