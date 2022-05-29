namespace MoneroBot.Daemon.Features;

using Microsoft.Extensions.Logging;
using MoneroBot.Fider;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public record GetDonationComments(int PostNumber);

public record DonationComment(int CommentId, ulong Amount, ulong TotalAmount, string Content);

public interface IGetDonationCommentsHandler
{
    public Task<List<DonationComment>> HandleAsync(GetDonationComments request, CancellationToken token = default);
}

public static class DonationTextRegexes
{
    public static readonly Regex LegacyText = new Regex(
        @"^bounty\s+increased\s+by\s+(?<amount>[\d.]+)\s+XMR\s+Total\s+Bounty:\s+(?<total>[\d.]+)\s+XMR$",
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

    public static readonly Regex CurrentText = new Regex(
        @"^bounty\s+increased\s+by\s+(?<amount>[\d.]+)\s+XMR\s+[📨💰⏳]+\s+Total\s+Bounty:\s+(?<total>[\d.]+)\s+XMR$",
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
}

public class GetDonationCommentsHandler : IGetDonationCommentsHandler
{
    private const string FIDER_MONERO_BOT_USERNAME = "Monero Bounties Bot";
    private const string FIDER_ADMIN_ROLE = "administrator";
    private const decimal MONERO_TO_ATOMIC_SCALER = 1e12m;
    private readonly IFiderApiClient fider;
    private readonly ILogger<GetDonationCommentsHandler> logger;

    public GetDonationCommentsHandler(ILogger<GetDonationCommentsHandler> logger, IFiderApiClient fider)
    {
        this.logger = logger;
        this.fider = fider;
    }

    public async Task<List<DonationComment>> HandleAsync(GetDonationComments request, CancellationToken token = default)
    {
        var post = await this.fider.GetPostAsync(request.PostNumber, token);
        var comments = await this.fider.ListCommentsAsync(post.Number, post.CommentsCount);

        var donations = new List<DonationComment>();
        foreach (var comment in comments)
        {
            if (comment.User.Role is not FIDER_ADMIN_ROLE)
            {
                continue;
            }

            if (comment.User.Name is not FIDER_MONERO_BOT_USERNAME)
            {
                continue;
            }

            if (comment.Content.Contains("XMR") is false)
            {
                continue;
            }

            var regexes = new[] { DonationTextRegexes.LegacyText, DonationTextRegexes.CurrentText };
            var match = regexes
                .Select(r => r.Match(comment.Content))
                .Where(m => m is { } match
                    && match.Success
                    && match.Groups.ContainsKey("amount")
                    && match.Groups.ContainsKey("total")
                    && match.Groups["amount"].Success
                    && match.Groups["total"].Success)
                .FirstOrDefault();
            if (match is not null
                && match.Groups["amount"].Value is { } amountText
                && match.Groups["total"].Value is { } totalText
                && decimal.TryParse(amountText, out var amountXmr)
                && decimal.TryParse(totalText, out var totalXmr))
            {
                var donation = new DonationComment(
                    CommentId: comment.Id,
                    Amount: (ulong)(amountXmr * MONERO_TO_ATOMIC_SCALER),
                    TotalAmount: (ulong)(totalXmr * MONERO_TO_ATOMIC_SCALER),
                    Content: comment.Content);
                donations.Add(donation);
            }
            else
            {
                this.logger.LogError(
                    "Failed to parse and extract the amount/total from the comment {@comment} which is expected to be a donation comment for post {@post} given that it contained the phrase 'XMR' and was made by the monero bot.",
                    comment,
                    post);
            }
        }

        return donations;
    }
}
