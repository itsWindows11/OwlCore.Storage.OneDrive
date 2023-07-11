# OwlCore.Storage.OneDrive [![Version](https://img.shields.io/nuget/v/OwlCore.Storage.OneDrive.svg)](https://www.nuget.org/packages/OwlCore.Storage.OneDrive)

An implementation of OwlCore.Storage that uses MSGraph to access OneDrive.

**NOTE:** This library is read-only for now, and doesn't support modifying folder contents. Check back later, or feel free to submit a PR.

## Install

Published releases are available on [NuGet](https://www.nuget.org/packages/OwlCore.Storage.OneDrive). To install, run the following command in the [Package Manager Console](https://docs.nuget.org/docs/start-here/using-the-package-manager-console).

    PM> Install-Package OwlCore.Storage.OneDrive
    
Or using [dotnet](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet)

    > dotnet add package OwlCore.Storage.OneDrive

## Usage

Before you begin, obtain an instance of a `GraphClient` from either: 
- The official [`Microsoft.Graph`](https://learn.microsoft.com/en-us/graph/sdks/create-client?tabs=CS) libraries. 
- or, the [`CommunityToolkit.Graph`](https://github.com/CommunityToolkit/Graph-Controls) helpers. This is recommended for apps in the Microsoft Store.


```cs
var graphClient = CreateGraphClient(...);

// Then, get the desired drive to work in.
var drive = await graphClient.Me.Drive.GetAsync();

// To get folder from a known folder Id:
var knownFolderId = "someId";
var driveItem = await graphClient.Drives[driveItem.Id].Items[knownFolderId].GetAsync();

// To get user's root folder in OneDrive:
var driveItem = await graphClient.Drives[drive.Id].Root.GetAsync();

// Then pass to a new OneDrive folder
var oneDrive = new OneDriveFolder(graphClient, driveItem);

// Retrieve all files in the folder
await foreach (var file in oneDrive.GetFilesAsync())
{
    // ...
}
```

## Financing

We accept donations [here](https://github.com/sponsors/Arlodotexe) and [here](https://www.patreon.com/arlodotexe), and we do not have any active bug bounties.

## Versioning

Version numbering follows the Semantic versioning approach. However, if the major version is `0`, the code is considered alpha and breaking changes may occur as a minor update.

## License

All OwlCore code is licensed under the MIT License. OwlCore is licensed under the MIT License. See the [LICENSE](./src/LICENSE.txt) file for more details.

