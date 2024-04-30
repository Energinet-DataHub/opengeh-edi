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

using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation.CimXml;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation.Ebix;

namespace Energinet.DataHub.EDI.Tests.Fixtures;

public class DocumentValidationFixture
{
    public DocumentValidator Validator { get; } = new(new[]
    {
        new CimXmlValidator(new CimXmlSchemaProvider(new CimXmlSchemas())) as IValidator,
        new EbixValidator(new EbixSchemaProvider()) as IValidator,
    });
}
