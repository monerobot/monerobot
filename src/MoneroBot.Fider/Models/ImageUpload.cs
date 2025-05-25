namespace MoneroBot.Fider.Models;

public record ImageUpload(string BlobKey, ImageUploadData? Upload, bool Remove);

public record ImageUploadData(string FileName, string ContentType, byte[] Content);
