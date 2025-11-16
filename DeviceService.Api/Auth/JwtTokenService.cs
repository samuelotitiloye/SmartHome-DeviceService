using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DeviceService.Api.Auth
{
    public class JwtTokenService
    {
        private readonly JwtOptions _settings;

        public JwtTokenService(IOptions<JwtOptions> settings)
        {
            _settings = settings.Value;
            Console.WriteLine("JWT TOKEN SERVICE DEBUG:");
            Console.WriteLine($"Issuer: {_settings.Issuer}");
            Console.WriteLine($"Audience: {_settings.Audience}");
            Console.WriteLine($"Secret: {_settings.Secret}");
        }

        public string GenerateToken(string username)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: new[]
                {
                    new Claim(ClaimTypes.Name, username)
                },
                expires: DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
