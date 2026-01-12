using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeSpokedBikes.Pages;

public class LegalModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("Terms");
}