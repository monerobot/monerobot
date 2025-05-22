namespace MoneroBot.Fider;

using Models;

public interface IFiderApiClient
{
    public Task<Post> GetPostAsync(int number, CancellationToken token = default);

    public Task<List<Post>> GetPostsAsync(int count, CancellationToken token = default);

    public Task<Post?> GetLatestPostAsync(CancellationToken token = default);

    public Task<List<Comment>> ListCommentsAsync(int postNumber, int number, CancellationToken token = default);

    public Task<int> PostCommentAsync(int postNumber, string content, List<ImageUpload> attachments, CancellationToken token = default);

    public Task UpdateCommentAsync(int postNumber, int commentId, string content, List<ImageUpload> attachments, CancellationToken token = default);

    public Task UpdateCommentAsync(int postNumber, int commentId, string content, CancellationToken token = default);

    public Task DeleteCommentAsync(int postNumber, int commentId, CancellationToken token = default);

    public Task EditPostAsync(int postNumber, string title, string? description, CancellationToken token = default);
}
