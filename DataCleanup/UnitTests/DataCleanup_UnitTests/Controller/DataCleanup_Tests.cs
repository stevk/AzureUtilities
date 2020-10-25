using AzureUtilities.DataCleanup;
using AzureUtilities.DataCleanup.Shared;
using DataCleanup_UnitTests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DataCleanup_UnitTests
{
    public class DataCleanup_Tests
    {
        private readonly MockServiceLayer _ServiceLayer = new MockServiceLayer();

        [Fact]
        public async Task DataCleanup_InitializeCleanup_ShouldWork()
        {
            // Setup
            var dataCleanup = new DataCleanup(_ServiceLayer);

            // Act
            var result = await dataCleanup.InitializeCleanup(TestValues.GetDataCleanupParameters());

            // Verify
            result.Should().BeOfType<OkResult>();
            _ServiceLayer.DeleteQueuesCalled.Should().BeTrue();
            _ServiceLayer.DeleteTablesCalled.Should().BeTrue();
            _ServiceLayer.PopulateDomainTopicQueueCalled.Should().BeTrue();
        }

        [Fact]
        public async Task DataCleanup_DomainTopicList_ShouldWork()
        {
            // Setup
            var dataCleanup = new DataCleanup(_ServiceLayer);

            // Act
            await dataCleanup.DomainTopicList(TestValues.GetQueueMessageData());

            // Verify
            _ServiceLayer.PopulateDomainTopicQueueCalled.Should().BeTrue();
            _ServiceLayer.LastParametersPassed.Should().BeEquivalentTo(TestValues.GetDataCleanupParameters());
        }

        [Fact]
        public async Task DataCleanup_DomainTopicCleanup_ShouldWork()
        {
            // Setup
            var dataCleanup = new DataCleanup(_ServiceLayer);
            string topicName = "domaintopicname";
            string parameters = TestValues.GetQueueMessageData(topicName);

            // Act
            await dataCleanup.DomainTopicCleanup(parameters);

            // Verify
            _ServiceLayer.DeleteDomainTopicCalled.Should().BeTrue();
            var expectedParameters = TestValues.GetDataCleanupParameters();
            expectedParameters.DomainTopicName = topicName;
            _ServiceLayer.LastParametersPassed.Should().BeEquivalentTo(expectedParameters);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void DataCleanup_DomainTopicCleanup_NullOrWhitespaceDomainTopicName_ThrowsException(string topicName)
        {
            // Setup
            var dataCleanup = new DataCleanup(_ServiceLayer);
            string parameters = TestValues.GetQueueMessageData(topicName);

            Func<Task> act = async () => await dataCleanup.DomainTopicCleanup(parameters);

            // Act + Verify
            act.Should().Throw<ArgumentException>().WithMessage("DomainTopicName value is invalid.");
        }
    }
}
