using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TraSuaAppWeb.Pages
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet(string? returnUrl = "/")
        {
            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("display_name");
            return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
        }
    }
}