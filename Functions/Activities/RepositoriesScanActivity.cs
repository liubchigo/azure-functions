﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Functions.Model;
using Microsoft.Azure.WebJobs;
using SecurePipelineScan.Rules.Security;
using SecurePipelineScan.VstsService;

namespace Functions.Activities
{
    public class RepositoriesScanActivity
    {
        private readonly IRulesProvider _rulesProvider;
        private readonly EnvironmentConfig _config;
        private readonly IVstsRestClient _azuredo;

        public RepositoriesScanActivity(EnvironmentConfig config, IVstsRestClient azuredo, 
            IRulesProvider rulesProvider)
        {
            _config = config;
            _azuredo = azuredo;
            _rulesProvider = rulesProvider;
        }

        [FunctionName(nameof(RepositoriesScanActivity))]
        public async Task<ItemExtensionData> RunAsync(
            [ActivityTrigger] RepositoriesScanActivityRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var rules = _rulesProvider.RepositoryRules(_azuredo).ToList();

            return new ItemExtensionData
            {
                Item = request.Repository.Name,
                ItemId = request.Repository.Id,
                Rules = await Task.WhenAll(rules.Select(async rule => 
                    new EvaluatedRule
                    {
                        Name = rule.GetType().Name,
                        Description = rule.Description,
                        Why = rule.Why,
                        IsSox = rule.IsSox,
                        Status = await rule.EvaluateAsync(request.Project.Id, request.Repository.Id, request.Policies)
                            .ConfigureAwait(false),
                        Reconcile = ReconcileFunction.ReconcileFromRule(rule as IReconcile, _config, 
                            request.Project.Id, RuleScopes.Repositories, request.Repository.Id)
                    })
                    .ToList())
                    .ConfigureAwait(false),
                CiIdentifiers = String.Join(",", request.CiIdentifiers)
            };
        }
    }
}