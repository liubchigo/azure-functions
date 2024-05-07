using Functions.Activities;
using Functions.Model;
using System.Threading.Tasks;
using Functions.Helpers;
using Functions.Starters;
using System.Collections.Generic;
using Response = SecurePipelineScan.VstsService.Response;
using System;
using AzureDevOps.Compliance.Rules;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

namespace Functions.Orchestrators
{
    public class GlobalPermissionsOrchestrator
    {
        private readonly EnvironmentConfig _config;

        public GlobalPermissionsOrchestrator(EnvironmentConfig config) => _config = config;

        [Function(nameof(GlobalPermissionsOrchestrator))]
        public async Task RunAsync([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var (project, scanDate) = 
                context.GetInput<(Response.Project, DateTime)>();

            var data = new ItemsExtensionData
            {
                Id = project.Name,
                Date = context.CurrentUtcDateTime,
                RescanUrl = ProjectScanHttpStarter.RescanUrl(
                    _config, project.Name, RuleScopes.GlobalPermissions),
                HasReconcilePermissionUrl = ReconcileFunction.HasReconcilePermissionUrl(
                    _config, project.Id),
                Reports = new List<ItemExtensionData>
                {
                    await context.CallActivityAsync<ItemExtensionData>(nameof(
                        ScanGlobalPermissionsActivity), project, RetryHelper.ActivityRetryOptions())
                }
            };

            await context.CallActivityAsync(nameof(UploadExtensionDataActivity), 
                (permissions: data, RuleScopes.GlobalPermissions));
        }
    }
}