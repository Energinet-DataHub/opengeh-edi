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

using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.TimeEvents;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Commands;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Commands;
using Energinet.DataHub.EDI.Process.Domain.Commands;
using Energinet.DataHub.EDI.Process.Infrastructure.InternalCommands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.Process.Application.Extensions.DependencyInjection;

internal static class InternalCommandExtensions
{
    internal static IServiceCollection AddInternalCommands(this IServiceCollection services)
    {
        services.AddSingleton(CreateInternalCommandMap())
            .AddTransient<CommandExecutor>()
            .AddScoped<ICommandScheduler, CommandScheduler>()
            .AddScoped<CommandSchedulerFacade>()
            .AddTransient<InternalCommandAccessor>()
            .AddTransient<InternalCommandProcessor>()
            .AddTransient<INotificationHandler<TenSecondsHasHasPassed>, ProcessInternalCommandsOnTimeHasPassed>()
            .AddTransient<IDataRetention, InternalCommandsRetention>();

        return services;
    }

    private static InternalCommandMapper CreateInternalCommandMap()
    {
        var mapper = new InternalCommandMapper();
        mapper.Add("GridAreaOwnershipAssigned", typeof(GridAreaOwnershipAssignedDto));
        mapper.Add("SendAggregatedMeasureDataRequestToWholesale", typeof(SendAggregatedMeasureDataRequestToWholesale));
        mapper.Add("AcceptedAggregatedTimeSeries", typeof(AcceptedEnergyResultTimeSeriesCommand));
        mapper.Add("RejectedAggregatedTimeSeries", typeof(RejectedAggregatedTimeSeries));
        mapper.Add("SendWholesaleServicesRequestToWholesale", typeof(SendWholesaleServicesRequestToWholesale));
        mapper.Add("RejectedWholesaleServices", typeof(RejectedWholesaleServices));
        mapper.Add("AcceptedWholesaleServicesSerieCommand", typeof(AcceptedWholesaleServices));

        return mapper;
    }
}
