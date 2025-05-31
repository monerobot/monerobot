namespace MoneroBot.Fider;

using Microsoft.AspNetCore.WebUtilities;

internal static class Endpoints
{
    public static string GetPost(int number) => $"/api/v1/posts/{number}";

    public static string ListPosts(string? query = null, string? view = null, int? limit = null, string? tags = null)
    {
        var @params = new Dictionary<string, string?>();
        if (query is not null)
        {
            @params.Add("query", query);
        }

        if (view is not null)
        {
            @params.Add("view", view);
        }

        if (limit is not null)
        {
            @params.Add("limit", limit.Value.ToString());
        }

        if (tags is not null)
        {
            @params.Add("tags", string.Join(",", tags));
        }

        return QueryHelpers.AddQueryString("/api/v1/posts", @params);
    }

    public static string ListComments(int number, int? count)
    {
        var @params = new Dictionary<string, string?>();
        if (count is not null)
        {
            @params.Add("number", count.Value.ToString());
        }

        return QueryHelpers.AddQueryString($"/api/v1/posts/{number}/comments", @params);
    }

    public static string AddComment(int number) => $"/api/v1/posts/{number}/comments";

    public static string EditComment(int number, int id) => $"/api/v1/posts/{number}/comments/{id}";

    public static string DeleteComment(int number, int id) => $"/api/v1/posts/{number}/comments/{id}";

    public static string EditPost(int number) => $"/api/v1/posts/{number}";
}
