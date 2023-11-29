using application;
using Blazored.LocalStorage;
using infrastructure;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using presentation.Authentication;
using presentation.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddHttpClient();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CognitoAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(s =>
    s.GetRequiredService<CognitoAuthenticationStateProvider>());

builder.Services.AddInfrastructure();
builder.Services.AddApplication();

await builder.Build().RunAsync();