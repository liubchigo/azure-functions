using Functions.Model;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;

namespace Functions.Activities
{
    public class UploadExtensionDataActivity
    {
        private readonly IVstsRestClient _azuredo;
        private readonly EnvironmentConfig _config;

        public UploadExtensionDataActivity(IVstsRestClient azuredo,
            EnvironmentConfig config)
        {
            _azuredo = azuredo;
            _config = config;
        }

        [Function(nameof(UploadExtensionDataActivity))]
        public async Task RunAsync([ActivityTrigger] ItemsExtensionData data, string scope)
        {
            await _azuredo.PutAsync(ExtensionManagement.ExtensionData<ExtensionDataReports>(
                _config.ExtensionPublisher, _config.ExtensionName, scope), data)
                .ConfigureAwait(false);
        }

        //TODO: figure out where scope and data comes from
    }
}