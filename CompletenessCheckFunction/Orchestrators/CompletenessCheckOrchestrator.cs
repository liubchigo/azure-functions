﻿using System.Collections.Generic;
using System.Linq;
using CompletenessCheckFunction.Activities;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using CompletenessCheckFunction.Requests;
using DurableFunctionsAdministration.Client.Response;

namespace CompletenessCheckFunction.Orchestrators
{
    public class CompletenessCheckOrchestrator
    {
        [FunctionName(nameof(CompletenessCheckOrchestrator))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var scansToVerify = await context.CallActivityAsync<List<OrchestrationInstance>>(
                nameof(GetOrchestratorsToVerifyActivity), null);
            var alreadyVerifiedScans = await context.CallActivityAsync<List<string>>(
                nameof(GetCompletedScansFromLogAnalyticsActivity),
                null);

            var filteredScansToVerify = await context.CallActivityAsync<List<OrchestrationInstance>>(
                nameof(FilterAlreadyAnalyzedOrchestratorsActivity),
                new FilterAlreadyAnalyzedOrchestratorsActivityRequest
                    {InstancesToAnalyze = scansToVerify, InstanceIdsAlreadyAnalyzed = alreadyVerifiedScans});

            await Task.WhenAll(filteredScansToVerify.Select(f =>
                context.CallSubOrchestratorAsync(nameof(SingleAnalysisOrchestrator),
                    new SingleAnalysisOrchestratorRequest { InstanceToAnalyze = f })));
        }
    }
}


