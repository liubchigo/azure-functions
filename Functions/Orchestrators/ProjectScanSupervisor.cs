using SecurePipelineScan.VstsService.Response;
using System.Collections.Generic;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using System.Threading;
using System;
using Microsoft.DurableTask;
using Microsoft.Azure.Functions.Worker;

namespace Functions.Orchestrators
{
    public class ProjectScanSupervisor
    {
        private const int TimerInterval = 25; 

        [Function(nameof(ProjectScanSupervisor))]
        public async Task RunAsync([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var scanDate = context.CurrentUtcDateTime;
            var projects = context.GetInput<List<Project>>();

            await Task.WhenAll(projects.Select(async (p, i) => 
                await StartProjectScanOrchestratorWithTimerAsync(context, p, i, scanDate)));
        }

        private static async Task StartProjectScanOrchestratorWithTimerAsync(
            TaskOrchestrationContext context, Project project, int index, DateTime scanDate)
        {
            await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(index * TimerInterval), CancellationToken.None);
            await context.CallSubOrchestratorAsync(nameof(ProjectScanOrchestrator),
                (project, (string)null, scanDate));
        }
    }
}