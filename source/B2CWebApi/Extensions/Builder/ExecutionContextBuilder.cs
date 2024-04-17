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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.B2CWebApi.Extensions.Builder;

public static class ExecutionContextBuilder
{
    public static IApplicationBuilder UseExecutionContextMiddleware(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var executionContext = context.RequestServices.GetRequiredService<Energinet.DataHub.EDI.BuildingBlocks.Domain.ExecutionContext>();
            executionContext.SetExecutionType(ExecutionType.B2CWebApi);

            // Call the next delegate/middleware in the pipeline.
            await next(context).ConfigureAwait(false);
        });
        return app;
    }
}
