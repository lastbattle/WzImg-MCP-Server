using WzImgMCP.Tools;

namespace WzImgMCP.Tests;

public class AudioToolsTests : IClassFixture<TestFixture>
{
    private readonly AudioTools _tools;

    public AudioToolsTests(TestFixture fixture)
    {
        fixture.InitializeDataSource();
        _tools = new AudioTools(fixture.Session);
    }

    [Fact]
    public void AudioLookups_HandleMissingAndLists()
    {
        var info = _tools.GetSoundInfo("Sound", "TestSound.img", "soundInfo");
        MarkdownTestHelper.AssertFailure(info);

        var data = _tools.GetSoundData("Sound", "TestSound.img", "soundInfo");
        MarkdownTestHelper.AssertFailure(data);

        var list = _tools.ListSoundsInImage("Sound", "TestSound.img", 10);
        MarkdownTestHelper.AssertSuccess(list);
        Assert.Contains("count", list, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveSoundLink_InvalidType_Fails()
    {
        var resolved = _tools.ResolveSoundLink("Sound", "TestSound.img", "description");
        MarkdownTestHelper.AssertFailure(resolved);
    }
}
