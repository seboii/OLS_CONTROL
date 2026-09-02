using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OLS.Business.Common;
using OLS.Business.Services.Authentication;
using OLS.Business.Services.Authorization;

namespace OLS.API.Controllers.Front;

/// <summary>
/// olsold: <c>App\Http\Controllers\Front\FrontLogin\FrontLoginController</c>
/// Uçlar: POST /api/v1/login, GET /api/v1/auth, POST /api/v1/logout
/// </summary>
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUser _currentUser;

    public AuthController(IAuthService authService, ICurrentUser currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    public sealed class LoginRequest
    {
        /// <summary>
        /// E-posta ya da Siber kullanıcı kodu. Alan adı <c>email</c> olarak
        /// KALIYOR: arayüz ve kaynak sözleşme bu adı kullanıyor, değiştirmek
        /// giriş isteğini kıracaktı.
        /// </summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("password")]
        public string? Password { get; set; }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        // olsold'daki Validator kuralları ve Türkçe mesajları birebir korunuyor.
        var errors = new Dictionary<string, string[]>();

        // BİÇİM KONTROLÜ YALNIZCA E-POSTAYA. Kullanıcılar Siber hesabıyla da
        // girebiliyor ve Siber kullanıcı kodu "FATIHT" gibi, e-posta değil —
        // katı e-posta doğrulaması bu girişleri daha sunucuya varmadan reddederdi.
        // "@" içeren değer e-posta sayılır ve eskisi gibi doğrulanır.
        if (string.IsNullOrWhiteSpace(request.Email))
            errors["email"] = [Translator.Get("E-mail Alanı boş olamaz")];
        else if (request.Email.Contains('@') && !new EmailAddressAttribute().IsValid(request.Email))
            errors["email"] = [Translator.Get("E-mail Alanı boş olamaz")];

        if (string.IsNullOrWhiteSpace(request.Password))
            errors["password"] = [Translator.Get("Şifre Alanı boş olamaz")];

        if (errors.Count > 0)
            return BadRequest(ApiResponse.ValidationErrors(errors));

        var result = await _authService.LoginAsync(
            request.Email!, request.Password!, cancellationToken);

        return result.Outcome switch
        {
            LoginOutcome.Inactive => Unauthorized(
                ApiResponse.Error("Bu Kullanıcı Pasif Durumdadır.")),

            LoginOutcome.InvalidCredentials => Unauthorized(
                ApiResponse.Message(Translator.Get("Kullanıcı adı ya da şifre hatalı"))),

            _ => Ok(new { user = result.User, token = result.Token }, "Giriş Başarılı"),
        };
    }

    /// <summary>
    /// olsold: <c>check_auth</c>. Frontend router guard'ı her sayfa geçişinde çağırır.
    /// Yanıt şekli <c>{ data, authenticated }</c> — zarf diğer uçlardan farklı.
    /// </summary>
    [HttpGet("auth")]
    [Authorize]
    public async Task<IActionResult> CheckAuth(CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Unauthorized(ApiResponse.Error(Translator.Get("Yetkisiz Erişim")));

        var user = await _authService.GetAuthenticatedUserAsync(userId, cancellationToken);

        return base.Ok(new AuthCheckResponse
        {
            Data = user,
            Authenticated = user is not null,
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var expClaim = User.FindFirstValue(JwtRegisteredClaimNames.Exp);

        if (jti is not null && _currentUser.Id is { } userId)
        {
            var expiresAt = long.TryParse(expClaim, out var exp)
                ? DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime
                : DateTime.UtcNow;

            await _authService.RevokeAsync(jti, userId, expiresAt, cancellationToken);
        }

        return base.Ok(new LogoutResponse
        {
            Status = true,
            Message = Translator.Get("Çıkış İşlemi Tamamlandı"),
        });
    }

    private sealed class AuthCheckResponse
    {
        [JsonPropertyName("data")]
        public object? Data { get; init; }

        [JsonPropertyName("authenticated")]
        public bool Authenticated { get; init; }
    }

    private sealed class LogoutResponse
    {
        [JsonPropertyName("status")]
        public bool Status { get; init; }

        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;
    }
}
