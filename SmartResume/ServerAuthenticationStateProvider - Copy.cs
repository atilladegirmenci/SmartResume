using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace SmartResume
{
    // Bu sınıf sadece sunucu tarafı (Server-Side) render sırasında çalışır.
    // Token tarayıcıda olduğu için sunucu kullanıcıyı tanıyamaz.
    // O yüzden sunucuya "Kullanıcı yok" (Anonim) diyoruz.
    public class ServerAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // Boş bir ClaimsPrincipal = Giriş yapmamış kullanıcı
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            return Task.FromResult(new AuthenticationState(anonymous));
        }
    }
}