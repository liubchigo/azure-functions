using System;
using System.Net.Http;
using AzureDevOps.Compliance.Rules;
using Functions.Helpers;
//using Functions.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Security;

namespace Functions
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWebApplication()
                .ConfigureServices((context, services) =>
                {
                    RegisterServices(services);
                })
                .Build();

            host.Run();
        }

        private static void RegisterServices(IServiceCollection services)
        {
            var token = GetEnvironmentVariable("TOKEN");
            var organization = GetEnvironmentVariable("ORGANIZATION");

            services.AddSingleton<IVstsRestClient>(new VstsRestClient(organization, token));
            services.AddSingleton<IMemoryCache>(_ => new MemoryCache(new MemoryCacheOptions()));

            var config = new EnvironmentConfig
            {
                ExtensionName = GetEnvironmentVariable("EXTENSION_NAME"),
                ExtensionPublisher = GetEnvironmentVariable("EXTENSION_PUBLISHER"),
                Organization = organization,
                FunctionAppHostname = GetEnvironmentVariable("WEBSITE_HOSTNAME"),
            };

            services.AddSingleton(config);
            services.AddSingleton<ITokenizer>(new Tokenizer(GetEnvironmentVariable("EXTENSION_SECRET")));

            services.AddDefaultRules();
            services.AddSingleton(new HttpClient());

            services.AddSingleton<IPoliciesResolver, PoliciesResolver>();
        }

        private static string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process)
                   ?? throw new ArgumentNullException(name,
                       $"Please provide a valid value for environment variable '{name}'");
        }
    }
}