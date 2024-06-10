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

using System.ComponentModel.DataAnnotations;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;

/// <summary>
/// Options for the Edi delta table views in Databricks.
/// </summary>
public class EdiDatabricksOptions
{
    public const string SectionName = "EdiDatabricks";

    /// <summary>
    /// Name of the database in which the views for EDI are located.
    /// </summary>
    [Required]
    public string DatabaseName { get; set; } = "wholesale_edi_results";
}
