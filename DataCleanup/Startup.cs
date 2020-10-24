using AzureUtilities.DataCleanup.DataLayer;
using AzureUtilities.DataCleanup.Interfaces;
using AzureUtilities.DataCleanup.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(AzureUtilities.DataCleanup.Startup))]
namespace AzureUtilities.DataCleanup
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddScoped<IServiceLayer, ServiceLayer>();
            builder.Services.AddScoped<IEventGridManager, EventGridManager>();
            builder.Services.AddScoped<IQueueManager, QueueManager>();
            builder.Services.AddScoped<ITableManager, TableManager>();
        }
    }
}
