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

using System.IO;
using Energinet.DataHub.EDI.Application.ArchivedMessages;
using Energinet.DataHub.EDI.Application.SearchMessages;
using Energinet.DataHub.EDI.Domain.ArchivedMessages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.Infrastructure.ArchivedMessages;

public static class ArchivedMessageConfiguration
{
    public static void Configure(IServiceCollection services)
    {
        services.AddTransient<IRequestHandler<GetMessagesQuery, MessageSearchResult>, GetMessagesQueryHandler>();
        services.AddScoped<IArchivedMessageRepository, ArchivedMessageRepository>();

        services.AddTransient<IRequestHandler<GetArchivedMessageDocumentQuery, Stream?>, GetArchivedMessageDocumentQueryHandler>();
    }
}
