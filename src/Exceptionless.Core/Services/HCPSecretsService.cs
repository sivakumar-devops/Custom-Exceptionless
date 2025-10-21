using System.Net.Http.Headers;
using System.Text;
using Exceptionless.Core.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

public class HcpSecretsService
{
    private readonly HttpClient _httpClient;
    private readonly HcpSettings _settings;

    public HcpSecretsService(HttpClient httpClient, IOptions<HcpSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

	public async Task<string> GetAccessTokenAsync()
	{
		var form = new Dictionary<string, string>
		{
			["client_id"] = _settings.ClientId,
			["client_secret"] = _settings.ClientSecret,
			["grant_type"] = "client_credentials",
            ["audience"] = "https://api.hashicorp.cloud"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://auth.idp.hashicorp.com/oauth2/token")
        {
            Content = new FormUrlEncodedContent(form)
        };

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

		var content = await response.Content.ReadAsStringAsync();
		var token = JsonConvert.DeserializeObject<HcpTokenResponse>(content);

		if (token?.AccessToken == null)
			throw new InvalidOperationException("Access token is null or invalid.");

		return token.AccessToken;
	}

    public async Task<string> GetSecretAsync()
    {
        var token = await GetAccessTokenAsync();
        var url = $"https://api.cloud.hashicorp.com/secrets/2023-11-28/organizations/{_settings.OrgId}/projects/{_settings.ProjectId}/apps/{_settings.AppName}/secrets/{_settings.SecretName}:open";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return content;
    }
}
