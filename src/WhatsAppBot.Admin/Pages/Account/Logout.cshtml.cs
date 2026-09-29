using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WhatsAppBot.Admin.Authentication;

namespace WhatsAppBot.Admin.Pages.Account;

public sealed class LogoutModel(IAdminSession adminSession) : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        adminSession.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Account/Login");
    }
}
