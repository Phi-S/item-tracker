namespace presentation.Authentication;

public record LocalStorageUserInfoModel(
    string AccessToken,
    DateTime AccessTokenExpirationDateUtc,
    string UserId,
    string Username,
    string Email
);