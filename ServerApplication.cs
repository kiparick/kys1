using Microsoft.AspNetCore.Mvc.Testing;

internal class ServerApplication : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureWebHost(webBuilder =>
        {
            webBuilder.UseEnvironment("Development");
        });

        return base.CreateHost(builder);
    }
}