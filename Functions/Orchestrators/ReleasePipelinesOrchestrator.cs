using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureDevOps.Compliance.Rules;
using Functions.Activities;
using Functions.Helpers;
using Functions.Model;
using Functions.Starters;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Response = SecurePipelineScan.VstsService.Response;

namespace Functions.Orchestrators
{
    public class ReleasePipelinesOrchestrator
    {
        private readonly EnvironmentConfig _config;

        public ReleasePipelinesOrchestrator(EnvironmentConfig config) => _config = config;

        [Function(nameof(ReleasePipelinesOrchestrator))]
        public async Task RunAsync(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var (project, scanDate) = 
                context.GetInput<(Response.Project, DateTime)>();
           
            var releasePipelines =
                await context.CallActivityAsync<List<Response.ReleaseDefinition>>(
                nameof(GetReleasePipelinesActivity), project.Id, RetryHelper.ActivityRetryOptions());

            var data = new ItemsExtensionData
            {
                Id = project.Name,
                Date = context.CurrentUtcDateTime,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(
                    _config, project.Name, RuleScopes.ReleasePipelines),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(
                    _config, project.Id),
                Reports = await Task.WhenAll(releasePipelines.Select(r =>
                    StartScanActivityAsync(context, r, project)))
            };

            await context.CallActivityAsync(nameof(UploadExtensionDataActivity),
                (releasePipelines: data, RuleScopes.ReleasePipelines));
        }

        private static Task<ItemExtensionData> StartScanActivityAsync(TaskOrchestrationContext context,
                Response.ReleaseDefinition r, Response.Project project) =>
            context.CallActivityAsync<ItemExtensionData>(
                nameof(ScanReleasePipelinesActivity), (project, r), RetryHelper.ActivityRetryOptions());
    }
}