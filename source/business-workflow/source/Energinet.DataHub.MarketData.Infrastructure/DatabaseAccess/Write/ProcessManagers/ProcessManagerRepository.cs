// // Copyright 2020 Energinet DataHub A/S
// //
// // Licensed under the Apache License, Version 2.0 (the "License2");
// // you may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// //     http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.

using System;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Application;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketData.Infrastructure.DatabaseAccess.Write.ProcessManagers
{
    public class ProcessManagerRepository : IProcessManagerRepository
    {
        private readonly IWriteDatabaseContext _databaseContext;

        public ProcessManagerRepository(IWriteDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        public async Task<IProcessManager> GetAsync(ProcessId processManagerId)
        {
            var processManagerDataModel = await _databaseContext.ProcessManagerDataModels.SingleAsync(x => x.ProcessId == processManagerId.Value);

            switch (processManagerDataModel.Type)
            {
                case nameof(ChangeOfSupplierProcessManager):
                    return new ChangeOfSupplierProcessManager(processManagerDataModel.ProcessId, processManagerDataModel.State, processManagerDataModel.EffectiveDate);

                default:
                    throw new NotImplementedException(processManagerDataModel.Type);
            }
        }

        public void Add(IProcessManager processManager)
        {
            var processManagerDataModel = new ProcessManagerDataModel
            {
                Id = Guid.NewGuid(),
                State = processManager.State,
                EffectiveDate = processManager.EffectiveDate,
                ProcessId = processManager.ProcessId?.Value,
                Type = processManager.GetType().Name,
            };

            _databaseContext.ProcessManagerDataModels.Add(processManagerDataModel);
        }

        public async Task SaveAsync(IProcessManager processManager)
        {
            if (string.IsNullOrEmpty(processManager.ProcessId?.Value))
            {
                throw new ArgumentException(nameof(processManager.ProcessId));
            }

            var processManagerDataModel = await _databaseContext.ProcessManagerDataModels.SingleAsync(x => x.ProcessId == processManager.ProcessId.Value);

            processManagerDataModel.State = processManager.State;
            processManagerDataModel.EffectiveDate = processManager.EffectiveDate;
        }
    }
}
