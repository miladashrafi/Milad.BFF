using Duende.Bff;

namespace Milad.BFF;

class FrontendHostReturnUrlValidator : IReturnUrlValidator
{
    public Task<bool> IsValidAsync(string returnUrl)
    {
        var uri = new Uri(returnUrl);
        return Task.FromResult(uri.Host == "localhost" && (uri.Port == 5004 || uri.Port == 5002 || uri.Port == 5003));
    }
}