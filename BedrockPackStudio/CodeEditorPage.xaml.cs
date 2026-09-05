using System;
using System.Text.Json;

namespace BedrockPackStudio;

public partial class CodeEditorPage : ContentPage
{
    private string _previousText = "";

    private string _nextText = "";

    private bool _changingText;

    public CodeEditorPage()
    {
        InitializeComponent();

        CodeEditor.Text =
@"{
  ""format_version"": 2,
  ""header"": {
    ""name"": ""Test Pack"",
    ""description"": ""Bedrock Pack Studio"",
    ""uuid"": ""00000000-0000-0000-0000-000000000000"",
    ""version"": [1, 0, 0],
    ""min_engine_version"": [1, 20, 0]
  },
  ""modules"": [
    {
      ""type"": ""resources"",
      ""uuid"": ""00000000-0000-0000-0000-000000000001"",
      ""version"": [1, 0, 0]
    }
  ]
}";

        _previousText =
            CodeEditor.Text;
    }

    private void OnTextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_changingText)
            return;

        if (e.OldTextValue != null)
            _previousText =
                e.OldTextValue;

        UpdateLineNumbers();
    }

    private void UpdateLineNumbers()
    {
        int count =
            Math.Max(
                1,
                (CodeEditor.Text ?? "")
                    .Split('\n')
                    .Length
            );

        var text = "";

        for (int i = 1; i <= count; i++)
        {
            text += i;

            if (i < count)
                text += "\n";
        }

        LineNumbers.Text = text;
    }

    // =====================================================
    // UNDO
    // =====================================================

    private void OnUndoClicked(
        object sender,
        EventArgs e)
    {
        if (string.IsNullOrEmpty(_previousText))
            return;

        _nextText =
            CodeEditor.Text;

        _changingText = true;

        CodeEditor.Text =
            _previousText;

        _changingText = false;

        UpdateLineNumbers();
    }

    private void OnRedoClicked(
        object sender,
        EventArgs e)
    {
        if (string.IsNullOrEmpty(_nextText))
            return;

        _changingText = true;

        CodeEditor.Text =
            _nextText;

        _changingText = false;

        UpdateLineNumbers();
    }

    // =====================================================
    // FORMAT
    // =====================================================

    private void OnFormatClicked(
        object sender,
        EventArgs e)
    {
        try
        {
            using JsonDocument document =
                JsonDocument.Parse(
                    CodeEditor.Text
                );

            string formatted =
                JsonSerializer.Serialize(
                    document.RootElement,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }
                );

            _previousText =
                CodeEditor.Text;

            CodeEditor.Text =
                formatted;
        }
        catch
        {
            DisplayAlert(
                "JSON Hatası",
                "Kod geçerli bir JSON değil.",
                "Tamam"
            );
        }
    }

    // =====================================================
    // VALIDATE
    // =====================================================

    private async void OnValidateClicked(
        object sender,
        EventArgs e)
    {
        try
        {
            JsonDocument.Parse(
                CodeEditor.Text
            );

            await DisplayAlert(
                "✓ Geçerli",
                "JSON dosyası geçerli.",
                "Tamam"
            );
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "✗ Hatalı JSON",
                ex.Message,
                "Tamam"
            );
        }
    }

    // =====================================================
    // SAVE
    // =====================================================

    private async void OnSaveClicked(
        object sender,
        EventArgs e)
    {
        await DisplayAlert(
            "Kaydedildi",
            "Kod editörde kaydedildi.",
            "Tamam"
        );
    }

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
