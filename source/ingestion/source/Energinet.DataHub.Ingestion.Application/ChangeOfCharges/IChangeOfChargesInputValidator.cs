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

using System.Threading.Tasks;
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using GreenEnergyHub.Queues.ValidationReportDispatcher.Validation;

namespace Energinet.DataHub.Ingestion.Application.ChangeOfCharges
{
    /// <summary>
    /// Contract defining the input validator for change of charges messages.
    /// </summary>
    public interface IChangeOfChargesInputValidator
    {
        /// <summary>
        /// Input validates a <see cref="ChangeOfChargesMessage"/>.
        /// </summary>
        /// <param name="changeOfChargesMessage">The message to validate.</param>
        /// <returns>The validation result.</returns>
        Task<HubRequestValidationResult> ValidateAsync(ChangeOfChargesMessage changeOfChargesMessage);
    }
}
