using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rules.Reports;
using SecurePipelineScan.Rules;
using SecurePipelineScan.VstsService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VstsLogAnalytics.Client;
using VstsLogAnalytics.Common;

namespace VstsLogAnalyticsFunction
{
    public static class RepositoryScanFunction
    {
        [FunctionName("RepositoryScanFunction")]
        public static async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo timerInfo,
            [Inject]ILogAnalyticsClient logAnalyticsClient,
            [Inject]IVstsRestClient client,
            ILogger log)
        {
            try
            {
                log.LogInformation($"Repository scan timed check start: {DateTime.Now}");

                var scan = new RepositoryScan(client, _ =>
                {
                    var reports = _ as IEnumerable<RepositoryReport>;
                    foreach (var r in reports)
                    {
                        logAnalyticsClient.AddCustomLogJsonAsync("GitRepository",
                            JsonConvert.SerializeObject(new
                            {
                                r.Project,
                                r.Repository,
                                r.HasRequiredReviewerPolicy,
                                Date = DateTime.UtcNow,

                            }), "Date");
                    }
                });
                scan.Execute("TAS");
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Failed to write repository scan to log analytics: {ex}");
                throw;
            }
        }
    }
}