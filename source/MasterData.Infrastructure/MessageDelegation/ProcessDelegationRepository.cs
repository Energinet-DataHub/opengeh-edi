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

using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.MasterData.Domain.ProcessDelegations;
using Energinet.DataHub.EDI.MasterData.Infrastructure.DataAccess;

namespace Energinet.DataHub.EDI.MasterData.Infrastructure.MessageDelegation;

public class ProcessDelegationRepository : IProcessDelegationRepository
{
    private readonly MasterDataContext _masterDataContext;

    public ProcessDelegationRepository(MasterDataContext masterDataContext)
    {
        _masterDataContext = masterDataContext;
    }

    public void Create(ProcessDelegation processDelegation, CancellationToken cancellationToken)
    {
        _masterDataContext.ProcessDelegations.Add(processDelegation);
    }
}