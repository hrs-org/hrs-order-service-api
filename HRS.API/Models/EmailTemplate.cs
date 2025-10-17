namespace HRS.API.Models;

public class EmailTemplate
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;
    public Uri? ButtonUrl { get; set; }
    public string AdditionalInfo { get; set; } = string.Empty;
    public string FooterText { get; set; } = string.Empty;
}
