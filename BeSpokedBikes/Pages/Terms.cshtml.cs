using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeSpokedBikes.Pages;

[AllowAnonymous]
public class TermsModel : PageModel
{
    public string PageName => "Terms";
    public void OnGet() { }
}