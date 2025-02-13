using Artemis.Core;
using Artemis.WebClient.Workshop.Exceptions;
using IdentityModel.Client;

namespace Artemis.WebClient.Workshop.Services;

internal class AuthenticationToken
{
    public AuthenticationToken(TokenResponse tokenResponse)
    {
        if (tokenResponse.AccessToken == null)
            throw new ArtemisWebClientException("Token response contains no access token");
        if (tokenResponse.RefreshToken == null)
            throw new ArtemisWebClientException("Token response contains no refresh token");

        AccessToken = tokenResponse.AccessToken;
        RefreshToken = tokenResponse.RefreshToken;
        ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
    }

    public DateTimeOffset ExpiresAt { get; private set; }
    public bool Expired => DateTimeOffset.UtcNow.AddSeconds(5) >= ExpiresAt;

    public string AccessToken { get; private set; }
    public string RefreshToken { get; private set; }
}