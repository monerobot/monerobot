namespace MoneroBot.Fider.Models;

using System.Text.Json.Serialization;

public record ImageAttachment
{
    [JsonPropertyName("bKey")]
    public string BlobKey { get; init; }
    public bool Remove { get; init; }
    public ImageUploadData? Upload { get; init; }

    private ImageAttachment(string blobKey, bool remove, ImageUploadData? upload)
    {
        this.BlobKey = blobKey;
        this.Remove = remove;
        this.Upload = upload;
    }

    public static ImageAttachment Removal(string blobKey) =>
        new(blobKey, remove: true, upload: null);

    public static ImageAttachment Addition(string blobKey, ImageUploadData upload) =>
        new(blobKey, remove: false, upload: upload);
}

public record ImageUploadData(string FileName, string ContentType, byte[] Content);
