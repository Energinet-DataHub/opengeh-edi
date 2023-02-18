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

using Application.Actors;
using Application.Configuration.Commands;
using Application.Configuration.TimeEvents;
using Application.Transactions.Aggregations;
using Application.Transactions.Aggregations.HourlyConsumption;
using Application.Transactions.MoveIn;
using Application.Transactions.MoveIn.MasterDataDelivery;
using Application.Transactions.MoveIn.Notifications;
using Application.Transactions.UpdateCustomer;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using StartTransaction = Application.Transactions.Aggregations.HourlyConsumption.StartTransaction;

namespace Infrastructure.Configuration.InternalCommands;

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
    }

    private static InternalCommandMapper CreateInternalCommandMap()
    {
        var mapper = new InternalCommandMapper();
        mapper.Add("CreateActor", typeof(CreateActor));
        mapper.Add("FetchCustomerMasterData", typeof(FetchCustomerMasterData));
        mapper.Add("FetchMeteringPointMasterData", typeof(FetchMeteringPointMasterData));
        mapper.Add("ForwardMeteringPointMasterData", typeof(ForwardMeteringPointMasterData));
        mapper.Add("SetCurrentKnownCustomerMasterData", typeof(SetCurrentKnownCustomerMasterData));
        mapper.Add("SendCustomerMasterDataToGridOperator", typeof(SendCustomerMasterDataToGridOperator));
        mapper.Add("NotifyCurrentEnergySupplier", typeof(NotifyCurrentEnergySupplier));
        mapper.Add("NotifyGridOperator", typeof(NotifyGridOperator));
        mapper.Add("SetConsumerHasMovedIn", typeof(SetConsumerHasMovedIn));
        mapper.Add("UpdateCustomerMasterData", typeof(UpdateCustomerMasterData));
        mapper.Add("StartTransaction", typeof(Application.Transactions.Aggregations.StartTransaction));
        mapper.Add("RetrieveAggregationResult", typeof(RetrieveAggregationResult));
        mapper.Add("Aggregations.HourlyConsumption.PrepareTransactions", typeof(PrepareTransactions));
        mapper.Add("Aggregations.HourlyConsumption.StartTransaction", typeof(StartTransaction));
        mapper.Add("Aggregations.SendAggregationResult", typeof(SendAggregationResult));
        mapper.Add("Aggregations.RetrieveAggregationResults", typeof(RetrieveAggregationResults));

        return mapper;
    }
}
