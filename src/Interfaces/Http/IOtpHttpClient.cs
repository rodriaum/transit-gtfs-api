using Tranzor.Models.OTP;

namespace Tranzor.Interfaces.Http;

public interface IOtpHttpClient
{
    Task<OTPResponse?> ExecuteGraphQLQueryAsync(string query, string? operationName = null);
}