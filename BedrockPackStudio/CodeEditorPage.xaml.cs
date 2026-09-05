using System.Text.Json;

namespace BedrockPackStudio;

public partial class CodeEditorPage : ContentPage
{
    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();

    private bool _internalChange;

    public CodeEditorPage()
    {
        InitializeComponent();

        LoadFile();
    }

    private async void LoadFile()
    {
        string? path =
            ProjectContext.CurrentFilePath;

        if (string.IsNullOrWhiteSpace(path))
        {
            CodeEditor.Text = "{}";
            UpdateLineNumbers();
            return;
        }

        FileNameLabel.Text =
            Path.GetFileName(path);

        try
        {
            if (File.Exists(path))
            {
                CodeEditor.Text =
                    await File.ReadAllTextAsync(path);
            }
            else
            {
                CodeEditor.Text = "{}";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Hata",
                ex.Message,
                "Tamam");
        }

        UpdateLineNumbers();
    }

    private void OnTextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_internalChange)
            return;

        if (e.OldTextValue != null &&
            e.OldTextValue != e.NewTextValue)
        {
            _undo.Push(
                e.OldTextValue);

            _redo.Clear();
        }

        UpdateLineNumbers();
    }

    private void UpdateLineNumbers()
    {
        int count =
            Math.Max(
                1,
                (CodeEditor.Text ?? "")
                    .Split('\n')
                    .Length);

        LineNumbers.Text =
            string.Join(
                "\n",
                Enumerable.Range(
                    1,
                    count));
    }

    private void OnUndoClicked(
        object sender,
        EventArgs e)
    {
        if (_undo.Count == 0)
            return;

        string current =
            CodeEditor.Text ?? "";

        string previous =
            _undo.Pop();

        _redo.Push(current);

        SetTextWithoutHistory(
            previous);
    }

    private void OnRedoClicked(
        object sender,
        EventArgs e)
    {
        if (_redo.Count == 0)
            return;

        string current =
            CodeEditor.Text ?? "";

        string next =
            _redo.Pop();

        _undo.Push(current);

        SetTextWithoutHistory(
            next);
    }

    private void SetTextWithoutHistory(
        string text)
    {
        _internalChange = true;

        CodeEditor.Text = text;

        _internalChange = false;

        UpdateLineNumbers();
    }

    private async void OnFormatClicked(
        object sender,
        EventArgs e)
    {
        try
        {
            using JsonDocument document =
                JsonDocument.Parse(
                    CodeEditor.Text);

            string formatted =
                JsonSerializer.Serialize(
                    document.RootElement,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            _undo.Push(
                CodeEditor.Text);

            _redo.Clear();

            SetTextWithoutHistory(
                formatted);
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "JSON Hatası",
                ex.Message,
                "Tamam");
        }
    }

    private async void OnValidateClicked(
        object sender,
        EventArgs e)
    {
        try
        {
            JsonDocument.Parse(
                CodeEditor.Text);

            await DisplayAlert(
                "✓ Geçerli",
                "JSON dosyası geçerli.",
                "Tamam");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "✗ Geçersiz JSON",
                ex.Message,
                "Tamam");
        }
    }

    private async void OnSaveClicked(
        object sender,
        EventArgs e)
    {
        string? path =
            ProjectContext.CurrentFilePath;

        if (string.IsNullOrWhiteSpace(path))
        {
            await DisplayAlert(
                "Hata",
                "Kaydedilecek dosya bulunamadı.",
                "Tamam");

            return;
        }

        try
        {
            await File.WriteAllTextAsync(
                path,
                CodeEditor.Text ?? "");

            await DisplayAlert(
                "Kaydedildi ✓",
                Path.GetFileName(path),
                "Tamam");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Kaydetme Hatası",
                ex.Message,
                "Tamam");
        }
    }

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
