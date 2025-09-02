using System;

namespace EliteDangerousCompanionAPI
{
    public interface IOAuth2Request : IDisposable
    {
        string AuthURL { get; }
        OAuth2 GetAuth();
    }
}
