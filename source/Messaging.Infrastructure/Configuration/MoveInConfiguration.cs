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
using Messaging.Application.Configuration.TimeEvents;
using Messaging.Application.IncomingMessages.RequestChangeOfSupplier;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Application.Transactions.MoveIn.MasterDataDelivery;
using Messaging.Application.Transactions.MoveIn.Notifications;
using Messaging.Application.Transactions.MoveIn.UpdateCustomer;
using Messaging.Domain.Transactions.MoveIn.Events;
using Messaging.Infrastructure.Transactions.MoveIn;
using Microsoft.Extensions.DependencyInjection;

namespace Messaging.Infrastructure.Configuration;

internal static class MoveInConfiguration
{
    public static void Configure(IServiceCollection services, MoveInSettings settings)
    {
        services.AddScoped<MoveInNotifications>();
        services.AddScoped<IMoveInRequester, MoveInRequester>();
        services.AddScoped<IMeteringPointMasterDataClient, MeteringPointMasterDataClient>();
        services.AddScoped<ICustomerMasterDataClient, CustomerMasterDataClient>();
        services.AddTransient<IRequestHandler<RequestChangeOfSupplierTransaction, Unit>, MoveInRequestHandler>();
        services.AddTransient<IRequestHandler<FetchCustomerMasterData, Unit>, FetchCustomerMasterDataHandler>();
        services.AddTransient<IRequestHandler<FetchMeteringPointMasterData, Unit>, FetchMeteringPointMasterDataHandler>();
        services.AddTransient<IRequestHandler<SetConsumerHasMovedIn, Unit>, SetConsumerHasMovedInHandler>();
        services.AddTransient<IRequestHandler<ForwardMeteringPointMasterData, Unit>, ForwardMeteringPointMasterDataHandler>();
        services.AddTransient<IRequestHandler<SendCustomerMasterDataToEnergySupplier, Unit>, SendCustomerMasterDataToEnergySupplierHandler>();
        services.AddTransient<IRequestHandler<NotifyCurrentEnergySupplier, Unit>, NotifyCurrentEnergySupplierHandler>();
        services.AddTransient<IRequestHandler<NotifyGridOperator, Unit>, NotifyGridOperatorHandler>();
        services.AddTransient<IRequestHandler<SendCustomerMasterDataToGridOperator, Unit>, SendCustomerMasterDataToGridOperatorHandler>();
        services.AddTransient<IRequestHandler<ReceiveCustomerMasterData, Unit>, ReceiveCustomerMasterDataHandler>();
        services.AddTransient<IRequestHandler<UpdateCustomerMasterData, Unit>, UpdateCustomerMasterDataHandler>();
        services.AddTransient<INotificationHandler<MoveInWasAccepted>, FetchMeteringPointMasterDataWhenAccepted>();
        services.AddTransient<INotificationHandler<MoveInWasAccepted>, FetchCustomerMasterDataWhenAccepted>();
        services.AddTransient<INotificationHandler<EndOfSupplyNotificationChangedToPending>, NotifyCurrentEnergySupplierWhenConsumerHasMovedIn>();
        services.AddTransient<INotificationHandler<CustomerMasterDataWasReceived>, SendCustomerMasterDataToEnergySupplierWhenDataIsReceived>();
        services.AddTransient<INotificationHandler<BusinessProcessWasCompleted>, NotifyGridOperatorWhenConsumerHasMovedIn>();
        services.AddTransient<INotificationHandler<ADayHasPassed>, DispatchCustomerMasterDataForGridOperatorWhenGracePeriodHasExpired>();
        services.AddTransient<CustomerMasterDataMessageFactory>();
        services.AddSingleton(settings);
    }
}
