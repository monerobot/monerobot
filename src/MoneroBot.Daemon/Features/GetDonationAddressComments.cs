namespace MoneroBot.Daemon.Features;

using Microsoft.Extensions.Logging;
using MoneroBot.Fider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public record GetDonationAddressComments(int PostNumber);

public record DonationAddressComment(int CommentId, string Address, string Content);

public interface IGetDonationAddressCommentsHandler
{
    public Task<List<DonationAddressComment>> HandleAsync(GetDonationAddressComments request, CancellationToken token = default);
}

public static class DonationAddressTextRegexes
{
    public static readonly Regex LegacyText = new Regex(
        @"^donate\s+to\s+the\s+address\s+below\s+to\s+fund\s+this\s+bounty\s+(?<address>[123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz]{95})\s",
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

    public static readonly Regex WithPaymentHref = new Regex(
        @"^donate\s+to\s+the\s+address\s+below\s+to\s+fund\s+this\s+bounty\s+\[(?<address>[123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz]{95})\]",
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
}

public class GetDonationAddressCommentsHandler : IGetDonationAddressCommentsHandler
{
    private const string FIDER_MONERO_BOT_USERNAME = "Monero Bounties Bot";
    private const string FIDER_ADMIN_ROLE = "administrator";
    private readonly IFiderApiClient fider;
    private readonly ILogger<GetDonationAddressCommentsHandler> logger;

    public GetDonationAddressCommentsHandler(ILogger<GetDonationAddressCommentsHandler> logger, IFiderApiClient fider)
    {
        this.logger = logger;
        this.fider = fider;
    }

    public async Task<List<DonationAddressComment>> HandleAsync(GetDonationAddressComments request, CancellationToken token = default)
    {
        var post = await this.fider.GetPostAsync(request.PostNumber, token);
        var comments = await this.fider.ListCommentsAsync(post.Number, post.CommentsCount);

        var addresses = new List<DonationAddressComment>();
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

            if (comment.Content.Contains("donate", StringComparison.OrdinalIgnoreCase) is false)
            {
                continue;
            }

            var regexes = new[]
            {
                DonationAddressTextRegexes.LegacyText,
                DonationAddressTextRegexes.WithPaymentHref,
            };
            var match = regexes
                .Select(r => r.Match(comment.Content))
                .Where(m => m is { } match
                    && match.Success
                    && match.Groups.ContainsKey("address")
                    && match.Groups["address"].Success)
                .FirstOrDefault();
            if (match is not null && match.Groups["address"].Value is { } addr)
            {
                var address = new DonationAddressComment(CommentId: comment.Id, Address: addr, Content: comment.Content);
                addresses.Add(address);
            }
            else
            {
                this.logger.LogError(
                    "Failed to parse and extract the donation address from the comment {@comment} which is expected to be a donation address comment for post {@post} given that it contained the phrase 'donate' and was made by the monero bot.",
                    comment,
                    post);
            }
        }

        return addresses;
    }
}
