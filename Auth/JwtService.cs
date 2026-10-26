using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using _20263RecetasApi.Models;

namespace _20263RecetasApi.Auth
{
    public interface IJwtService
    {
        (string Token, DateTime ExpiraUtc) GenerarToken(
            Usuario usuario, IEnumerable<string> roles);
    }

    public class JwtService : IJwtService
    {
        private readonly JwtSettings _settings;

        public JwtService(IOptions<JwtSettings> options)
        {
            _settings = options.Value;
        }

        public (string Token, DateTime ExpiraUtc) GenerarToken(
            Usuario usuario, IEnumerable<string> roles)
        {
            // Se usan ClaimTypes.* para que User.FindFirstValue(ClaimTypes.NameIdentifier)
            // y [Authorize(Roles = ...)] funcionen sin configuración extra.
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.Id),
                new(ClaimTypes.Name,
                    usuario.UserName ?? usuario.Email ?? usuario.Id),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (!string.IsNullOrEmpty(usuario.Email))
                claims.Add(new Claim(ClaimTypes.Email, usuario.Email));

            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var llave = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_settings.Key));
            var credenciales = new SigningCredentials(
                llave, SecurityAlgorithms.HmacSha256);

            var ahora = DateTime.UtcNow;
            var expira = ahora.AddMinutes(_settings.ExpirationMinutes);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: ahora,
                expires: expira,
                signingCredentials: credenciales);

            return (new JwtSecurityTokenHandler().WriteToken(token), expira);
        }
    }
}
