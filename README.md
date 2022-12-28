# OwlCore.Storage.OneDrive [![Version](https://img.shields.io/nuget/v/OwlCore.Storage.OneDrive.svg)](https://www.nuget.org/packages/OwlCore.Storage.OneDrive)

An implementation of OwlCore.Storage that uses MSGraph to access OneDrive.

## Install

Published releases are available on [NuGet](https://www.nuget.org/packages/OwlCore.Storage.OneDrive). To install, run the following command in the [Package Manager Console](https://docs.nuget.org/docs/start-here/using-the-package-manager-console).

    PM> Install-Package OwlCore.Storage.OneDrive
    
Or using [dotnet](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet)

    > dotnet add package OwlCore.Storage.OneDrive

## Usage

```cs
// First, obtain an instance of GraphServiceClient using the Microsoft.Graph. 
// See https://learn.microsoft.com/en-us/graph/sdks/create-client?tabs=CS for instructions.
var graphClient = CreateGraphClient(...);

// Then, obtain an instance of DriveItem.

// To get folder from a known folder ID:
var knownFolderId = ...;
var driveItem = await graphClient.Drive.Items[knownFolderId].Request().GetAsync(cancellationToken);

// To get user's root folder in OneDrive:
var driveItem = await graphClient.Drive.Root.Request().Expand("children").GetAsync(cancellationToken);

// Then pass to a new OneDrive folder
IFolder oneDrive = new OneDriveFolder(graphClient, driveItem);

// Retrieve all files in the folder
await foreach(var file in oneDrive.GetFilesAsync())
{
    ...
}
```

## Financing

We accept donations, and we do not have any active bug bounties.

If you’re looking to contract a new project, new features, improvements or bug fixes, please contact me. 

## Versioning

Version numbering follows the Semantic versioning approach. However, if the major version is `0`, the code is considered alpha and breaking changes may occur as a minor update.

## License

We’re using the MIT license for 3 reasons:
1. We're here to share useful code. You may use any part of it freely, as the MIT license allows. 
2. A library is no place for viral licensing.
3. Easy code transition to larger community-based projects, such as the .NET Community Toolkit.

