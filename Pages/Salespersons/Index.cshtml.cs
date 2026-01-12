using Microsoft.AspNetCore.Mvc.RazorPages;
using BeSpokedBikes.Models;
using BeSpokedBikes.Services;

namespace BeSpokedBikes.Pages.Salespersons;

public class IndexModel(ApiClient api) : PageModel
{
    public IList<Salesperson> Salespersons { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Salespersons = await api.GetSalespersonsAsync();
    }
}