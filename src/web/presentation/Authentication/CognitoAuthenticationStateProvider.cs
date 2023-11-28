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

    private const string TokenKey = "token";


    public TokenResponseModel? Token { get; private set; }
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

    public CognitoAuthenticationStateProvider(ILogger<CognitoAuthenticationStateProvider> logger,
        IConfiguration configuration, HttpClient httpClient,
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

    private AuthenticationState ReturnNotAuthenticated()
    {
        Token = null;
        var identity = new ClaimsIdentity();
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorageService.GetItemAsync<TokenResponseModel>(TokenKey);
            _logger.LogInformation("Token: {Token}", token);

            if (token is null)
            {
                _logger.LogInformation("No token found in local storage");
                return ReturnNotAuthenticated();
            }

            if (DateTime.UtcNow > token.ExpirationUtc)
            {
                _logger.LogInformation("Token is expired");
                await _localStorageService.RemoveItemAsync(TokenKey);
                return ReturnNotAuthenticated();
            }

            var userInfo = await UserInfo(token.AccessToken);
            if (userInfo.IsError)
            {
                // TODO: refresh token.... 
                _logger.LogInformation("AccessToken is not valid");
                await _localStorageService.RemoveItemAsync(TokenKey);
                return ReturnNotAuthenticated();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, userInfo.Value.Nickname),
                new Claim(ClaimTypes.Email, userInfo.Value.Email),
                new Claim("sub", userInfo.Value.Sub),
                new Claim("access_token", token.AccessToken)
            };
            var identity = new ClaimsIdentity(claims, "Server authentication");
            Token = token;
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch (HttpRequestException e)
        {
            _logger.LogError(e, "Get authentication state failed");
            await _localStorageService.RemoveItemAsync(TokenKey);
            return ReturnNotAuthenticated();
        }
    }

    private async Task<ErrorOr<UserInfoResponseModel>> UserInfo(string accessToken)
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

        var currentDateTime = DateTime.UtcNow;
        tokenResponseModel.ExpirationUtc = currentDateTime.Add(TimeSpan.FromSeconds(tokenResponseModel.ExpiresIn));
        await _localStorageService.SetItemAsync(TokenKey, tokenResponseModel);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> Revoke()
    {
        var token = await _localStorageService.GetItemAsync<TokenResponseModel>(TokenKey);
        _logger.LogInformation("Token: {Token}", token);

        if (token is null)
        {
            _logger.LogInformation("No token found in local storage");
            return Error.Failure();
        }

        var request = new HttpRequestMessage();
        request.Method = HttpMethod.Post;
        request.RequestUri = new Uri($"{_authority}/oauth2/revoke");
        //request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("token", token.RefreshToken)
        });

        var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode == false)
        {
            _logger.LogError("Revoke api call failed with status code {StatusCode}", response.StatusCode);
            return Error.Failure();
        }

        await _localStorageService.RemoveItemAsync(TokenKey);
        return Result.Success;
    }
}