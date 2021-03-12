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

using System;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Application.ChangeOfSupplier;
using Energinet.DataHub.Ingestion.Application.TimeSeries;
using Energinet.DataHub.Ingestion.Infrastructure.MessageQueue;
using GreenEnergyHub.Json;
using GreenEnergyHub.Queues;
using GreenEnergyHub.Queues.AzureServiceBus;
using GreenEnergyHub.Queues.AzureServiceBus.Integration.ServiceCollection;
using GreenEnergyHub.Queues.Kafka;
using GreenEnergyHub.Queues.ValidationReportDispatcher;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.Ingestion.Asynchronous.AzureFunction.Configuration
{
    internal static class MessageQueuesConfiguration
    {
        internal static void AddTimeSeriesMessageQueue(this IServiceCollection services)
        {
            services.AddSingleton<ITimeSeriesMessageQueueDispatcher>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var kaftaConfiguration = new KafkaConfiguration
                {
                    BoostrapServers = configuration.GetValue<string>("TIMESERIES_QUEUE_URL"),
                    SaslMechanism = configuration.GetValue<string>("KAFKA_SASL_MECHANISM"),
                    SaslUsername = configuration.GetValue<string>("KAFKA_USERNAME"),
                    SaslPassword = configuration.GetValue<string>("TIMESERIES_QUEUE_CONNECTION_STRING"),
                    SecurityProtocol = configuration.GetValue<string>("KAFKA_SECURITY_PROTOCOL"),
                    SslCaLocation =
                        Environment.ExpandEnvironmentVariables(configuration.GetValue<string>("KAFKA_SSL_CA_LOCATION")),
                    MessageTimeoutMs = configuration.GetValue<int>("KAFKA_MESSAGE_TIMEOUT_MS"),
                    MessageSendMaxRetries = configuration.GetValue<int>("KAFKA_MESSAGE_SEND_MAX_RETRIES"),
                };
                string messageQueueTopic = configuration.GetValue<string>("TIMESERIES_QUEUE_TOPIC");
                return new TimeSeriesMessageQueueDispatcher(
                    new KafkaDispatcher(new KafkaProducerFactory(kaftaConfiguration)),
                    sp.GetRequiredService<IJsonSerializer>(),
                    messageQueueTopic);
            });
        }

        internal static IServiceCollection AddServiceBusSupport(this IServiceCollection services)
        {
            services.AddServiceBusQueueDispatcher();
            return services;
        }

        internal static IServiceCollection AddMarketDataMessageQueue(this IServiceCollection services)
        {
            services.AddSingleton<IMarketDataMessageQueueDispatcher>(sp =>
            {
                var configuration = sp.GetService<IConfiguration>();
                string marketDataQueueName = configuration.GetValue<string>("MARKET_DATA_QUEUE_NAME");
                var serviceBusConfiguration = new ServiceBusConfiguration()
                {
                    ConnectionString = configuration.GetValue<string>("MARKET_DATA_QUEUE_CONNECTION_STRING"),
                };
                var serviceBusClientFactory = new ServiceBusClientFactory(serviceBusConfiguration);
                var serviceBusQueueDispatcher = new ServiceBusQueueDispatcher(serviceBusClientFactory);

                return new MarketDataMessageQueueDispatcher(
                    serviceBusQueueDispatcher,
                    sp.GetRequiredService<IMessageEnvelopeFactory>(),
                    marketDataQueueName);
            });
            return services;
        }

        internal static IServiceCollection AddChargeQueue(this IServiceCollection services)
        {
            services.AddSingleton<IChangeOfChargePostOfficeQueueDispatcher>(sp =>
            {
                var configuration = sp.GetService<IConfiguration>();
                string chargeQueueName = configuration.GetValue<string>("CHARGE_QUEUE_NAME");

                var serviceBusConfiguration = new ServiceBusConfiguration()
                {
                    ConnectionString = configuration.GetValue<string>("CHARGE_QUEUE_CONNECTION_STRING"),
                };
                var serviceBusClientFactory = new ServiceBusClientFactory(serviceBusConfiguration);
                var serviceBusQueueDispatcher = new ServiceBusQueueDispatcher(serviceBusClientFactory);

                return new ChangeOfChargePostOfficeQueueDispatcher(
                    serviceBusQueueDispatcher,
                    sp.GetRequiredService<IMessageEnvelopeFactory>(),
                    chargeQueueName);
            });
            return services;
        }

        internal static void AddValidationReportQueue(this IServiceCollection services)
        {
            services.AddSingleton<IValidationReportQueueDispatcher>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var kaftaConfiguration = new KafkaConfiguration()
                {
                    BoostrapServers = configuration.GetValue<string>("VALIDATION_REPORTS_URL"),
                    SaslMechanism = configuration.GetValue<string>("KAFKA_SASL_MECHANISM"),
                    SaslUsername = configuration.GetValue<string>("KAFKA_USERNAME"),
                    SaslPassword = configuration.GetValue<string>("VALIDATION_REPORTS_CONNECTION_STRING"),
                    SecurityProtocol = configuration.GetValue<string>("KAFKA_SECURITY_PROTOCOL"),
                    SslCaLocation =
                        Environment.ExpandEnvironmentVariables(configuration.GetValue<string>("KAFKA_SSL_CA_LOCATION")),
                    MessageTimeoutMs = configuration.GetValue<int>("KAFKA_MESSAGE_TIMEOUT_MS"),
                    MessageSendMaxRetries = configuration.GetValue<int>("KAFKA_MESSAGE_SEND_MAX_RETRIES"),
                };
                string validationReportTopic = configuration.GetValue<string>("VALIDATION_REPORTS_QUEUE_TOPIC");
                return new ValidationReportQueueDispatcher(
                    new KafkaDispatcher(new KafkaProducerFactory(kaftaConfiguration)),
                    sp.GetRequiredService<IJsonSerializer>(),
                    validationReportTopic);
            });
        }
    }
}
