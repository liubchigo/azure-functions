using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using SecurePipelineScan.Rules.Reports;
using SecurePipelineScan.VstsService.Response;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests.RepositoryScan
{
    public class RepositoryScanProjectOrchestrationTests
    {
        [Fact]
        public async System.Threading.Tasks.Task RunWithHasTwoProjectsShouldCallActivityAsyncForEachProject()
        {
            var fixture = new Fixture();
            fixture.Customize(new AutoMoqCustomization());

            //Arrange
            var durableOrchestrationContextMock = new Mock<DurableOrchestrationContextBase>();
            durableOrchestrationContextMock.Setup(context => context.GetInput<Multiple<Project>>()).Returns(fixture.Create<Multiple<Project>>());

            //Act
            RepositoryScanProjectOrchestration orch = new RepositoryScanProjectOrchestration();
            await orch.Run(durableOrchestrationContextMock.Object, new Mock<ILogger>().Object);
            
            //Assert
            durableOrchestrationContextMock.Verify(x => x.CallActivityAsync<IEnumerable<RepositoryReport>>(nameof(RepositoryScanProjectActivity), It.IsAny<Project>()),Times.AtLeastOnce());
        }
        
    }
}