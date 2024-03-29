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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Process.Application.ProcessInitializationHandlers;
using Energinet.DataHub.EDI.Process.Interfaces;

namespace Energinet.DataHub.EDI.Process.Application;

public class ProcessClient : IProcessClient
{
    private readonly IEnumerable<IProcessInitializationHandler> _processInitializationHandlers;

    public ProcessClient(IEnumerable<IProcessInitializationHandler> processInitializationHandlers)
    {
        _processInitializationHandlers = processInitializationHandlers;
    }

    public async Task InitializeAsync(string processTypeToInitialize, byte[] processInitializationData)
    {
        var processInitializationHandler = _processInitializationHandlers.Single(h => h.CanHandle(processTypeToInitialize));
        await processInitializationHandler.ProcessAsync(processInitializationData).ConfigureAwait(true);
    }
}
