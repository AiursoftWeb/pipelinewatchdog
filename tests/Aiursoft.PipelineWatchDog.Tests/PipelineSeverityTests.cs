namespace Aiursoft.PipelineWatchDog.Tests;

[TestClass]
public sealed class PipelineSeverityTests
{
    [TestMethod]
    [DataRow(0, "sev4")]
    [DataRow(10799, "sev4")]
    [DataRow(10800, "sev3")]
    [DataRow(28799, "sev3")]
    [DataRow(28800, "sev2")]
    public void FailureAgeDeterminesReportedLevel(int seconds, string expected)
    {
        Assert.AreEqual(expected, Reporter.FailureLevel(TimeSpan.FromSeconds(seconds)));
    }

    [TestMethod]
    [DataRow("sev4", "sev3", true)]
    [DataRow("sev4", "sev2", true)]
    [DataRow("sev3", "sev2", true)]
    [DataRow("sev4", "sev4", false)]
    [DataRow("sev3", "sev3", false)]
    [DataRow("sev2", "sev2", false)]
    [DataRow("sev2", "sev3", false)]
    [DataRow("sev3", "sev4", false)]
    [DataRow("sev1", "sev2", false)]
    public void OnlyRaiseAnExistingIncidentsSeverity(string current, string target, bool expected)
    {
        Assert.AreEqual(expected, Reporter.ShouldEscalate(current, target));
    }
}
