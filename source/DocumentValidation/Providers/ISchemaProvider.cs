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

namespace DocumentValidation.Providers;

/// <summary>
/// Provides schemas
/// </summary>
/// <typeparam name="TSchema">Type of schema object used</typeparam>
public interface ISchemaProvider<TSchema>
{
    /// <summary>
    /// Return schema object for the specified document type
    /// </summary>
    /// <param name="details"></param>
    Task<TSchema> GetSchemaForAsync(SchemaDetails details);

    /// <summary>
    /// Return schema details for the specified document type
    /// </summary>
    /// <param name="documentName"></param>
    /// <returns><see cref="SchemaDetails"/></returns>
    SchemaDetails GetDetailsFor(string documentName);
}
