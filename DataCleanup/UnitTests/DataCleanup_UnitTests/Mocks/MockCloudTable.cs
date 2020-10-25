using Microsoft.Azure.Cosmos.Table;
using System;

namespace DataCleanup_UnitTests.Mocks
{
    public class MockCloudTable : CloudTable
    {
        public MockCloudTable(Uri tableAddress) : base(tableAddress)
        { }

        public MockCloudTable(StorageUri tableAddress, StorageCredentials credentials) : base(tableAddress, credentials)
        { }

        public MockCloudTable(Uri tableAbsoluteUri, StorageCredentials credentials) : base(tableAbsoluteUri, credentials)
        { }
    }
}
