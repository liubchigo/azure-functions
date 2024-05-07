using Functions.Model;
using Response = SecurePipelineScan.VstsService.Response;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using AzureDevOps.Compliance.Rules;
using Microsoft.Azure.Functions.Worker;

namespace Functions.Activities
{
    public class ScanGlobalPermissionsActivity
    {
        private readonly EnvironmentConfig _config;
        private readonly IEnumerable<IProjectRule> _rules;

        public ScanGlobalPermissionsActivity(
            EnvironmentConfig config, IEnumerable<IProjectRule> rules)
        {
            _config = config;
            _rules = rules;
        }

        [Function(nameof(ScanGlobalPermissionsActivity))]
        public async Task<ItemExtensionData> RunAsync([ActivityTrigger]
            Response.Project project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            return new ItemExtensionData
            {
                Item = null,
                ItemId = null,
                Rules = await Task.WhenAll(_rules.Select(async r =>
                {
                    var ruleName = r.GetType().Name;
                    return new EvaluatedRule
                    {
                        Name = ruleName,
                        Description = r.Description,
                        Link = r.Link,
                        Status = await r.EvaluateAsync(project.Id)
                                .ConfigureAwait(false),
                        Reconcile = ReconcileFunction.ReconcileFromRule(
                                _config, project.Id, r as IProjectReconcile)
                    };
                })
                    .ToList())
                    .ConfigureAwait(false)
            };
        }
    }
}