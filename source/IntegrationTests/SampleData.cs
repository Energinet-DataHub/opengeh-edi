﻿// Copyright 2020 Energinet DataHub A/S
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

namespace Energinet.DataHub.EDI.IntegrationTests;

internal static class SampleData
{
    internal static string MeteringPointNumber => "571234567891234568";

    internal static string NewEnergySupplierNumber => "5790000555551";

    internal static string TransactionId => "8BA514FA-2E4D-4CB7-8B4A-1B1137185BD7";

    internal static string MarketEvaluationPointId => "e17fe44f-ea4e-47e0-bbb0-64bfb382922a";

    internal static string OriginalMessageId => "EDE97146-C592-489A-B23A-3C73C096E368";

    internal static string ConsumerName => "John Doe";

    internal static string ReceiverId => "5790001330552";

    internal static string SenderId => "5790000555551";

    internal static Guid BusinessProcessId => Guid.Parse("F43D99F0-CE9A-4CDA-9192-4BE20C243CD5");

    internal static Guid GridOperatorId => Guid.Parse("AC509EEB-C67E-4E64-A94F-23C0C6BC4828");

    internal static string GridOperatorNumber => "5790000555551";

    internal static string? ChargeOwner => "5799999933318";
}
