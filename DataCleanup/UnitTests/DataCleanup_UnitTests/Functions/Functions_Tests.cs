using AzureUtilities.DataCleanup;
using AzureUtilities.DataCleanup.Dto;
using AzureUtilities.DataCleanup.Shared;
using DataCleanup_UnitTests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DataCleanup_UnitTests
{
    public class Functions_Tests
    {
        private readonly MockServiceLayer _ServiceLayer = new MockServiceLayer();

        #region DeleteQueues

        [Fact]
        public async Task DataCleanup_DeleteQueues_ShouldWork()
        {
            // Setup
            var dataCleanup = new Functions(_ServiceLayer);

            // Act
            var result = await dataCleanup.DeleteQueues(TestValues.GetDataCleanupParameters());

            // Verify
            result.Should().BeOfType<OkResult>();
            _ServiceLayer.DeleteQueuesCalled.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task DataCleanup_DeleteQueues_NullOrWhitespaceStorageConnectionString_ThrowsException(string storageConnectionString)
        {
            // Setup
            var dataCleanup = new Functions(_ServiceLayer);
            DataCleanupParameters parameters = new DataCleanupParameters()
            {
                StorageConnectionString = storageConnectionString
            };

            // Act
            var result = await dataCleanup.DeleteQueues(parameters);

            // Verify
            result.Should().BeOfType<BadRequestResult>();
            _ServiceLayer.DeleteQueuesCalled.Should().BeFalse();
        }

        #endregion

        #region DeleteTables

        [Fact]
        public async Task DataCleanup_DeleteTables_ShouldWork()
        {
            // Setup
            var dataCleanup = new Functions(_ServiceLayer);

            // Act
            var result = await dataCleanup.DeleteTables(TestValues.GetDataCleanupParameters());

            // Verify
            result.Should().BeOfType<OkResult>();
            _ServiceLayer.DeleteTablesCalled.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task DataCleanup_DeleteTables_NullOrWhitespaceStorageConnectionString_ThrowsException(string storageConnectionString)
        {
            // Setup
            var dataCleanup = new Functions(_ServiceLayer);
            DataCleanupParameters parameters = new DataCleanupParameters()
            {
                StorageConnectionString = storageConnectionString
            };

            // Act
            var result = await dataCleanup.DeleteTables(parameters);

            // Verify
            result.Should().BeOfType<BadRequestResult>();
            _ServiceLayer.DeleteTablesCalled.Should().BeFalse();
        }

        #endregion

        #region InitializeCleanup

        [Fact]
        public async Task DataCleanup_InitializeCleanup_ShouldWork()
        {
            // Setup
            var dataCleanup = new Functions(_ServiceLayer);

            // Act
            var result = await dataCleanup.InitializeCleanup(TestValues.GetDataCleanupParameters());

            // Verify
            result.Should().BeOfType<OkResult>();
            _ServiceLayer.DeleteQueuesCalled.Should().BeTrue();
            _ServiceLayer.DeleteTablesCalled.Should().BeTrue();
            _ServiceLayer.PopulateDomainTopicQueueCalled.Should().BeTrue();
        }

        #endregion

        #region DomainTopicList

        [Fact]
        public async Task DataCleanup_DomainTopicList_ShouldWork()
        {
            // Setup
            var dataCleanup = new Functions(_ServiceLayer);

            // Act
            await dataCleanup.DomainTopicList(TestValues.GetQueueMessageData());

            // Verify
            _ServiceLayer.PopulateDomainTopicQueueCalled.Should().BeTrue();
            _ServiceLayer.LastParametersPassed.Should().BeEquivalentTo(TestValues.GetDataCleanupParameters());
        }

        #endregion

        #region DomainTopicCleanup

        [Fact]
        public async Task DataCleanup_DomainTopicCleanup_ShouldWork()
        {
            // Setup
            var dataCleanup = new Functions(_ServiceLayer);
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
            var dataCleanup = new Functions(_ServiceLayer);
            string parameters = TestValues.GetQueueMessageData(topicName);

            Func<Task> act = async () => await dataCleanup.DomainTopicCleanup(parameters);

            // Act + Verify
            act.Should().Throw<ArgumentException>().WithMessage("DomainTopicName value is invalid.");
        }

        #endregion
    }
}
