using System.Text;
using System.Text.Json;
using Tranzor.Interfaces.Http;
using Tranzor.Models.GraphQL;
using Tranzor.Models.OTP;

namespace Tranzor.Services.Http;

public class OtpHttpClient : IOtpHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OtpHttpClient> _logger;
    private readonly string? _otpEndpoint;

    public OtpHttpClient(HttpClient httpClient, ILogger<OtpHttpClient> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _otpEndpoint = Environment.GetEnvironmentVariable("OTP_URL");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("OTPTimeout", "180000");
        _httpClient.Timeout = TimeSpan.FromSeconds(180);
    }

    public async Task<OTPResponse?> ExecuteGraphQLQueryAsync(string query, string? operationName = null)
    {
        try
        {
            if (string.IsNullOrEmpty(_otpEndpoint))
            {
                _logger.LogError("OTP API Url is not configured.");
                throw new ArgumentException($"OTP API Url is not configured.");
            }

            GraphQLRequest request = new GraphQLRequest
            {
                Query = query
            };

            if (!string.IsNullOrWhiteSpace(operationName))
            {
                request.OperationName = operationName;
            }

            string jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            StringContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending GraphQL request to OTP: {Query}", query);

            HttpResponseMessage response = await _httpClient.PostAsync(_otpEndpoint, httpContent);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OTP API returned {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                throw new HttpRequestException($"OTP API error: {response.StatusCode}");
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Received OTP response: {Response}", responseContent);

            OTPResponse? otpResponse = JsonSerializer.Deserialize<OTPResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            return otpResponse;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing OTP response");
            throw new InvalidOperationException("Failed to parse OTP response", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling OTP API");
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling OTP API");
            throw new TimeoutException("OTP API request timed out", ex);
        }
    }
}