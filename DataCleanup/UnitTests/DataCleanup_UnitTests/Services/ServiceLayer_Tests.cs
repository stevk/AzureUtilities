using Azure;
using Azure.Storage.Queues;
using AzureUtilities.DataCleanup.Dto;
using AzureUtilities.DataCleanup.Interfaces;
using AzureUtilities.DataCleanup.Services;
using AzureUtilities.DataCleanup.Shared;
using DataCleanup_UnitTests.Mocks;
using FluentAssertions;
using Microsoft.Azure.Cosmos.Table;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace DataCleanup_UnitTests
{
    public class ServiceLayer_Tests
    {
        private readonly MockEventGridManager _eventGridManager = new MockEventGridManager();
        private readonly MockQueueManager _queueManager = new MockQueueManager();
        private readonly MockTableManager _tableManager = new MockTableManager();
        private readonly MockLogger<ServiceLayer> _log = new MockLogger<ServiceLayer>();

        #region DeleteQueues

        #region DeleteQueues Helpers

        private static List<string> GetBasicQueueList()
        {
            return new List<string>()
            {
                "queue1",
                "queue2",
                "queue3"
            };
        }

        public static IEnumerable<object[]> GetQueueLists()
        {
            yield return new object[] { new List<string>()
            {
                "queue1"
            }};
            yield return new object[] { GetBasicQueueList() };
            yield return new object[] { new List<string>() };
            yield return new object[] { null };
        }

        private static (Mock<IQueueManager> queueManagerMock, List<string> deletedQueueList) GetMockQueueManager(List<string> queueList)
        {
            var queueManagerMock = new Mock<IQueueManager>();
            queueManagerMock
                .Setup(manager => manager.CreateQueueServiceClient(It.IsAny<string>()))
                .Returns((string storageConnectionString) =>
                {
                    return new QueueServiceClient(storageConnectionString);
                })
                .Verifiable();
            queueManagerMock
                .Setup(manager => manager.GetQueues(It.IsAny<QueueServiceClient>()))
                .Returns(Task.FromResult(queueList))
                .Verifiable();
            var deletedQueueList = new List<string>();
            queueManagerMock
                .Setup(manager => manager.DeleteQueue(It.IsAny<QueueServiceClient>(), It.IsAny<string>()))
                .Returns((QueueServiceClient queueServiceClient, string queueName) =>
                {
                    deletedQueueList.Add(queueName);
                    return Task.CompletedTask;
                });

            // Used by PopulateDomainTopicQueue
            queueManagerMock
                .Setup(manager => manager.CreateQueueClient(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string storageConnectionString, string queueName) =>
                {
                    return new QueueClient(storageConnectionString, queueName);
                });
            queueManagerMock
                .Setup(manager => manager.CreateIfNotExistsAsync(It.IsAny<QueueClient>()))
                .Returns(Task.CompletedTask);
            queueManagerMock
                .Setup(manager => manager.SendMessageAsync(It.IsAny<QueueClient>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            return (queueManagerMock, deletedQueueList);
        }

        public static IEnumerable<object[]> GetMockQueueManagerWithException()
        {
            var exceptionMessage = "Expected exception";

            Mock<IQueueManager> queueManagerMock = GetMockQueueManager(GetBasicQueueList()).queueManagerMock;
            queueManagerMock
                .Setup(manager => manager.CreateQueueServiceClient(It.IsAny<string>()))
                .Throws(new RequestFailedException(exceptionMessage));
            yield return new object[]
            {
                queueManagerMock,
                exceptionMessage
            };

            queueManagerMock = GetMockQueueManager(GetBasicQueueList()).queueManagerMock;
            queueManagerMock
                .Setup(manager => manager.GetQueues(It.IsAny<QueueServiceClient>()))
                .Throws(new RequestFailedException(exceptionMessage));
            yield return new object[]
            {
                queueManagerMock,
                exceptionMessage
            };

            queueManagerMock = GetMockQueueManager(GetBasicQueueList()).queueManagerMock;
            queueManagerMock
                .Setup(manager => manager.DeleteQueue(It.IsAny<QueueServiceClient>(), It.IsAny<string>()))
                .Throws(new RequestFailedException(exceptionMessage));
            yield return new object[]
            {
                queueManagerMock,
                exceptionMessage
            };
        }

        public static IEnumerable<object[]> GetMockQueueManagerWithException2()
        {
            var exceptionMessage = "Expected exception";

            Mock<IQueueManager> queueManagerMock = GetMockQueueManager(GetBasicQueueList()).queueManagerMock;
            queueManagerMock
                .Setup(manager => manager.CreateQueueClient(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new RequestFailedException(exceptionMessage));
            yield return new object[]
            {
                queueManagerMock,
                exceptionMessage
            };

            queueManagerMock = GetMockQueueManager(GetBasicQueueList()).queueManagerMock;
            queueManagerMock
                .Setup(manager => manager.CreateIfNotExistsAsync(It.IsAny<QueueClient>()))
                .Throws(new RequestFailedException(exceptionMessage));
            yield return new object[]
            {
                queueManagerMock,
                exceptionMessage
            };

            queueManagerMock = GetMockQueueManager(GetBasicQueueList()).queueManagerMock;
            queueManagerMock
                .Setup(manager => manager.SendMessageAsync(It.IsAny<QueueClient>(), It.IsAny<string>()))
                .Throws(new RequestFailedException(exceptionMessage));
            yield return new object[]
            {
                queueManagerMock,
                exceptionMessage
            };
        }

        #endregion

        [Theory]
        [MemberData(nameof(GetQueueLists), MemberType = typeof(ServiceLayer_Tests))]
        public async Task ServiceLayer_DeleteQueues_UsingMoq_ShouldWork(List<string> queueList)
        {
            // Setup
            (Mock<IQueueManager> queueManagerMock, List<string> deletedQueueList) = GetMockQueueManager(queueList);

            ServiceLayer serviceLayer = new ServiceLayer(
                _eventGridManager,
                queueManagerMock.Object,
                _tableManager,
                _log);

            DataCleanupParameters parameters = TestValues.GetDataCleanupParameters();

            // Act
            await serviceLayer.DeleteQueues(parameters);

            // Verify
            queueManagerMock.Verify();
            deletedQueueList.Count.Should().Be(queueList?.Count ?? 0);
            deletedQueueList.Should().BeEquivalentTo(queueList ?? new List<string>());
            _log.Messages.Should().HaveCount(2);
            _log.Messages[0].Message.Should().Be("Queue deletion starting.");
            _log.Messages[1].Message.Should().Be($"Queue deletion completed! Removed {queueList?.Count ?? 0} queues.");
        }

        [Theory]
        [MemberData(nameof(GetMockQueueManagerWithException), MemberType = typeof(ServiceLayer_Tests))]
        public void ServiceLayer_DeleteQueues_DataLayerException_ShouldRethrow(Mock<IQueueManager> queueManagerMock, string exceptionMessage)
        {
            // Setup
            ServiceLayer serviceLayer = new ServiceLayer(
                _eventGridManager,
                queueManagerMock.Object,
                _tableManager,
                _log);

            DataCleanupParameters parameters = TestValues.GetDataCleanupParameters();
            Func<Task> act = async () => await serviceLayer.DeleteQueues(parameters);

            // Act
            act.Should().Throw<RequestFailedException>().WithMessage(exceptionMessage);

            // Verify
            _log.Messages.Should().HaveCount(2);
            _log.Messages[0].Message.Should().Be("Queue deletion starting.");
            _log.Messages[1].Message.Should().Be("Exception encountered in DeleteQueues method.");
        }

        #endregion

        #region DeleteTables

        #region DeleteTables Helpers

        private const string _baseUri = "http://unittests.localhost.com/";

        private static List<CloudTable> GetBasicTableList()
        {
            return new List<CloudTable>()
            {
                new MockCloudTable(new Uri($"{_baseUri}table1")),
                new MockCloudTable(new Uri($"{_baseUri}table2")),
                new MockCloudTable(new Uri($"{_baseUri}table3")),
            };
        }

        public static IEnumerable<object[]> GetTableLists()
        {
            yield return new object[] { new List<CloudTable>()
            {
                new MockCloudTable(new Uri($"{_baseUri}table1")),
            }};
            yield return new object[] { GetBasicTableList() };
            yield return new object[] { new List<CloudTable>() };
            yield return new object[] { null };
        }

        private static (Mock<ITableManager> tableManagerMock, List<CloudTable> deletedTableList) GetMockTableManager(List<CloudTable> tableList)
        {
            var tableManagerMock = new Mock<ITableManager>();
            tableManagerMock
                .Setup(manager => manager.CreateCloudTableClient(It.IsAny<string>()))
                .Returns((string storageConnectionString) =>
                {
                    var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
                    return storageAccount.CreateCloudTableClient();
                })
                .Verifiable();
            tableManagerMock
                .Setup(manager => manager.GetTableListAsync(It.IsAny<CloudTableClient>()))
                .Returns(Task.FromResult(tableList))
                .Verifiable();
            var deletedTableList = new List<CloudTable>();
            tableManagerMock
                .Setup(manager => manager.DeleteIfExists(It.IsAny<CloudTable>()))
                .Returns((CloudTable table) =>
                {
                    deletedTableList.Add(table);
                    return Task.CompletedTask;
                });

            return (tableManagerMock, deletedTableList);
        }

        public static IEnumerable<object[]> GetMockTableManagerWithException()
        {
            var exceptionMessage = "Expected exception";

            Mock<ITableManager> tableManagerMock = GetMockTableManager(GetBasicTableList()).tableManagerMock;
            tableManagerMock
                .Setup(manager => manager.CreateCloudTableClient(It.IsAny<string>()))
                .Throws(new RequestFailedException(exceptionMessage));
            yield return new object[]
            {
                tableManagerMock,
                exceptionMessage
            };

            tableManagerMock = GetMockTableManager(GetBasicTableList()).tableManagerMock;
            tableManagerMock
                .Setup(manager => manager.GetTableListAsync(It.IsAny<CloudTableClient>()))
                .Throws(new RequestFailedException(exceptionMessage));
            yield return new object[]
            {
                tableManagerMock,
                exceptionMessage
            };

            tableManagerMock = GetMockTableManager(GetBasicTableList()).tableManagerMock;
            tableManagerMock
                .Setup(manager => manager.DeleteIfExists(It.IsAny<CloudTable>()))
                .Throws(new RequestFailedException(exceptionMessage));
            yield return new object[]
            {
                tableManagerMock,
                exceptionMessage
            };
        }

        #endregion

        [Theory]
        [MemberData(nameof(GetTableLists), MemberType = typeof(ServiceLayer_Tests))]
        public async Task ServiceLayer_DeleteTables_ShouldWork(List<CloudTable> tableList)
        {
            // Setup
            (Mock<ITableManager> tableManagerMock, List<CloudTable> deletedTableList) = GetMockTableManager(tableList);

            ServiceLayer serviceLayer = new ServiceLayer(
                _eventGridManager,
                _queueManager,
                tableManagerMock.Object,
                _log);

            DataCleanupParameters parameters = TestValues.GetDataCleanupParameters();

            // Act
            await serviceLayer.DeleteTables(parameters);

            // Verify
            tableManagerMock.Verify();
            deletedTableList.Should().HaveCount(tableList?.Count ?? 0);
            deletedTableList.Should().BeEquivalentTo(tableList ?? new List<CloudTable>());
            _log.Messages.Should().HaveCount(2);
            _log.Messages[0].Message.Should().Be("Table deletion starting.");
            _log.Messages[1].Message.Should().Be($"Table deletion completed! Removed {tableList?.Count ?? 0} tables.");
        }

        [Theory]
        [MemberData(nameof(GetMockTableManagerWithException), MemberType = typeof(ServiceLayer_Tests))]
        public void ServiceLayer_DeleteTables_DataLayerException_ShouldRethrow(Mock<ITableManager> tableManagerMock, string exceptionMessage)
        {
            // Setup
            ServiceLayer serviceLayer = new ServiceLayer(
                _eventGridManager,
                _queueManager,
                tableManagerMock.Object,
                _log);

            DataCleanupParameters parameters = TestValues.GetDataCleanupParameters();
            Func<Task> act = async () => await serviceLayer.DeleteTables(parameters);

            // Act
            act.Should().Throw<RequestFailedException>().WithMessage(exceptionMessage);

            // Verify
            _log.Messages.Should().HaveCount(2);
            _log.Messages[0].Message.Should().Be("Table deletion starting.");
            _log.Messages[1].Message.Should().Be("Exception encountered in DeleteTables method.");
        }

        #endregion

        #region DeleteDomainTopic

        [Fact]
        public async Task ServiceLayer_DeleteDomainTopic_ShouldWork()
        {
            // Setup
            DataCleanupParameters parameters = TestValues.GetDataCleanupParameters();
            parameters.DomainTopicName = "domaintopicname";

            ServiceLayer serviceLayer = new ServiceLayer(
                _eventGridManager,
                _queueManager,
                _tableManager,
                _log);

            // Act
            await serviceLayer.DeleteDomainTopic(parameters);

            // Verify
            _log.Messages.Should().HaveCount(2);
            _log.Messages[0].Message.Should().Be($"Deleting domain topic {parameters.DomainTopicName}");
            _log.Messages[1].Message.Should().Be($"Domain topic deletion completed! {parameters.DomainTopicName}");
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void ServiceLayer_DeleteDomainTopic_DataLayerException_ShouldRethrow(bool getClientThrowsException, bool deleteThrowsException)
        {
            // Setup
            _eventGridManager.ThrowExceptionOnGetClient = getClientThrowsException;
            _eventGridManager.ThrowExceptionOnDelete = deleteThrowsException;

            ServiceLayer serviceLayer = new ServiceLayer(
                _eventGridManager,
                _queueManager,
                _tableManager,
                _log);

            DataCleanupParameters parameters = TestValues.GetDataCleanupParameters();
            parameters.DomainTopicName = "domaintopicname";
            Func<Task> act = async () => await serviceLayer.DeleteDomainTopic(parameters);

            // Act
            act.Should().Throw<RequestFailedException>().WithMessage(_eventGridManager.ExceptionMessage);

            // Verify
            _log.Messages.Should().HaveCount(2);
            _log.Messages[0].Message.Should().Be($"Deleting domain topic {parameters.DomainTopicName}");
            _log.Messages[1].Message.Should().Be("Exception encountered in DeleteDomainTopic method.");
        }

        #endregion

        #region PopulateDomainTopicQueue

        [Fact]
        public async Task ServiceLayer_PopulateDomainTopicQueue_ShouldWork()
        {
            // Setup
            DataCleanupParameters parameters = TestValues.GetDataCleanupParameters();

            ServiceLayer serviceLayer = new ServiceLayer(
                _eventGridManager,
                _queueManager,
                _tableManager,
                _log);

            // Act
            await serviceLayer.PopulateDomainTopicQueue(parameters);

            // Verify
            _log.Messages.Should().HaveCount(6);
            _log.Messages[0].Message.Should().Be("Queuing domain topics for deletion.");
            _log.Messages[1].Message.Should().Be($"Found {_eventGridManager.ReturnDomainTopicNames[0][0]}");
            _log.Messages[2].Message.Should().Be($"Found {_eventGridManager.ReturnDomainTopicNames[0][1]}");
            _log.Messages[3].Message.Should().Be($"Found {_eventGridManager.ReturnDomainTopicNames[0][2]}");
            _log.Messages[4].Message.Should().Be($"{_eventGridManager.ReturnDomainTopicNames[0].Count} domain topics being added to queue for deletion.");
            _log.Messages[5].Message.Should().Be("Finished processing page of domain topics.");
        }

        [Fact]
        public async Task ServiceLayer_PopulateDomainTopicQueue_MultiplePages_ShouldWork()
        {
            // Setup
            DataCleanupParameters parameters = TestValues.GetDataCleanupParameters();

            _eventGridManager.ReturnDomainTopicNames = new List<List<string>>()
            {
                new List<string>()
                {
                    "domaintopic1"
                },
                new List<string>()
                {
                    "domaintopic2"
                },
            };

            ServiceLayer serviceLayer = new ServiceLayer(
                _eventGridManager,
                _queueManager,
                _tableManager,
                _log);

            // Act
            await serviceLayer.PopulateDomainTopicQueue(parameters);

            // Verify
            _log.Messages.Should().HaveCount(5);
            _log.Messages[0].Message.Should().Be("Queuing domain topics for deletion.");
            _log.Messages[1].Message.Should().Be($"Found {_eventGridManager.ReturnDomainTopicNames[0][0]}");
            _log.Messages[2].Message.Should().Be($"{_eventGridManager.ReturnDomainTopicNames[0].Count} domain topics being added to queue for deletion.");
            _log.Messages[3].Message.Should().Be("Added next domain topic page to queue.");
            _log.Messages[4].Message.Should().Be("Finished processing page of domain topics.");

            _queueManager.SentMessages.Should().HaveCount(2);
            _queueManager.SentMessages[1].Should().Contain("\"domainTopicNextPage\":\"Nextpage\"");
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void ServiceLayer_PopulateDomainTopicQueue_EventGridException_ShouldRethrow(bool getClientThrowsException, bool getTopicsThrowsException)
        {
            // Setup
            _eventGridManager.ThrowExceptionOnGetClient = getClientThrowsException;
            _eventGridManager.ThrowExceptionOnGetTopics = getTopicsThrowsException;

            ServiceLayer serviceLayer = new ServiceLayer(
                _eventGridManager,
                _queueManager,
                _tableManager,
                _log);

            DataCleanupParameters parameters = TestValues.GetDataCleanupParameters();
            Func<Task> act = async () => await serviceLayer.PopulateDomainTopicQueue(parameters);

            // Act
            act.Should().Throw<RequestFailedException>().WithMessage(_eventGridManager.ExceptionMessage);

            // Verify
            _log.Messages.Should().HaveCount(2);
            _log.Messages[0].Message.Should().Be("Queuing domain topics for deletion.");
            _log.Messages[1].Message.Should().Be("Exception encountered in PopulateDomainTopicQueue method.");
        }

        [Theory]
        [MemberData(nameof(GetMockQueueManagerWithException2), MemberType = typeof(ServiceLayer_Tests))]
        public void ServiceLayer_PopulateDomainTopicQueue_QueueException_ShouldRethrow(Mock<IQueueManager> queueManagerMock, string exceptionMessage)
        {
            // Setup
            ServiceLayer serviceLayer = new ServiceLayer(
                _eventGridManager,
                queueManagerMock.Object,
                _tableManager,
                _log);

            DataCleanupParameters parameters = TestValues.GetDataCleanupParameters();
            Func<Task> act = async () => await serviceLayer.PopulateDomainTopicQueue(parameters);

            // Act
            act.Should().Throw<RequestFailedException>().WithMessage(exceptionMessage);

            // Verify
            _log.Messages.Should().HaveCountGreaterOrEqualTo(2);
            _log.Messages[0].Message.Should().Be("Queuing domain topics for deletion.");
            _log.Messages[_log.Messages.Count - 1].Message.Should().Be("Exception encountered in PopulateDomainTopicQueue method.");
        }

        #endregion
    }
}
