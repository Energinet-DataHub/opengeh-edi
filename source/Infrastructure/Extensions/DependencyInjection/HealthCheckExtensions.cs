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

using System;
using System.Diagnostics.CodeAnalysis;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.Infrastructure.Extensions.DependencyInjection;

public static class HealthCheckExtensions
{
    public static void AddBlobStorageHealthCheck(this IServiceCollection services, string name, string blobConnectionString)
    {
        services.AddHealthChecks().AddAzureBlobStorage(blobConnectionString, name: name);
    }

    public static IServiceCollection AddBlobStorageHealthCheck(this IServiceCollection services, string name, Uri storageAccountUri)
    {
        services.AddHealthChecks().AddAzureBlobStorage(storageAccountUri, new DefaultAzureCredential(), name: name);

        return services;
    }
}
