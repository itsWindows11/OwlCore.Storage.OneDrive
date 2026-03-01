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
public class OneDriveFileTests : CommonIFileTests
{
    private GraphServiceClient? _graphClient;

    public override bool SupportsWriting => false;

    // OneDrive does not reliably populate LastAccessedAt
    public override PropertyValueAvailability LastAccessedAtAvailability => PropertyValueAvailability.Maybe;

    // OneDrive updates are eventual, not immediate
    public override PropertyUpdateBehavior LastModifiedAtUpdateBehavior => PropertyUpdateBehavior.Eventual;
    public override PropertyUpdateBehavior LastAccessedAtUpdateBehavior => PropertyUpdateBehavior.Never;

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

        return _graphClient;
    }

    public override async Task<IFile> CreateFileAsync()
    {
        var client = await GetGraphClientAsync();
        
        // Use a specific test root folder
        var testRootName = "OwlCore_Storage_Tests";
        
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

             // Create a random file
             var uniqueName = Guid.NewGuid().ToString() + ".txt";
             
             var fileToCreate = new DriveItem
             {
                 Name = uniqueName,
                 File = new Microsoft.Graph.Models.FileObject(),
                 AdditionalData = new global::System.Collections.Generic.Dictionary<string, object>
                 {
                     { "@microsoft.graph.conflictBehavior", "fail" }
                 }
             };
             
             var createdItem = await client.Drives[drive.Id].Items[rootFolder.Id].Children.PostAsync(fileToCreate);

             // Add content to the file
             using var contentStream = new MemoryStream(global::System.Text.Encoding.UTF8.GetBytes("Hello World"));
             await client.Drives[drive.Id].Items[createdItem.Id].Content.PutAsync(contentStream);

             // Return the file wrapper
             return new OneDriveFile(client, createdItem);
        }
        catch (Exception ex)
        {
             throw new InvalidOperationException($"Could not setup test file: {ex.Message}", ex);
        }
    }

    // OneDrive doesn't support setting timestamps at creation via the Graph API
    public override Task<IFile?> CreateFileWithCreatedAtAsync(DateTime createdAt) => Task.FromResult<IFile?>(null);
    public override Task<IFile?> CreateFileWithLastModifiedAtAsync(DateTime lastModifiedAt) => Task.FromResult<IFile?>(null);
    public override Task<IFile?> CreateFileWithLastAccessedAtAsync(DateTime lastAccessedAt) => Task.FromResult<IFile?>(null);
}
