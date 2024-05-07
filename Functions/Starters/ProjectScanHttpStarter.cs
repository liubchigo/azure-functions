using Functions.Orchestrators;
using Microsoft.Azure.Functions.Worker;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using System;
using System.Net;
using System.Threading.Tasks;
using AzureDevOps.Compliance.Rules;
using SecurePipelineScan.VstsService.Security;
using Microsoft.DurableTask.Client;
using System.Threading;
using Microsoft.Azure.Functions.Worker.Http;
using System.Linq;

namespace Functions.Starters
{
    public class ProjectScanHttpStarter
    {
        private readonly ITokenizer _tokenizer;
        private readonly IVstsRestClient _azuredo;
        private readonly IPoliciesResolver _policiesResolver;
        private const int TimeOut = 180;

        public ProjectScanHttpStarter(ITokenizer tokenizer, IVstsRestClient azuredo, IPoliciesResolver policiesResolver)
        {
            _tokenizer = tokenizer;
            _azuredo = azuredo;
            _policiesResolver = policiesResolver;
        }

        [Function(nameof(ProjectScanHttpStarter))]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "scan/{organization}/{projectName}/{scope}")]
                    HttpRequestData request, string organization, string projectName, string scope,
            [DurableClient] DurableTaskClient starter)
        {
            if (starter == null)
                throw new ArgumentNullException(nameof(starter));

            if (request.Identities == null || request.Identities.Count() == 0)
            return request.CreateResponse(HttpStatusCode.Unauthorized);

            var project = await _azuredo.GetAsync(Project.ProjectByName(projectName));
            if (project == null)
            {
                var response = request.CreateResponse(HttpStatusCode.NotFound);
                return response;
            }

            // clear cache for manual scan
            _policiesResolver.Clear(project.Id);

            var scanDate = DateTime.UtcNow;
            var instanceId = await starter.ScheduleNewOrchestrationInstanceAsync(nameof(ProjectScanOrchestrator), (project, scope, scanDate), CancellationToken.None);

            await starter.WaitForInstanceCompletionAsync(instanceId, new CancellationTokenSource(TimeOut).Token);
            return await starter.CreateCheckStatusResponseAsync(request, instanceId);
        }

        public static Uri RescanUrl(EnvironmentConfig environmentConfig, string project, string scope)
        {
            if (environmentConfig == null)
                throw new ArgumentNullException(nameof(environmentConfig));

            return new Uri($"https://{environmentConfig.FunctionAppHostname}/api/scan/{environmentConfig.Organization}/" +
                $"{project}/{scope}");
        }
    }
}