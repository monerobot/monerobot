namespace MoneroBot.Fider;

using System.ComponentModel.DataAnnotations;

public class FiderApiClientOptions
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "A Fider API key is required")]
    public string? ApiKey { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "The Fider API base address is required")]
    [Url(ErrorMessage = "Expected the Fider API base address to be a URL")]
    public string? BaseAddress { get; set; }

    public string? ImpersonationUserId { get; set; }
}
