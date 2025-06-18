namespace App.Models;

//singleton
public class DisplayMedia_Info
{
    public readonly List<string> ImageFormats = [".jpg", ".jpeg", ".png", ".webp", ".svg"];
    public readonly List<string> VideoFormats =
    [".mp4", ".mov", ".wmv", ".webm", ".avi", ".mp3", ".wav", ".ogg", ".aac", ".aiff", ".alac", ".m4a"];
}

public class DisplayMediaModel
{
    public string Category { get; set; } = string.Empty;
    public string Src { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string VideoPoster { get; set; } = string.Empty;
}