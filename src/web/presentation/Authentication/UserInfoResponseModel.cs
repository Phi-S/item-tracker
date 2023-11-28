using System.Text.Json.Serialization;

namespace presentation.Authentication;

public record UserInfoResponseModel(
    [property: JsonPropertyName("sub")] string Sub,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("nickname")]
    string Nickname,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("email_verified")]
    string EmailVerified
);