namespace MoneroBot.Fider;

using MoneroBot.Fider.Models;

public interface IFiderApiClient
{
    public Task<FiderResponse<Post>> GetPostAsync(int number, CancellationToken token = default);

    public Task<FiderResponse<List<Post>>> GetPostsAsync(int count, CancellationToken token = default);

    public Task<FiderResponse<Post?>> GetLatestPostAsync(CancellationToken token = default);

    public Task<FiderResponse<int>> PostCommentAsync(int postNumber, string content, List<ImageUpload> attachments, CancellationToken token = default);

    public Task<FiderResponse<Unit>> UpdateCommentAsync(int postNumber, int commentId, string content, CancellationToken token = default);
}
