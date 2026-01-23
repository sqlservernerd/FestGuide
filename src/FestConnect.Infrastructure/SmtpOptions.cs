using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace FestConnect.Infrastructure;

/// <summary>
/// Configuration options for SMTP email sending.
/// </summary>
public class SmtpOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Smtp";

    /// <summary>
    /// Gets or sets the SMTP server host.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP server port.
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Gets or sets the username for SMTP authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for SMTP authentication.
    /// This value is sensitive and must not be stored in source control or in appsettings.json.
    /// Use secure configuration providers such as user secrets, environment variables, or a secret store.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender email address.
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender display name.
    /// </summary>
    public string FromName { get; set; } = "FestConnect";

    /// <summary>
    /// Gets or sets whether to use SSL/TLS.
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// Gets or sets whether email sending is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the base URL for the application (used for generating links in emails).
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Returns a string representation of the SMTP options with sensitive data redacted.
    /// </summary>
    public override string ToString()
    {
        return $"SmtpOptions {{ Host = {Host}, Port = {Port}, Username = {Username}, Password = [REDACTED], FromAddress = {FromAddress}, FromName = {FromName}, UseSsl = {UseSsl}, Enabled = {Enabled}, BaseUrl = {BaseUrl} }}";
    }
}
