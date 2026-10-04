using Asoagro.Components;

var builder = WebApplication.CreateBuilder(args);

// Servicios para componentes Razor interactivos y controladores API
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();

// Registrar HttpClient para el servidor local
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:5162") });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();

// AQUÍ ESTABA EL ERROR: El nombre correcto es AddInteractiveServerRenderMode
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();