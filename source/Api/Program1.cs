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
//
// using System;
// using System.Threading.Tasks;
// using BuildingBlocks.Application.Configuration.Logging;
// using BuildingBlocks.Application.Extensions.DependencyInjection;
// using Energinet.DataHub.Core.App.FunctionApp.Extensions.DependencyInjection;
// using Energinet.DataHub.EDI.Api.Configuration.Middleware;
// using Energinet.DataHub.EDI.Api.Configuration.Middleware.Authentication;
// using Energinet.DataHub.EDI.Api.Configuration.Middleware.Correlation;
// using Energinet.DataHub.EDI.Api.Extensions.DependencyInjection;
// using Energinet.DataHub.EDI.ArchivedMessages.Application.Extensions.DependencyInjection;
// using Energinet.DataHub.EDI.Common.DateTime;
// using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
// using Energinet.DataHub.EDI.IncomingMessages.Application.Extensions.DependencyInjection;
// using Energinet.DataHub.EDI.IntegrationEvents.Application.Configuration;
// using Energinet.DataHub.EDI.MasterData.Application.Extensions.DependencyInjection;
// using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.DependencyInjection;
// using Energinet.DataHub.EDI.Process.Application.Extensions.DependencyInjection;
// using Microsoft.ApplicationInsights.Extensibility;
// using Microsoft.Azure.Functions.Worker;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using Microsoft.IdentityModel.Protocols;
// using Microsoft.IdentityModel.Protocols.OpenIdConnect;
// using Microsoft.IdentityModel.Tokens;
//
// //
// // private static async Task<TokenValidationParameters> GetTokenValidationParametersAsync(IConfiguration configuration)
// // {
// //     var tenantId = Environment.GetEnvironmentVariable("B2C_TENANT_ID") ?? throw new InvalidOperationException("B2C tenant id not found.");
// //     var audience = Environment.GetEnvironmentVariable("BACKEND_SERVICE_APP_ID") ?? throw new InvalidOperationException("Backend service app id not found.");
// //     var metaDataAddress = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";
// //     var openIdConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(metaDataAddress, new OpenIdConnectConfigurationRetriever());
// //     var stsConfig = await openIdConfigurationManager.GetConfigurationAsync().ConfigureAwait(false);
// //     return new TokenValidationParameters
// //     {
// //         ValidateAudience = true,
// //         ValidateIssuer = true,
// //         ValidateIssuerSigningKey = true,
// //         ValidateLifetime = true,
// //         RequireSignedTokens = true,
// //         ClockSkew = TimeSpan.Zero,
// //         ValidAudience = audience,
// //         IssuerSigningKeys = stsConfig.SigningKeys,
// //         ValidIssuer = stsConfig.Issuer,
// //     };
// // }
// // var tokenValidationParameters = GetTokenValidationParametersAsync()
//
// var host = new HostBuilder()
//     .ConfigureFunctionsWorkerDefaults(
//         worker =>
//         {
//             worker.UseMiddleware<UnHandledExceptionMiddleware>();
//             worker.UseMiddleware<CorrelationIdMiddleware>();
//             worker.UseMiddleware<MarketActorAuthenticatorMiddleware>();
//         },
//         option =>
//         {
//             option.EnableUserCodeException = true;
//         })
//     .ConfigureServices(
//         (context, services) =>
//         {
//             services.AddApplicationInsights()
//                 .ConfigureFunctionsApplicationInsights()
//                 .AddSingleton<ITelemetryInitializer, EnrichExceptionTelemetryInitializer>()
//                 .AddDataRetention()
//                 .AddCorrelation(context.Configuration)
//                 .AddLiveHealthCheck()
//                 .AddExternalDomainServiceBusQueuesHealthCheck(
//                     runtime.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_MANAGE!,
//                     runtime.EDI_INBOX_MESSAGE_QUEUE_NAME!,
//                     runtime.WHOLESALE_INBOX_MESSAGE_QUEUE_NAME!)
//                 .AddSqlServerHealthCheck(context.Configuration);
//
//             // WAT DO
//             services.AddB2BAuthentication();
//
//             services.AddBlobStorageHealthCheck("edi-web-jobs-storage", runtime.AzureWebJobsStorage!);
//             services.AddBlobStorageHealthCheck("edi-documents-storage", runtime.AZURE_STORAGE_ACCOUNT_URL!);
//
//             services
//                 .AddIntegrationEventModule()
//                 .AddArchivedMessagesModule(context.Configuration)
//                 .AddIncomingMessagesModule(context.Configuration)
//                 .AddOutgoingMessagesModule(context.Configuration)
//                 .AddProcessModule(context.Configuration)
//                 .AddMasterDataModule(context.Configuration)
//                 .AddDataAccessModule(context.Configuration);
//
//             CompositionRoot.Initialize(services)
//                 .AddSystemClock(new SystemDateTimeProvider());
//         })
//     .Build();
//
// host.RunAsync();
