using System.Text.Json.Serialization;

namespace presentation.Authentication;

public record TokenResponseModel(
    [property: JsonPropertyName("access_token")]
    string AccessToken,
    [property: JsonPropertyName("id_token")]
    string IdToken,
    [property: JsonPropertyName("refresh_token")]
    string RefreshToken,
    [property: JsonPropertyName("token_type")]
    string TokenType,
    [property: JsonPropertyName("expires_in")]
    long ExpiresIn
)
{
    public DateTime ExpirationUtc { get; set; }
};