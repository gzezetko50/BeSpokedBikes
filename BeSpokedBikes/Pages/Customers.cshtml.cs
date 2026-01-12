using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BeSpokedBikes.Models;
using BeSpokedBikes.Services;

namespace BeSpokedBikes.Pages
{
    public class CustomersModel : PageModel
    {
        private readonly ApiClient _apiClient;

        public CustomersModel(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public List<Customer> Customers { get; set; } = [];

        public async Task OnGetAsync()
        {
            try
            {
                var list = await _apiClient.GetCustomersAsync();
                Customers = list ?? [];
            }
            catch (System.Net.Http.HttpRequestException)
            {
                Customers = [];
            }
        }
    }
}