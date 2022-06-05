namespace MoneroBot.Daemon.Features;

using System.Text.RegularExpressions;
using Fider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public record GetDonationAddressComments(int PostNumber);

public record DonationAddressComment(int CommentId, string Address);

public interface IGetDonationAddressCommentsHandler
{
    public Task<List<DonationAddressComment>> HandleAsync(GetDonationAddressComments request, CancellationToken token = default);
}

public static class DonationAddressTextRegexes
{
    public static readonly Regex LegacyText = new(
        @"^donate\s+to\s+the\s+address\s+below\s+to\s+fund\s+this\s+bounty\s+(?<address>[123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz]{95})\s",
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

    public static readonly Regex WithPaymentHref = new(
        @"^donate\s+to\s+the\s+address\s+below\s+to\s+fund\s+this\s+bounty\s+\[(?<address>[123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz]{95})\]",
        RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
}

public class GetDonationAddressCommentsHandler : IGetDonationAddressCommentsHandler
{
    private readonly IFiderApiClient fider;
    private readonly ILogger<GetDonationAddressCommentsHandler> logger;
    private readonly DaemonOptions options;

    public GetDonationAddressCommentsHandler(ILogger<GetDonationAddressCommentsHandler> logger, IOptions<DaemonOptions> options,  IFiderApiClient fider)
    {
        this.logger = logger;
        this.fider = fider;
        this.options = options.Value;
    }

    public async Task<List<DonationAddressComment>> HandleAsync(GetDonationAddressComments request, CancellationToken token = default)
    {
        var post = await this.fider.GetPostAsync(request.PostNumber, token);
        var comments = await this.fider.ListCommentsAsync(post.Number, post.CommentsCount, token);

        var addresses = new List<DonationAddressComment>();
        foreach (var comment in comments)
        {
            if (comment.User.Id != this.options.FiderMoneroBotUserId)
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
                .FirstOrDefault(m =>
                    m is {Success: true} match
                    && match.Groups.ContainsKey("address")
                    && match.Groups["address"].Success);
            if (match?.Groups["address"].Value is { } addr)
            {
                var address = new DonationAddressComment(CommentId: comment.Id, Address: addr);
                addresses.Add(address);
            }
            else
            {
                this.logger.LogError(
                    "Failed to parse and extract the donation address from the comment {@Comment} which is expected to be a donation address comment for post {@Post} given that it contained the phrase 'donate' and was made by the monero bot",
                    comment,
                    post);
            }
        }

        return addresses;
    }
}
