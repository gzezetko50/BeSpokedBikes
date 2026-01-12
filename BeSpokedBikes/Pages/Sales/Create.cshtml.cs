using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BeSpokedBikes.Models;

namespace BeSpokedBikes.Pages.Sales;

public class CreateModel : PageModel
{
    public string PageName => "Create Sale";

    // Input model bound to the form
    public class InputModel
    {
        public int ProductId { get; set; }
        public int SalespersonId { get; set; }
        public int CustomerId { get; set; }
        public DateOnly SalesDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    // Collections used by the form selects
    public List<Product> Products { get; set; } = new();
    public List<Salesperson> Salespersons { get; set; } = new();
    public List<Customer> Customers { get; set; } = new();

    public void OnGet()
    {
        // TODO: populate Products, Salespersons and Customers from your data source / ApiClient.
        // For now ensure they are not null so the Razor view can render without compilation/runtime errors.
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            // Re-populate lists if needed before redisplaying the page
            return Page();
        }

        // TODO: create the Sale using Input values (call API / save to DB)
        return RedirectToPage("/Sales/Index");
    }
}