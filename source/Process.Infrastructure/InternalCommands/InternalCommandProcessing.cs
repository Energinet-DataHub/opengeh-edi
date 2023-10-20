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

using Energinet.DataHub.EDI.Application.Actors;
using Energinet.DataHub.EDI.Application.Configuration.Commands;
using Energinet.DataHub.EDI.Application.Configuration.TimeEvents;
using Energinet.DataHub.EDI.Infrastructure.DataRetention;
using Energinet.DataHub.EDI.Process.Application.Transactions.Aggregations;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.Process.Infrastructure.InternalCommands;

internal static class InternalCommandProcessing
{
    internal static void Configure(IServiceCollection services)
    {
        services.AddSingleton(CreateInternalCommandMap());
        services.AddTransient<CommandExecutor>();
        services.AddScoped<ICommandScheduler, CommandScheduler>();
        services.AddScoped<CommandSchedulerFacade>();
        services.AddTransient<InternalCommandAccessor>();
        services.AddTransient<InternalCommandProcessor>();
        services.AddTransient<INotificationHandler<TenSecondsHasHasPassed>, ProcessInternalCommandsOnTimeHasPassed>();
        services.AddTransient<IDataRetention, InternalCommandsRetention>();
    }

    private static InternalCommandMapper CreateInternalCommandMap()
    {
        var mapper = new InternalCommandMapper();
        mapper.Add("CreateActor", typeof(CreateActorCommand));
        mapper.Add("Aggregations.ForwardAggregationResult", typeof(ForwardAggregationResult));
        mapper.Add("SendAggregatedMeasureRequestToWholesale", typeof(SendAggregatedMeasureRequestToWholesale));
        mapper.Add("AcceptedAggregatedTimeSerie", typeof(AcceptedAggregatedTimeSerie));
        mapper.Add("RejectedAggregatedTimeSeries", typeof(RejectedAggregatedTimeSeries));

        return mapper;
    }
}
