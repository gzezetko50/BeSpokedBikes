using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BeSpokedBikes.Models;
using BeSpokedBikes.Services;
using static BeSpokedBikes.Services.ApiClient;

namespace BeSpokedBikes.Pages
{
    public class SalespersonsModel : PageModel
    {
        private readonly ApiClient _apiClient;

        public SalespersonsModel(ApiClient apiClient)
        {
            _apiClient = apiClient;
            // Ensure bound Input is never null to avoid CS8618 / null-ref
            Input = new SalespersonInputModel
            {
                FirstName = string.Empty,
                LastName = string.Empty,
                Address = string.Empty,
                Phone = string.Empty,
                Manager = string.Empty,
                StartDate = DateTime.Today,
                StatusId = 1
            };
        }

        public List<Salesperson> Salespersons { get; set; } = [];

        [BindProperty]
        public SalespersonInputModel Input { get; set; }

        public async Task OnGetAsync()
        {
            await LoadSalespersonsAsync();
        }

        private async Task LoadSalespersonsAsync()
        {
            try
            {
                var list = await _apiClient.GetSalespersonsAsync();
                Salespersons = list ?? [];
            }
            catch (System.Net.Http.HttpRequestException)
            {
                Salespersons = [];
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSalespersonsAsync();
                return Page();
            }

            var dto = ToUpsertDto(Input);
            await _apiClient.CreateSalespersonAsync(dto);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            // PROOF that this handler is hit and that ModelState is ok
            if (!ModelState.IsValid)
            {
                await LoadSalespersonsAsync();
                return Page();
            }

            var dto = ToUpsertDto(Input);
            await _apiClient.UpdateSalespersonAsync(Input.Id, dto);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            if (Input == null || Input.Id <= 0)
            {
                await LoadSalespersonsAsync();
                return Page();
            }

            await _apiClient.DeleteSalespersonAsync(Input.Id);
            return RedirectToPage();
        }

        private static SalespersonUpsertDto ToUpsertDto(SalespersonInputModel input) =>
            new(
                firstName: input.FirstName,
                lastName: input.LastName,
                startDate: DateOnly.FromDateTime(input.StartDate),
                terminationDate: input.TerminationDate.HasValue
                    ? DateOnly.FromDateTime(input.TerminationDate.Value)
                    : null,
                managerId: null,
                statusId: input.StatusId,
                addresses: string.IsNullOrWhiteSpace(input.Address)
                    ? null
                    : new[] { input.Address },
                phones: string.IsNullOrWhiteSpace(input.Phone)
                    ? null
                    : new[] { input.Phone }
            );
    }

    public class SalespersonInputModel
    {
        public int Id { get; set; }                 // set by hidden field in form
        public string FirstName { get; set; } = "";
        public string LastName  { get; set; } = "";
        public string Address   { get; set; } = "";
        public string Phone     { get; set; } = "";
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime? TerminationDate { get; set; }
        public string Manager   { get; set; } = "";
        public int StatusId     { get; set; } = 1;  // default 1, can be changed later
    }
}