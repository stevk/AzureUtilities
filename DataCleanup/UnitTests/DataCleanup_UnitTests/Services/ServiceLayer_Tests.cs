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

        #endregion

        #region DeleteQueues

        // Example of testing without using Moq. Does not add additional code coverage.
        [Theory]
        [MemberData(nameof(GetQueueLists), MemberType = typeof(ServiceLayer_Tests))]
        public async Task ServiceLayer_DeleteQueues_ShouldWork(List<string> queueList)
        {
            // Setup
            DataCleanupParameters parameters = TestValues.GetDataCleanupParameters();
            _queueManager.ReturnQueueList = queueList;
            ServiceLayer serviceLayer = new ServiceLayer(
                _eventGridManager,
                _queueManager,
                _tableManager,
                _log);

            // Act
            await serviceLayer.DeleteQueues(parameters);

            // Verify
            _queueManager.DeletedQueues.Count.Should().Be(queueList?.Count ?? 0);
            _queueManager.DeletedQueues.Should().BeEquivalentTo(queueList ?? new List<string>());
            _log.Messages.Count.Should().Be(2);
            _log.Messages[0].Message.Should().Be("Queue deletion starting.");
            _log.Messages[1].Message.Should().Be($"Queue deletion completed! Removed {queueList?.Count ?? 0} queues.");
        }

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
            _log.Messages.Count.Should().Be(2);
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
            _log.Messages.Count.Should().Be(2);
            _log.Messages[0].Message.Should().Be("Queue deletion starting.");
            _log.Messages[1].Message.Should().Be("Exception encountered in DeleteQueues method.");
        }

        #endregion

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

        #region DeleteTables

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
            deletedTableList.Count.Should().Be(tableList?.Count ?? 0);
            deletedTableList.Should().BeEquivalentTo(tableList ?? new List<CloudTable>());
            _log.Messages.Count.Should().Be(2);
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
            _log.Messages.Count.Should().Be(2);
            _log.Messages[0].Message.Should().Be("Table deletion starting.");
            _log.Messages[1].Message.Should().Be("Exception encountered in DeleteTables method.");
        }

        #endregion
    }
}
