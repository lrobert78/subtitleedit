using Nikse.SubtitleEdit.Features.Video.SpeechToText;

namespace UITests.Features.Video.SpeechToText;

public class WhisperXDiarizationArgumentsTests
{
    [Theory]
    [InlineData("", "--diarize")]
    [InlineData("--hf_token token", "--hf_token token --diarize")]
    [InlineData("--diarize --hf_token token", "--diarize --hf_token token")]
    public void RequestedDiarizationAddsTheSwitchExactlyOnce(string original, string expected)
    {
        Assert.Equal(expected, SpeechToTextViewModel.EnsureWhisperXDiarizeArgument(original));
    }

    [Theory]
    [InlineData("--hf_token secret --diarize", "--hf_token [redacted] --diarize")]
    [InlineData("--hf_token=\"secret value\" --diarize", "--hf_token [redacted] --diarize")]
    public void HuggingFaceTokenIsHiddenFromToolsLog(string original, string expected)
    {
        Assert.Equal(expected, SpeechToTextViewModel.RedactWhisperXToken(original));
    }

    [Theory]
    [InlineData("--hf_token hf_example --diarize", "--diarize", "hf_example")]
    [InlineData("--diarize --hf_token=hf_example", "--diarize", "hf_example")]
    [InlineData("--hf_token=\"hf example\" --diarize", "--diarize", "hf example")]
    [InlineData("--diarize --hf_token 'hf example'", "--diarize", "hf example")]
    [InlineData("--diarize", "--diarize", null)]
    public void HuggingFaceTokenIsRemovedFromPersistedArguments(string original, string expectedArguments, string? expectedToken)
    {
        var (arguments, token) = SpeechToTextViewModel.ExtractWhisperXTokenArgument(original);

        Assert.Equal(expectedArguments, arguments);
        Assert.Equal(expectedToken, token);
    }
}
