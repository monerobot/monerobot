namespace MoneroBot.Fider;

using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Models;

public class FiderApiClient : IFiderApiClient
{
    private readonly HttpClient http;
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    public FiderApiClient(HttpClient http)
    {
        this.http = http;
    }

    public async Task<Post[]> GetPostsAsync(int count, CancellationToken token = default)
    {
        var response = await this.http.GetAsync(Endpoints.ListPosts(view: "recent", limit: count), token);
        return await this.ProcessResponseAsync<Post[]>(response, token);
    }

    public async Task<Post> GetPostAsync(int number, CancellationToken token = default)
    {
        var response = await this.http.GetAsync(Endpoints.GetPost(number: number), token);
        return await this.ProcessResponseAsync<Post>(response, token);
    }

    public async Task<bool> DoesPostExistAsync(int number, CancellationToken token = default)
    {
        var response = await this.http.GetAsync(Endpoints.GetPost(number: number), token);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<Post?> GetLatestPostAsync(CancellationToken token = default)
    {
        var posts = await this.GetPostsAsync(count: 1, token);
        return posts.FirstOrDefault();
    }

    public async Task<Comment[]> ListCommentsAsync(int postNumber, int number, CancellationToken token = default)
    {
        var response = await this.http.GetAsync(Endpoints.ListComments(postNumber, number));
        return await this.ProcessResponseAsync<Comment[]>(response, token);
    }

    public async Task<int> PostCommentAsync(int postNumber, string content, ImageAttachment[] attachments, CancellationToken token = default)
    {
        var data = new StringContent(
            JsonSerializer.Serialize(new { content, attachments }, this.jsonOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        var response = await this.http.PostAsync(Endpoints.AddComment(number: postNumber), data);
        var node = await this.ProcessResponseAsync<JsonNode>(response, token);
        if (node["id"] is { } prop && prop.GetValue<int?>() is { } id)
        {
            return id;
        }

        throw new HttpRequestException("Malformed response");
    }

    public async Task UpdateCommentAsync(int postNumber, int commentId, string content, CancellationToken token = default)
    {
        var data = new StringContent(
            JsonSerializer.Serialize(new { content }, this.jsonOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        var response = await this.http.PutAsync(Endpoints.EditComment(number: postNumber, id: commentId), data);
        await this.ProcessResponseAsync<JsonNode>(response, token);
    }

    public async Task UpdateCommentAsync(int postNumber, int commentId, string content, ImageAttachment[] attachments, CancellationToken token = default)
    {
        var data = new StringContent(
            JsonSerializer.Serialize(new { content, attachments }, this.jsonOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        var response = await this.http.PutAsync(Endpoints.EditComment(number: postNumber, id: commentId), data);
        await this.ProcessResponseAsync<JsonNode>(response, token);
    }

    public async Task DeleteCommentAsync(int postNumber, int commentId, CancellationToken token = default)
    {
        var response = await this.http.DeleteAsync(Endpoints.DeleteComment(postNumber, commentId), token);
        response.EnsureSuccessStatusCode();
    }

    private async Task<T> ProcessResponseAsync<T>(HttpResponseMessage response, CancellationToken token = default)
    {
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStreamAsync(token);
        var result = await JsonSerializer.DeserializeAsync<T>(content, this.jsonOptions, cancellationToken: token);
        if (result is null)
        {
            throw new JsonException();
        }

        return result;
    }

    public async Task EditPostAsync(int postNumber, string title, string? description, CancellationToken token = default) 
    {
        var data = new StringContent(
            JsonSerializer.Serialize(
                new { title, description }),
                Encoding.UTF8,
                MediaTypeNames.Application.Json);
        var response = await this.http.PutAsync(Endpoints.EditPost(postNumber), data);
        response.EnsureSuccessStatusCode();
    }
}
