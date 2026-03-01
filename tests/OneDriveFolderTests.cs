using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OwlCore.Storage.CommonTests;

namespace OwlCore.Storage.OneDrive.Tests;

[TestClass]
public class OneDriveFolderTests : CommonIFolderTests
{
    private GraphServiceClient? _graphClient;

    // OneDrive does not reliably populate LastAccessedAt, and does not provide any user-level control over it.
    public override PropertyValueAvailability LastAccessedAtAvailability => PropertyValueAvailability.Maybe;

    // OneDrive updates are eventual, not immediate
    public override PropertyUpdateBehavior LastModifiedAtUpdateBehavior => PropertyUpdateBehavior.Eventual;
    public override PropertyUpdateBehavior LastAccessedAtUpdateBehavior => PropertyUpdateBehavior.Eventual;
    public override PropertyUpdateBehavior CreatedAtUpdateBehavior => PropertyUpdateBehavior.Never;

    public async Task<GraphServiceClient> GetGraphClientAsync()
    {
        if (_graphClient is not null)
            return _graphClient;

        var token = OneDriveTestConfig.GetAccessToken();

        if (string.IsNullOrWhiteSpace(token))
        {
            Assert.Inconclusive("Token not found. Set the 'OC_ONEDRIVE_TOKEN' environment variable or user secret.");
        }

        var accessTokenProvider = new TestAccessTokenProvider(token);
        var authProvider = new BaseBearerTokenAuthenticationProvider(accessTokenProvider);
        _graphClient = new GraphServiceClient(authProvider);

        // Verify connectivity
        try 
        {
             await _graphClient.Me.GetAsync();
        }
        catch (Exception ex)
        {
            Assert.Inconclusive($"Failed to connect to Graph API: {ex.Message}");
        }

        return _graphClient;
    }

    public override async Task<IFolder> CreateFolderAsync()
    {
        var client = await GetGraphClientAsync();
        
        var testRootName = "OwlCore_Storage_Tests";
        
        // Ensure test root exists
        DriveItem rootFolder;
        try
        {
            var drive = await client.Me.Drive.GetAsync();
            var children = await client.Drives[drive.Id].Items["root"].Children.GetAsync();
             
             DriveItem? existing = null;
             if (children?.Value != null)
             {
                 foreach(var item in children.Value)
                 {
                    if (item.Name == testRootName) 
                    {
                        existing = item;
                        break;
                    }
                 }
             }

             if (existing != null)
                 rootFolder = existing;
             else
             {
                 var newRoot = new DriveItem
                 {
                     Name = testRootName,
                     Folder = new Folder(),
                     AdditionalData = new global::System.Collections.Generic.Dictionary<string, object>
                     {
                         { "@microsoft.graph.conflictBehavior", "replace" }
                     }
                 };
                 rootFolder = await client.Drives[drive.Id].Items["root"].Children.PostAsync(newRoot);
             }
        }
        catch (Exception ex)
        {
             throw new InvalidOperationException($"Could not setup test root: {ex.Message}", ex);
        }

        // Create a unique folder for THIS test instance
        var uniqueName = Guid.NewGuid().ToString();
        var driveRef = await client.Me.Drive.GetAsync();
        
        var folderToCreate = new DriveItem
        {
            Name = uniqueName,
            Folder = new Folder(),
             AdditionalData = new global::System.Collections.Generic.Dictionary<string, object>
             {
                 { "@microsoft.graph.conflictBehavior", "fail" }
             }
        };

        var createdItem = await client.Drives[driveRef.Id].Items[rootFolder.Id].Children.PostAsync(folderToCreate);

        return new OneDriveFolder(client, createdItem);
    }

    public override async Task<IFolder> CreateFolderWithItems(int fileCount, int folderCount)
    {
        var folder = (OneDriveFolder)await CreateFolderAsync();
        var client = await GetGraphClientAsync();
        var drive = await client.Me.Drive.GetAsync();

        for (int i = 0; i < fileCount; i++)
        {
             var fileToCreate = new DriveItem
             {
                 Name = $"File_{i}.txt",
                 File = new Microsoft.Graph.Models.FileObject(),
                 AdditionalData = new global::System.Collections.Generic.Dictionary<string, object>
                 {
                     { "@microsoft.graph.conflictBehavior", "fail" }
                 }
             };
             await client.Drives[drive.Id].Items[folder.Id].Children.PostAsync(fileToCreate);
        }

        for (int i = 0; i < folderCount; i++)
        {
             var folderToCreate = new DriveItem
             {
                 Name = $"Folder_{i}",
                 Folder = new Folder(),
                 AdditionalData = new global::System.Collections.Generic.Dictionary<string, object>
                 {
                     { "@microsoft.graph.conflictBehavior", "fail" }
                 }
             };
             await client.Drives[drive.Id].Items[folder.Id].Children.PostAsync(folderToCreate);
        }

        return folder;
    }

    public override Task<IFolder?> CreateFolderWithCreatedAtAsync(DateTime createdAt) => Task.FromResult<IFolder?>(null);
    public override Task<IFolder?> CreateFolderWithLastModifiedAtAsync(DateTime lastModifiedAt) => Task.FromResult<IFolder?>(null);
    public override Task<IFolder?> CreateFolderWithLastAccessedAtAsync(DateTime lastAccessedAt) => Task.FromResult<IFolder?>(null);
}