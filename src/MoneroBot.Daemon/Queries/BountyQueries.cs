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

    public static async Task<List<int>> GetMissingPostNumbers(MoneroBotContext ctx, CancellationToken token = default)
    {
        return await ctx
            .Set<PostNumber>()
            .FromSqlInterpolated($@"
                WITH RECURSIVE [numbers] ([number]) AS
                (
                    SELECT 1
                    UNION ALL
                    SELECT [number] + 1 FROM [numbers]
                    WHERE [number] < (SELECT MAX([PostNumber]) FROM [Bounties])
                )
                SELECT
                    [n].[number] AS [Number]
                FROM [numbers] AS [n]
                WHERE NOT EXISTS
                (
                    SELECT
                        *
                    FROM [Bounties] AS [b]
                    WHERE [b].[PostNumber] = [n].[number]
                )")
            .OrderByDescending(n => n.Number)
            .Select(n => n.Number)
            .ToListAsync(token);
    }
}
