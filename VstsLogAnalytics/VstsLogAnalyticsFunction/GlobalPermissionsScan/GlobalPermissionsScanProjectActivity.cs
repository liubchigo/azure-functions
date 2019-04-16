using System;
using System.Linq;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using VstsLogAnalytics.Client;
using VstsLogAnalyticsFunction.Model;
using Project = SecurePipelineScan.VstsService.Response.Project;
using Task = System.Threading.Tasks.Task;

namespace VstsLogAnalyticsFunction.GlobalPermissionsScan
{
    public class GlobalPermissionsScanProjectActivity
    {
        private readonly ILogAnalyticsClient _client;
        private readonly IVstsRestClient _azuredo;
        private readonly IEnvironmentConfig _azuredoConfig;
        private readonly IRulesProvider _rulesProvider;

        public GlobalPermissionsScanProjectActivity(ILogAnalyticsClient client,
            IVstsRestClient azuredo,
            IEnvironmentConfig azuredoConfig,
            IRulesProvider rulesProvider)
        {
            _client = client;
            _azuredo = azuredo;
            _azuredoConfig = azuredoConfig;
            _rulesProvider = rulesProvider;
        }

        [FunctionName(nameof(GlobalPermissionsScanProjectActivity))]
        public async Task Run(
            [ActivityTrigger] DurableActivityContextBase context,
            ILogger log)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var project = context.GetInput<Project>();

            if (project == null) throw new Exception("No Project found in parameter DurableActivityContextBase");

            await Run(project.Name, log);
        }

        [FunctionName("GlobalPermissionsScanProject")]
        public async Task Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, Route = "scan/{organization}/{project}/globalpermissions")]
            HttpRequestMessage request,
            string organization,
            string project,
            ILogger log)
        {
            await Run(project, log);
        }

        private async Task Run(string project, ILogger log)
        {
            log.LogInformation($"Creating preventive analysis log for project {project}");
            var dateTimeUtcNow = DateTime.UtcNow;

            var globalPermissionsRuleset = _rulesProvider.GlobalPermissions(_azuredo);

            var evaluatedRules = globalPermissionsRuleset.Select(r => new
            {
                scope = "globalpermissions",
                rule = r.GetType().Name,
                description = r.Description,
                status = r.Evaluate(project),
                project,
                evaluatedDate = dateTimeUtcNow
            }).ToList();

            log.LogInformation($"Writing preventive analysis log for project {project} to Log Analytics Workspace");
            foreach (var rule in evaluatedRules)
            {
                try
                {
                    await _client.AddCustomLogJsonAsync("preventive_analysis_log", new
                    {
                        rule.scope,
                        rule.rule,
                        rule.status,
                        rule.project,
                        rule.evaluatedDate
                    }, "evaluatedDate");
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $"Failed to write report to log analytics: {ex}");
                    throw;
                }
            }

            try
            {
                var extensionData = new GlobalPermissionsExtensionData
                {
                    Id = project,
                    Date = dateTimeUtcNow,
                    Reports = evaluatedRules.Select(r => new EvaluatedRule
                    {
                        Description = r.description,
                        Status = r.status,
                        ReconcileUrl =
                            $"https://{_azuredoConfig.FunctionAppHostname}/api/reconcile/{_azuredoConfig.Organisation}/{project}/globalpermissions/{r.rule}"
                    }).ToList()
                };
                _azuredo.Put(ExtensionManagement.ExtensionData<GlobalPermissionsExtensionData>("tas",
                    _azuredoConfig.ExtensionName,
                    "globalpermissions"), extensionData);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Write Extension data failed: {ex}");
                throw;
            }
        }
    }


}