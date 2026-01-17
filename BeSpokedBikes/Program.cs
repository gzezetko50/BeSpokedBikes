using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Razor Pages
builder.Services.AddRazorPages();

// Cookie authentication for Razor Pages
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

// Add services to the container.
// Required for token storage/attachment
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BeSpokedBikes.Services.BearerTokenHandler>();

// Typed HttpClient for the API, using base URL from configuration
builder.Services.AddHttpClient<BeSpokedBikes.Services.ApiClient>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

    var apiKey = builder.Configuration["Api:ApiKey"];
    if (!string.IsNullOrWhiteSpace(apiKey))
        client.DefaultRequestHeaders.Add("x-api-key", apiKey); // adjust header name if needed
})
.AddHttpMessageHandler<BeSpokedBikes.Services.BearerTokenHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable cookie auth before authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
