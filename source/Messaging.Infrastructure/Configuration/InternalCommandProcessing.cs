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

using MediatR;
using Messaging.Application.Actors;
using Messaging.Application.Configuration.Commands;
using Messaging.Application.Configuration.TimeEvents;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Application.Transactions.MoveIn.MasterDataDelivery;
using Messaging.Application.Transactions.MoveIn.Notifications;
using Messaging.Application.Transactions.MoveIn.UpdateCustomer;
using Messaging.Application.Transactions.UpdateCustomer;
using Messaging.Infrastructure.Configuration.InternalCommands;
using Messaging.Infrastructure.OutgoingMessages.Requesting;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging.Infrastructure.Configuration;

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
        mapper.Add("SendFailureNotification", typeof(SendFailureNotification));
        mapper.Add("SendSuccessNotification", typeof(SendSuccessNotification));
        mapper.Add("UpdateCustomerMasterData", typeof(UpdateCustomerMasterData));

        return mapper;
    }
}
