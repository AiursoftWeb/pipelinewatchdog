using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aiursoft.PipelineWatchDog.Tests;

[TestClass]
public sealed class RuntimeContractTests
{
    private TestEnvironment environment = null!;

    [TestInitialize]
    public void Initialize()
    {
        environment = new TestEnvironment();
        environment.Set("GITLAB_URL", "https://gitlab.example/");
        environment.Set("GITLAB_ACCESS_TOKEN", "initial-test-token");
        environment.Set("ALERTCENTER_URL", "https://alerts.example/");
        environment.Set("ALERTCENTER_API_KEY", "test-key");
        environment.Set("GITLAB_GROUPS", "aiursoft, aiursoft, aiursoft/team");
        environment.Set("TOKEN_DIRECTORY", environment.DirectoryPath);
        File.Delete(Program.HealthFile);
    }

    [TestCleanup]
    public void Cleanup()
    {
        File.Delete(Program.HealthFile);
        environment.Dispose();
    }

    [TestMethod]
    public void SettingsNormalizeEndpointsAndDeduplicateExactGroups()
    {
        var settings = Settings.Load();
        Assert.AreEqual("https://gitlab.example", settings.GitLabUrl);
        Assert.AreEqual("https://alerts.example", settings.AlertCenterUrl);
        CollectionAssert.AreEqual(new[] { "aiursoft", "aiursoft/team" }, settings.Groups);
        Assert.AreEqual(environment.DirectoryPath, settings.TokenDirectory);
    }

    [TestMethod]
    [DataRow("ftp://gitlab.example")]
    [DataRow("https://token@gitlab.example")]
    [DataRow("https://gitlab.example/?token=secret")]
    [DataRow("https://gitlab.example/#fragment")]
    public void RejectCredentialBearingOrInvalidEndpoints(string endpoint)
    {
        environment.Set("GITLAB_URL", endpoint);
        Assert.ThrowsExactly<InvalidOperationException>(() => Settings.Load());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("/aiursoft")]
    [DataRow("aiursoft/")]
    [DataRow("aiursoft//team")]
    public void RejectInvalidGroupPaths(string groups)
    {
        environment.Set("GITLAB_GROUPS", groups);
        Assert.ThrowsExactly<InvalidOperationException>(() => Settings.Load());
    }

    [TestMethod]
    public async Task HealthcheckDoesNotNeedCredentialsAndRejectsMissingEvidence()
    {
        environment.Set("GITLAB_ACCESS_TOKEN", null);
        environment.Set("ALERTCENTER_API_KEY", null);
        Assert.AreEqual(1, await Program.Main(["--healthcheck"]));
    }

    [TestMethod]
    [DataRow(1, 0)]
    [DataRow(16, 1)]
    public async Task HealthcheckKeepsItsExistingFreshnessBoundary(int minutesOld, int expected)
    {
        await File.WriteAllTextAsync(Program.HealthFile, "test");
        File.SetLastWriteTimeUtc(Program.HealthFile, DateTime.UtcNow.AddMinutes(-minutesOld));
        Assert.AreEqual(expected, await Program.Main(["--healthcheck"]));
    }

    [TestMethod]
    public async Task ExistingSavedCredentialWinsWithoutRotation()
    {
        await SaveCredential("https://gitlab.example", false);
        using var token = new GitLabToken(Settings.Load(), NullLogger<GitLabToken>.Instance);
        await token.Refresh(CancellationToken.None);
        Assert.AreEqual("saved-test-token", token.Current);
    }

    [TestMethod]
    public async Task InterruptedRotationRemainsBlocked()
    {
        await SaveCredential("https://gitlab.example", true);
        using var token = new GitLabToken(Settings.Load(), NullLogger<GitLabToken>.Instance);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => token.Refresh(CancellationToken.None));
    }

    [TestMethod]
    public async Task SavedCredentialForAnotherEndpointIsRejected()
    {
        await SaveCredential("https://other.example", false);
        using var token = new GitLabToken(Settings.Load(), NullLogger<GitLabToken>.Instance);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => token.Refresh(CancellationToken.None));
    }

    [TestMethod]
    public async Task SavedTokenDirectoryKeepsOneWriter()
    {
        await SaveCredential("https://gitlab.example", false);
        using var first = new GitLabToken(Settings.Load(), NullLogger<GitLabToken>.Instance);
        using var second = new GitLabToken(Settings.Load(), NullLogger<GitLabToken>.Instance);
        await first.Refresh(CancellationToken.None);
        await Assert.ThrowsExactlyAsync<IOException>(() => second.Refresh(CancellationToken.None));
    }

    private Task SaveCredential(string endpoint, bool pending) => File.WriteAllTextAsync(
        Path.Combine(environment.DirectoryPath, "gitlab-token.json"),
        JsonSerializer.Serialize(new { Endpoint = endpoint, Token = "saved-test-token", RotatedAt = DateTimeOffset.UtcNow, RotationPending = pending }));
}
