namespace Nikse.SubtitleEdit.Logic.Config.Language;

public class LanguageSpeakerProfiles
{
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public string SummaryXSpeakersYLines { get; set; }
    public string Gender { get; set; }
    public string Confidence { get; set; }
    public string Source { get; set; }
    public string SaveErrorX { get; set; }
    public string LoadWarningX { get; set; }
    public string DetectFromVideo { get; set; }
    public string DetectedXSpeakersYLines { get; set; }
    public string NoOverlappingSpeech { get; set; }
    public string HfTokenSessionOnly { get; set; }
    public string HfTokenHint { get; set; }
    public string PlaySample { get; set; }
    public string SuggestGender { get; set; }
    public string ApplySuggestion { get; set; }
    public string Suggestion { get; set; }
    public string Suggesting { get; set; }
    public string SuggestedCountX { get; set; }
    public string PythonSetupRequired { get; set; }
    public string SuggestHint { get; set; }

    public LanguageSpeakerProfiles()
    {
        Title = "Speaker profiles";
        Subtitle = "Review speaker names and gender metadata used during translation.";
        SummaryXSpeakersYLines = "{0} speakers used in {1} subtitle lines";
        Gender = "Gender";
        Confidence = "Confidence";
        Source = "Source";
        SaveErrorX = "The subtitle was saved, but its speaker profiles could not be saved: {0}";
        LoadWarningX = "Speaker profiles were ignored: {0}";
        DetectFromVideo = "Detect speakers in video...";
        DetectedXSpeakersYLines = "Detected {0} speakers and matched {1} subtitle lines";
        NoOverlappingSpeech = "Speakers were detected, but none of their speech overlaps the open subtitles.";
        HfTokenSessionOnly = "Hugging Face token (this window only)";
        HfTokenHint = "Optional for WhisperX diarization. The token is not saved in Subtitle Edit settings.";
        PlaySample = "Play voice sample";
        SuggestGender = "Suggest from voices";
        ApplySuggestion = "Accept suggestion";
        Suggestion = "Suggestion";
        Suggesting = "Analyzing voices locally...";
        SuggestedCountX = "Suggestions for {0} speakers. Review each one before accepting.";
        PythonSetupRequired = "Install numpy, librosa, onnxruntime, and huggingface_hub in Python 3. Set SUBTITLEEDIT_GENDER_PYTHON to that Python executable.";
        SuggestHint = "Downloads a small public model on first use. Audio stays on this computer; results need your approval.";
    }
}
