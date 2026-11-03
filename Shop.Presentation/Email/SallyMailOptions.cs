namespace Shop.Presentation.Email;
public sealed class SallyMailOptions
{
    public string Host { get; set; }="";
    public int Port { get; set; }=587;
    public string Username { get; set; }="";
    public string Password { get; set; }=""; // Only environment variables or user-secrets
    public string FromEmail { get; set; }="";
    public string FromName { get; set; }="SALLY";
    public string PublicBaseUrl { get; set; }=""; // Explicit public domain. Never trust request Host.
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host)&&!string.IsNullOrWhiteSpace(FromEmail)
       && !string.IsNullOrWhiteSpace(Username)&&!string.IsNullOrWhiteSpace(Password)
       && Uri.TryCreate(PublicBaseUrl,UriKind.Absolute,out var uri) && uri.Scheme==Uri.UriSchemeHttps
       && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
}
