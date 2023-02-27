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

using DocumentValidation.Providers;
using DocumentValidation.Xml;
using Json.Schema;

namespace DocumentValidation.Validators;

public class JsonDocumentValidator : IzzDocumentValidator
{
    private readonly ISchema _provider;

    public JsonDocumentValidator(CimJsonProvider provider)
    {
        _provider = provider;
    }

    public override DocumentFormat HandledFormat => DocumentFormat.CimJson;

    public override Task<T?> GetSchemaAsync<T>(string businessProcessType, string version)
        where T : default
    {
        var schemaName = _provider.GetSchemaLocation(businessProcessType, version);

        if (schemaName == null)
        {
            return Task.FromResult(default(T));
        }

        return (Task<T?>)(object)LoadSchemaWithDependentSchemasAsync<JsonSchema>(schemaName);
    }

    public override Task<ValidationResult> ValidateAsync(Stream document, DocumentType type)
    {
        throw new NotImplementedException();
    }

    protected override Task<T?> LoadSchemaWithDependentSchemasAsync<T>(string location)
        where T : default
    {
        var schema = JsonSchema.FromFile(location);
        return Task.FromResult((T?)(object)schema);
    }
}
