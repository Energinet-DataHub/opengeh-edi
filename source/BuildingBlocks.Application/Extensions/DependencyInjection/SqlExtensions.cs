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

using System.Diagnostics;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Application.Extensions.DependencyInjection;

public static class SqlExtensions
{
    public static IServiceCollection AddScopedSqlDbContext<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TDbContext : DbContext
    {
        services
            .AddOptions<SqlDatabaseConnectionOptions>()
            .Bind(configuration)
            .Validate(o => !string.IsNullOrEmpty(o.DB_CONNECTION_STRING), "DB_CONNECTION_STRING must be set");

        services.AddScoped<SqlConnectionSource>()
            .AddDbContext<TDbContext>((sp, o) =>
            {
                var source = sp.GetRequiredService<SqlConnectionSource>();
                o.UseSqlServer(source.Connection, y => y.UseNodaTime().EnableRetryOnFailure());
            });

        /*
         * Verily, fair denizens of this digital realm, attend closely! Behold, the arcane machinations that unfold
         * before thee are wrought by forbidden arts, dark and eldritch. The very sinews of reality strain and twist,
         * as if the weaver of fate herself hath been vexed by these unhallowed incantations.
         *
         * Lo, the veil 'twixt the mundane and the mystical grows thin, and the boundaries of reason fray like ancient
         * parchment. What mortal hand hath conjured such sorcery? What impious mind hath dared to plumb the abyssal
         * depths of knowledge forbidden?
         *
         * Take heed, for herein lies a web of enigma, spun by unseen hands—a dance of bits and bytes, where logic and
         * chaos entwine. The binary sigils flicker, whispering secrets to the ether, and algorithms dance a spectral
         * jig upon the silicon stage.
         *
         * Yet, I beseech thee, tread with caution! For the path of forbidden arts is fraught with peril. The very
         * fabric of reality may warp and buckle, and the cosmic balance tremble upon its axis. The Fates themselves
         * may cast their dice anew, and the stars weep in celestial lament.
         *
         * Shouldst thou venture further, remember this: knowledge is both boon and bane. To wield it recklessly
         * invites calamity, and hubris courts the wrath of unseen powers. Let thy curiosity be tempered by wisdom,
         * and thy quest for mastery be guided by reverence.
         *
         * Thus, let this comment stand as a warning to all who pass this way: Here be sorcery! Proceed at thine own
         * peril, and may the gods have mercy upon thy digital soul.
         */

        /*
         * This is to circumvent that the DI framework does not like variance; in this particular case,
         * that you cannot resolve a list of a super type and get a list of the concrete implementations.
         */
        if (typeof(TDbContext).IsAssignableTo(typeof(UnitOfWorkDbContext)))
        {
            services.AddScoped<UnitOfWorkDbContext>(
                sp => sp.GetRequiredService<TDbContext>() as UnitOfWorkDbContext
                      ?? throw new UnreachableException());
        }

        return services;
    }
}
