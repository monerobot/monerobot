namespace MoneroBot.Daemon.Repositories;

using Microsoft.EntityFrameworkCore;
using MoneroBot.Database;
using MoneroBot.Database.Entities;
using MoneroBot.Database.Entities.QueryResults;

public static class BountyQueries
{
    public static async Task<Bounty?> GetLatestBountyAsync(MoneroBotContext ctx, CancellationToken token = default)
    {
        return await ctx.Bounties
            .OrderByDescending(b => b.PostNumber)
            .FirstOrDefaultAsync(token);
    }
}
