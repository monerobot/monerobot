namespace MoneroBot.Fider;

using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MoneroBot.Fider.Models;

public class FiderApiClient : IFiderApiClient
{
    private readonly HttpClient http;
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    public FiderApiClient(HttpClient http)
    {
        this.http = http;
    }

    public async Task<FiderResponse<List<Post>>> GetPostsAsync(int count, CancellationToken token = default)
    {
        var response = await this.http.GetAsync(Endpoints.ListPosts(view: "recent", limit: count), token);
        return await this.ProcessResponseAsync<List<Post>>(response, token);
    }

    public async Task<FiderResponse<Post>> GetPostAsync(int number, CancellationToken token = default)
    {
        var response = await this.http.GetAsync(Endpoints.GetPost(number: number), token);
        return await this.ProcessResponseAsync<Post>(response, token);
    }

    public async Task<FiderResponse<Post?>> GetLatestPostAsync(CancellationToken token = default)
    {
        var posts = await this.GetPostsAsync(count: 1, token);
        return posts switch
        {
            { Result: { } ps } => FiderResponse<Post?>.Ok(ps.FirstOrDefault()),
            { Error: { } err } => FiderResponse<Post?>.Err(err),
            _ => throw new NotImplementedException()
        };
    }

    public async Task<FiderResponse<int>> PostCommentAsync(int postNumber, string content, List<ImageUpload> attachments, CancellationToken token = default)
    {
        var data = new StringContent(
            JsonSerializer.Serialize(new { content, attachments }, this.jsonOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        var response = await this.http.PostAsync(Endpoints.AddComment(number: postNumber), data);
        var result = await this.ProcessResponseAsync<JsonNode>(response, token);
        return result switch
        {
            { Result: { } node } => node["id"] is { } prop && prop.GetValue<int?>() is { } id
                ? FiderResponse<int>.Ok(id)
                : FiderResponse<int>.Err(new MalformedResponse()),
            { Error: { } err } => FiderResponse<int>.Err(err),
            _ => throw new NotImplementedException()
        };
    }

    public async Task<FiderResponse<Unit>> UpdateCommentAsync(int postNumber, int commentId, string content, CancellationToken token = default)
    {
        var data = new StringContent(
            JsonSerializer.Serialize(new { content }, this.jsonOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);
        var response = await this.http.PutAsync(Endpoints.EditComment(number: postNumber, id: commentId), data);
        var result = await this.ProcessResponseAsync<JsonNode>(response, token);
        return result switch
        {
            { Result: { } node } => FiderResponse<Unit>.Ok(new Unit()),
            { Error: { } err } => FiderResponse<Unit>.Err(err),
            _ => throw new NotImplementedException()
        };
    }

    private async Task<FiderResponse<T>> ProcessResponseAsync<T>(HttpResponseMessage response, CancellationToken token = default)
    {
        if (response.StatusCode is HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStreamAsync(token);
            var result = await JsonSerializer.DeserializeAsync<T>(content, this.jsonOptions, cancellationToken: token);
            return result switch
            {
                null => FiderResponse<T>.Err(new MalformedResponse()),
                { } value => FiderResponse<T>.Ok(value)
            };
        }
        else if (response.StatusCode is HttpStatusCode.BadRequest)
        {
            var content = await response.Content.ReadAsStreamAsync(token);
            var errorSet = await JsonSerializer.DeserializeAsync<ErrorSet>(content, this.jsonOptions, cancellationToken: token);
            return FiderResponse<T>.Err(new BadRequest(errorSet?.Errors ?? Array.Empty<Error>()));
        }
        else if (response.StatusCode is HttpStatusCode.Forbidden)
        {
            return FiderResponse<T>.Err(new Forbidden());
        }
        else if (response.StatusCode is HttpStatusCode.NotFound)
        {
            return FiderResponse<T>.Err(new NotFound());
        }
        else if (response.StatusCode is HttpStatusCode.InternalServerError)
        {
            return FiderResponse<T>.Err(new InternalError());
        }
        
        return FiderResponse<T>.Err(new UnkownError(response.StatusCode, await response.Content.ReadAsStringAsync(token)));
    }
}
