using Microsoft.DurableTask;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Functions.Helpers
{
    public static class RetryHelper
    {
        private const int FirstRetryInterval = 1 * 60; // First retry happens after 1 minute
        private const int MaxNumberOfAttempts = 10; // Maximum of 6 attempts
        private const double BackoffCoefficient = 1.5; // Back-off timer is multiplied by this number for each retry
        private const int MaxRetryInterval = 25 * 60; // Maximum time to wait
        private const int RetryTimeout = 5 * 60; // Time to wait before a single retry times out

        public static TaskOptions ActivityRetryOptions()
        {
            return TaskOptions.FromRetryPolicy(
                new RetryPolicy(
                    firstRetryInterval: TimeSpan.FromSeconds(FirstRetryInterval),
                    maxNumberOfAttempts: MaxNumberOfAttempts,
                    backoffCoefficient: BackoffCoefficient,
                    retryTimeout: TimeSpan.FromSeconds(RetryTimeout),
                    maxRetryInterval: TimeSpan.FromSeconds(MaxRetryInterval)
            ));

            //TODO figure out how to add RetryHandler
            //var handlerOptions = TaskOptions.FromRetryHandler(retryContext =>
            //{
            //    return IsRetryableActivity(retryContext.LastFailure);
            //});
        }

        private static bool IsRetryableActivity(TaskFailureDetails failureDetails) => 
                failureDetails.ErrorMessage.Contains("Call failed with status code 429") ||
                failureDetails.ErrorMessage.Contains("A connection attempt failed because " +
                    "the connected party did not properly respond after a period of time") ||
                failureDetails.IsCausedBy<SocketException>() ||
                failureDetails.IsCausedBy<TaskCanceledException>();
    }
}