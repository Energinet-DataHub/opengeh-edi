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

using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.Wholesale.Edi.Validation;
using Energinet.DataHub.Wholesale.Edi.Validation.AggregatedTimeSeriesRequest;
using Energinet.DataHub.Wholesale.Edi.Validation.AggregatedTimeSeriesRequest.Rules;
using Energinet.DataHub.Wholesale.Edi.Validation.WholesaleServicesRequest;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.Wholesale.Edi.Extensions.DependencyInjection;

/// <summary>
/// Registration of services required for the Calculations module.
/// </summary>
public static class EdiExtensions
{
    public static void AddEdiModule(this IServiceCollection services)
    {
        // services.AddScoped<IWholesaleInboxRequestHandler, AggregatedTimeSeriesRequestHandler>(); TODO: LRN
        // services.AddScoped<IWholesaleInboxRequestHandler, WholesaleServicesRequestHandler>();
        // services.AddTransient<WholesaleServicesRequestMapper>();
        //
        // services.AddSingleton<IEdiClient, EdiClient>();
        //
        // services
        //     .AddOptions<EdiInboxQueueOptions>()
        //     .BindConfiguration(EdiInboxQueueOptions.SectionName)
        //     .ValidateDataAnnotations();
        //
        // // Health checks
        // services.AddHealthChecks()
        //     // Must use a listener connection string
        //     .AddAzureServiceBusQueue(
        //         sp => sp.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.ConnectionString,
        //         sp => sp.GetRequiredService<IOptions<EdiInboxQueueOptions>>().Value.QueueName,
        //         name: "EdiInboxQueue");
        //
        // // Validation helpers
        // services.AddTransient<PeriodValidationHelper>();
        // // Validation
        // services.AddAggregatedTimeSeriesRequestValidation();
        // services.AddWholesaleServicesRequestValidation();
    }

    public static IServiceCollection AddAggregatedTimeSeriesRequestValidation(this IServiceCollection services)
    {
        services.AddScoped<IValidator<AggregatedTimeSeriesRequest>, AggregatedTimeSeriesRequestValidator>();
        services.AddScoped<IValidationRule<AggregatedTimeSeriesRequest>, PeriodValidationRule>();
        services.AddSingleton<IValidationRule<AggregatedTimeSeriesRequest>, MeteringPointTypeValidationRule>();
        services.AddSingleton<IValidationRule<AggregatedTimeSeriesRequest>, EnergySupplierValidationRule>();
        services.AddSingleton<IValidationRule<AggregatedTimeSeriesRequest>, SettlementMethodValidationRule>();
        services.AddSingleton<IValidationRule<AggregatedTimeSeriesRequest>, TimeSeriesTypeValidationRule>();
        services.AddSingleton<IValidationRule<AggregatedTimeSeriesRequest>, BalanceResponsibleValidationRule>();
        services.AddSingleton<IValidationRule<AggregatedTimeSeriesRequest>, SettlementVersionValidationRule>();
        services.AddScoped<IValidationRule<AggregatedTimeSeriesRequest>, GridAreaValidationRule>();
        services.AddSingleton<IValidationRule<AggregatedTimeSeriesRequest>, RequestedByActorRoleValidationRule>();

        return services;
    }

    public static IServiceCollection AddWholesaleServicesRequestValidation(this IServiceCollection services)
    {
        services.AddScoped<IValidator<WholesaleServicesRequest>, WholesaleServicesRequestValidator>();
        services
            .AddSingleton<
                IValidationRule<WholesaleServicesRequest>,
                Energinet.DataHub.Wholesale.Edi.Validation.WholesaleServicesRequest.Rules.ResolutionValidationRule>()
            .AddSingleton<
                IValidationRule<WholesaleServicesRequest>,
                Energinet.DataHub.Wholesale.Edi.Validation.WholesaleServicesRequest.Rules.EnergySupplierValidationRule>()
            .AddSingleton<
                IValidationRule<WholesaleServicesRequest>,
                Energinet.DataHub.Wholesale.Edi.Validation.WholesaleServicesRequest.Rules.ChargeCodeValidationRule>()
            .AddScoped<
                IValidationRule<WholesaleServicesRequest>,
                Energinet.DataHub.Wholesale.Edi.Validation.WholesaleServicesRequest.Rules.PeriodValidationRule>()
            .AddScoped<
                IValidationRule<WholesaleServicesRequest>,
                Energinet.DataHub.Wholesale.Edi.Validation.WholesaleServicesRequest.Rules.GridAreaValidationRule>()
            .AddScoped<
                IValidationRule<WholesaleServicesRequest>,
                Energinet.DataHub.Wholesale.Edi.Validation.WholesaleServicesRequest.Rules.SettlementVersionValidationRule>();

        return services;
    }
}
