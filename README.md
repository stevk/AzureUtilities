# About the Project

This project is an Azure Function Microservice intended for cleaning up persistent data leftover by integration tests run on a Microservice in a development environment.

## Assumptions

- This project is intended as an internal test tool. Additional security considerations are necessary before using this project in a production environment.
- The function is designed to be triggered by an Azure pipeline, either as part of an release or on a schedule.
- Assets in the Storage Account and Event Grid are deleted, not just cleared, with the assumption that they are either created on demand or on starting the Microservice. In the latter case, it is up to the caller to restart the Microservice.

## Why was it created

Running integration tests can result in persistent test data that is no longer useful. This junk data can make it more difficult to debug and has a maintenance cost.
This tool provides an automated means to restore a Microservice's data to a clean state.

## What does it currently cover

- All queues are deleted.
- All tables are deleted.
- All domains/subscriptions are deleted.

## Usage

Send a POST request to the Function with a body that contains the artifacts that are intended to be cleaned up and the credentials needed to act on those artifacts.

Example Request:

`POST http://localhost:7071/api/InitializeCleanup`

``` json
{
    "StorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=testaccount;AccountKey=abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789012345678901234567890123==;EndpointSuffix=core.windows.net",
    "SubscriptionId": "00000000-0000-0000-0000-000000000000",
    "ResourceGroupName": "rsg-tobecleaned",
    "EventGridName": "egd-tobecleaned",
    "ServicePrincipalClientId": "00000000-0000-0000-0000-000000000000",
    "ServicePrincipalClientKey": "abcdefghijklmnopqrstuvwxyz123456",
    "ServicePrincipalTenantId": "00000000-0000-0000-0000-000000000000"
}
```

The Function will return an OK result after all of the following:

- The queues have been deleted.
- The tables have been deleted.
- The first page of domains have been queued for deletion.

Tables and queues can be deleted quickly, but domains take a long time to delete, so that process is finished asynchronously by the Function.

## Roadmap

- Unit tests for methods involving event grid domains.
- Unit tests for controllers.
- Add blob support.
- Integration tests.
- Add callback and status support. (Azure pipeline only allows for short durations on requests before reporting a failure)
- Improvements to initialize performance involving requests that involve a large number of queues and/or tables.
- Add functions allowing more granular actions, like clearing only one type of data.
- Allow blacklisting, E.g. specify a table that is not to be deleted.
- Add option to clear table and queue contents without deleting the entire structure.
