using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using ErrorOr;
using Microsoft.AspNetCore.Components.Authorization;
using Throw;

namespace presentation.Authentication;

public class CognitoAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string ConfigurationAuthSectionKey = "Auth";
    private const string ConfigurationAuthAuthorityKey = "Authority";
    private const string ConfigurationAuthClientIdKey = "ClientId";

    private const string UserInfoKey = "user_info";
    private const string RefreshTokenKey = "refresh_token";

    public async Task<LocalStorageUserInfoModel?> UserInfo() => await
        _localStorageService.GetItemAsync<LocalStorageUserInfoModel>(UserInfoKey);

    private readonly string _authority;
    private readonly string _clientId;

    public string GetLoginRedirectUrl(string redirectUri)
    {
        return $"{_authority}/login?" +
               $"response_type=code&" +
               $"client_id={_clientId}&" +
               $"redirect_uri={redirectUri}&" +
               $"state=STATE&" +
               $"scope=openid+profile";
    }

    private readonly ILogger<CognitoAuthenticationStateProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorageService;

    public CognitoAuthenticationStateProvider(
        ILogger<CognitoAuthenticationStateProvider> logger,
        IConfiguration configuration,
        HttpClient httpClient,
        ILocalStorageService localStorageService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _localStorageService = localStorageService;

        var authSection = configuration.GetSection(ConfigurationAuthSectionKey);
        var authority = authSection.GetValue<string>(ConfigurationAuthAuthorityKey);
        authority.ThrowIfNull().IfEmpty().IfWhiteSpace();
        _authority = authority;
        var clientId = authSection.GetValue<string>(ConfigurationAuthClientIdKey);
        clientId.ThrowIfNull().IfWhiteSpace().IfEmpty();
        _clientId = clientId;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var userInfoFromLocalStorage =
                await _localStorageService.GetItemAsync<LocalStorageUserInfoModel>(UserInfoKey);
            if (userInfoFromLocalStorage is null)
            {
                _logger.LogInformation("No user infos found in local storage");
                return await ReturnNotAuthenticated();
            }

            var accessToken = userInfoFromLocalStorage.AccessToken;
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogInformation("Access token from local storage is empty");
                return await ReturnNotAuthenticated();
            }

            if (DateTime.UtcNow > userInfoFromLocalStorage.AccessTokenExpirationDateUtc)
            {
                _logger.LogInformation("Token has expired. Trying to refresh");
                var refreshToken = await _localStorageService.GetItemAsStringAsync(RefreshTokenKey);
                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    _logger.LogInformation("No refresh_token found in local storage");
                    return await ReturnNotAuthenticated();
                }

                var refresh = await Refresh(refreshToken);
                if (refresh.IsError)
                {
                    _logger.LogInformation("Failed to refresh token");
                    return await ReturnNotAuthenticated();
                }

                var tokenResponseModel = refresh.Value;
                userInfoFromLocalStorage = await SetLocalStorage(
                    refreshToken,
                    tokenResponseModel.AccessToken,
                    tokenResponseModel.ExpiresIn,
                    userInfoFromLocalStorage.UserId,
                    userInfoFromLocalStorage.Username,
                    userInfoFromLocalStorage.Email
                );

                _logger.LogInformation("Token has been refreshed");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, userInfoFromLocalStorage.Username),
                new Claim(ClaimTypes.Email, userInfoFromLocalStorage.Email),
                new Claim(ClaimTypes.NameIdentifier, userInfoFromLocalStorage.UserId),
                new Claim("access_token", userInfoFromLocalStorage.AccessToken)
            };
            var identity = new ClaimsIdentity(claims, "Server authentication");
            _logger.LogInformation("User authenticated: {UserInfo}", userInfoFromLocalStorage);
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch (HttpRequestException e)
        {
            _logger.LogError(e, "Get authentication state failed");
            return await ReturnNotAuthenticated();
        }
    }

    private async Task<AuthenticationState> ReturnNotAuthenticated()
    {
        await _localStorageService.RemoveItemAsync(UserInfoKey);
        await _localStorageService.RemoveItemAsync(RefreshTokenKey);
        var identity = new ClaimsIdentity();
        _logger.LogInformation("User failed to authenticate");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private async Task<LocalStorageUserInfoModel> SetLocalStorage(
        string refreshToken,
        string accessToken,
        long accessTokenExpiresInSeconds,
        string userId,
        string username,
        string email)
    {
        var accessTokenExpirationUtc = DateTime.UtcNow.Add(TimeSpan.FromSeconds(accessTokenExpiresInSeconds));
        var localStorageUserInfo = new LocalStorageUserInfoModel(
            accessToken,
            accessTokenExpirationUtc,
            userId,
            username,
            email
        );

        await _localStorageService.SetItemAsync(UserInfoKey, localStorageUserInfo);
        await _localStorageService.SetItemAsStringAsync(RefreshTokenKey, refreshToken);
        return localStorageUserInfo;
    }

    #region ApiCalls

    public async Task<ErrorOr<Success>> Login(string authorisationCode, string redirectUri)
    {
        var request = new HttpRequestMessage();
        request.Method = HttpMethod.Post;
        request.RequestUri = new Uri($"{_authority}/oauth2/token");
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("code", authorisationCode),
            new KeyValuePair<string, string>("redirect_uri", redirectUri)
        });

        var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode == false)
        {
            _logger.LogError("Login api call failed with status code {StatusCode}", response.StatusCode);
            return Error.Failure();
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var tokenResponseModel = JsonSerializer.Deserialize<TokenResponseModel>(jsonResponse);
        if (tokenResponseModel is null)
        {
            _logger.LogError("Failed to deserialize json response. {JsonResponse}", jsonResponse);
            return Error.Failure();
        }

        if (string.IsNullOrWhiteSpace(tokenResponseModel.RefreshToken))
        {
            _logger.LogError("RefreshToken is empty");
            return Error.Failure();
        }

        if (string.IsNullOrWhiteSpace(tokenResponseModel.AccessToken))
        {
            _logger.LogError("AccessToken is empty");
            return Error.Failure();
        }

        var userInfoResult = await GetUserInfo(tokenResponseModel.AccessToken);
        if (userInfoResult.IsError)
        {
            return userInfoResult.FirstError;
        }

        await SetLocalStorage(
            tokenResponseModel.RefreshToken,
            tokenResponseModel.AccessToken,
            tokenResponseModel.ExpiresIn,
            userInfoResult.Value.Sub,
            userInfoResult.Value.PreferredUsername,
            userInfoResult.Value.Email
        );
        return Result.Success;
    }

    private async Task<ErrorOr<TokenResponseModel>> Refresh(string refreshToken)
    {
        var request = new HttpRequestMessage();
        request.Method = HttpMethod.Post;
        request.RequestUri = new Uri($"{_authority}/oauth2/token");
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode == false)
        {
            _logger.LogError("Refresh api call failed with status code {StatusCode}", response.StatusCode);
            return Error.Failure();
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var tokenResponseModel = JsonSerializer.Deserialize<TokenResponseModel>(jsonResponse);
        if (tokenResponseModel is null)
        {
            _logger.LogError("Failed to deserialize json response. {JsonResponse}", jsonResponse);
            return Error.Failure();
        }

        return tokenResponseModel;
    }

    private async Task<ErrorOr<UserInfoResponseModel>> GetUserInfo(string accessToken)
    {
        var request = new HttpRequestMessage();
        request.Method = HttpMethod.Get;
        request.RequestUri = new Uri($"{_authority}/oauth2/userInfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode == false)
        {
            _logger.LogError("Login api call failed with status code {StatusCode}", response.StatusCode);
            return Error.Failure();
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var userInfoResponseModel = JsonSerializer.Deserialize<UserInfoResponseModel>(jsonResponse);
        if (userInfoResponseModel is null)
        {
            _logger.LogError("Failed to deserialize json response. {JsonResponse}", jsonResponse);
            return Error.Failure();
        }

        return userInfoResponseModel;
    }

    public async Task<ErrorOr<Success>> Revoke()
    {
        var token = await _localStorageService.GetItemAsStringAsync(RefreshTokenKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogInformation("No token found in local storage");
            return Error.Failure();
        }

        var request = new HttpRequestMessage();
        request.Method = HttpMethod.Post;
        request.RequestUri = new Uri($"{_authority}/oauth2/revoke");
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("token", token)
        });

        var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode == false)
        {
            _logger.LogError("Revoke api call failed with status code {StatusCode}", response.StatusCode);
            return Error.Failure();
        }

        await _localStorageService.RemoveItemAsync(UserInfoKey);
        await _localStorageService.RemoveItemAsync(RefreshTokenKey);
        return Result.Success;
    }

    #endregion
}