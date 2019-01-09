using System;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using Rules.Reports;
using SecurePipelineScan.Rules;
using SecurePipelineScan.VstsService.Response;
using VstsLogAnalytics.Client;
using VstsLogAnalyticsFunction.SecurityScan.Activites;
using Xunit;

namespace VstsLogAnalyticsFunction.Tests.SecurityScan.Activities
{
    public class CreateSecurityReportTest
    {
        [Fact]
        public async Task RunShouldCallSecurityReportScanExecute()
        {
            //Arrange
            var fixture = new Fixture();
            
            var logAnalyticsClient = new Mock<ILogAnalyticsClient>();
            var iLoggerMock = new Mock<ILogger>();
            var scan = new Mock<IProjectScan<SecurityReport>>();
            scan
                .Setup(x => x.Execute(It.IsAny<string>()))
                .Returns(fixture.Create<SecurityReport>());

            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            durableActivityContextBaseMock
                .Setup(x => x.GetInput<Project>())
                .Returns(fixture.Create<Project>());
            
            //Act
            await CreateSecurityReport.Run(
                durableActivityContextBaseMock.Object, 
                logAnalyticsClient.Object, 
                scan.Object,
                iLoggerMock.Object);

            //Assert
            scan
                .Verify(x => x.Execute(It.IsAny<string>()) ,Times.AtLeastOnce());
            logAnalyticsClient
                .Verify(x => x.AddCustomLogJsonAsync("SecurityScanReport", It.IsAny<SecurityReport>(), It.IsAny<string>()));
        }

        [Fact]
        public async Task RunWithNoProjectFoundFromContextShouldThrowException()
        {
            //Arrange
            var scan = new Mock<IProjectScan<SecurityReport>>(MockBehavior.Strict);
            var logAnalyticsClientMock = new Mock<ILogAnalyticsClient>();
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            var iLoggerMock = new Mock<ILogger>();

            //Act
            var ex = await Assert.ThrowsAsync<Exception>(async () => await CreateSecurityReport.Run(
                durableActivityContextBaseMock.Object, 
                logAnalyticsClientMock.Object,
                scan.Object,
                iLoggerMock.Object));
            
            //Assert
            Assert.Equal("No Project found in parameter DurableActivityContextBase", ex.Message);
        }

        [Fact]

        public async Task RunWithNullLogAnalyticsClientShouldThrowException()
        {
            //Arrange
            var scan = new Mock<IProjectScan<SecurityReport>>(MockBehavior.Strict);
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            var iLoggerMock = new Mock<ILogger>();

            //Act
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await CreateSecurityReport.Run(
                    durableActivityContextBaseMock.Object, 
                    null,
                    scan.Object,
                    iLoggerMock.Object));            
            //Assert
            Assert.Equal("Value cannot be null.\nParameter name: logAnalyticsClient", ex.Message);
        }
        
        [Fact]
        public async Task RunWithNullIVstsRestClientShouldThrowException()
        {
            //Arrange
            var logAnalyticsClientMock = new Mock<ILogAnalyticsClient>();
            var durableActivityContextBaseMock = new Mock<DurableActivityContextBase>();
            var iLoggerMock = new Mock<ILogger>();

            //Act
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await CreateSecurityReport.Run(
                    durableActivityContextBaseMock.Object, 
                    logAnalyticsClientMock.Object,
                    null,
                    iLoggerMock.Object));            
            //Assert
            Assert.Equal("Value cannot be null.\nParameter name: scan", ex.Message);
        }
        
        [Fact]
        public async Task RunWithNullDurableActivityContextShouldThrowException()
        {
            //Arrange
            var scan = new Mock<IProjectScan<SecurityReport>>(MockBehavior.Strict);
            var logAnalyticsClientMock = new Mock<ILogAnalyticsClient>();
            var iLoggerMock = new Mock<ILogger>();

            //Act
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await CreateSecurityReport.Run(
                    null, 
                    logAnalyticsClientMock.Object,
                    scan.Object,
                    iLoggerMock.Object));            
            //Assert
            Assert.Equal("Value cannot be null.\nParameter name: context", ex.Message);
        }
    }
}