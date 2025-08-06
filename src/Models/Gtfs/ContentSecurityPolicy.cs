namespace TransitGtfsApi.Models;

public class ContentSecurityPolicy
{
    public string[] DefaultSrc { get; set; } = Array.Empty<string>();
    public string[] ScriptSrc { get; set; } = Array.Empty<string>();
    public string[] StyleSrc { get; set; } = Array.Empty<string>();
    public string[] ImgSrc { get; set; } = Array.Empty<string>();
    public string[] ConnectSrc { get; set; } = Array.Empty<string>();
    public string[] FrameAncestors { get; set; } = Array.Empty<string>();
    public string[] FormAction { get; set; } = Array.Empty<string>();
    public string[] BaseUri { get; set; } = Array.Empty<string>();
    public string[] ObjectSrc { get; set; } = Array.Empty<string>();
}