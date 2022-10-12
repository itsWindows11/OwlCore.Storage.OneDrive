using System.Threading;

namespace OwlCore.Storage.OneDrive.Tests
{
    [TestClass]
    public class OneDriveFolderTests
    {
        [TestMethod]
        public async Task GetRootFolder()
        {
            var env = new TestEnv();
            var client = await env.CreateClientAsync();

            var rootDriveItem = await client.Drive.Root.Request().Expand("children").GetAsync();

            var rootFolder = new OneDriveFolder(client, rootDriveItem);
        }
    }
}
