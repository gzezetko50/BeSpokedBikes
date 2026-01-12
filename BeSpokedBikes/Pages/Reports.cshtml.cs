using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeSpokedBikes.Models;
using BeSpokedBikes.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BeSpokedBikes.Pages;

public class ReportsModel : PageModel
{
    private readonly ApiClient _api;

    public ReportsModel(ApiClient api)
    {
        _api = api;
    }

    public string PageName => "Quarterly Commission Report";

    [BindProperty(SupportsGet = true)]
    public int? Year { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Quarter { get; set; }

    public List<QuarterlyCommissionRow> Rows { get; set; } = new();

    public async Task OnGetAsync()
    {
        var now = DateTime.Now;
        var year = Year ?? now.Year;
        var quarter = Quarter ?? ((now.Month - 1) / 3 + 1);

        var (start, end) = GetQuarterRange(year, quarter);

        // Use existing ApiClient to fetch sales in range
        var sales = await _api.GetSalesAsync(DateOnly.FromDateTime(start), DateOnly.FromDateTime(end))
                              ?? new List<Sale>();

        // Aggregate per salesperson
        Rows = sales
            .GroupBy(s => new { s.SalespersonId, s.SalespersonFirstName, s.SalespersonLastName })
            .Select(g =>
            {
                // API may populate commission in different properties; prefer commissionAmount then commission
                decimal totalCommission = g.Sum(x => (x.CommissionAmount != 0m ? x.CommissionAmount : x.Commission));
                decimal totalSales = g.Sum(x => x.TotalPrice);
                return new QuarterlyCommissionRow(
                    Salesperson: $"{g.Key.SalespersonFirstName} {g.Key.SalespersonLastName}".Trim(),
                    Year: year,
                    Quarter: quarter,
                    SalesCount: g.Count(),
                    TotalSales: totalSales,
                    TotalCommission: totalCommission
                );
            })
            .OrderByDescending(r => r.TotalCommission)
            .ToList();

        Year = year;
        Quarter = quarter;
    }

    private static (DateTime start, DateTime end) GetQuarterRange(int year, int quarter)
    {
        if (quarter < 1 || quarter > 4) quarter = 1;
        var startMonth = (quarter - 1) * 3 + 1;
        var start = new DateTime(year, startMonth, 1);
        var end = start.AddMonths(3).AddDays(-1);
        return (start, end);
    }

    public record QuarterlyCommissionRow(
        string Salesperson,
        int Year,
        int Quarter,
        int SalesCount,
        decimal TotalSales,
        decimal TotalCommission
    );
}