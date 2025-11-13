using SistemaDental.Application.DTOs.Auth;
using SistemaDental.Application.DTOs.Tenant;

namespace SistemaDental.Application.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<bool> RegisterTenantAsync(TenantCreateDto dto);
    Task<string?> ForgotPasswordAsync(ForgotPasswordDto dto);
    Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
    Task<bool> GenerateEmailVerificationTokenAsync(string email);
    Task<(bool Success, string? Reason)> VerifyEmailAsync(VerifyEmailDto dto);
}

