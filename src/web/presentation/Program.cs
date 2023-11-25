using infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using presentation.Components;
using Throw;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Oidc", options.ProviderOptions);
    options.UserOptions.NameClaim = "nickname";
});


var apiEndpoint = builder.Configuration.GetSection("ITEM_TRACKER_API_ENDPOINT").Value;
apiEndpoint.ThrowIfNull();
builder.Services.AddInfrastructure(apiEndpoint);

await builder.Build().RunAsync();