using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace BedrockPackStudio;

public partial class MainPage : ContentPage
{
    private string _lastGeneratedPackPath = string.Empty;

    public MainPage()
    {
        InitializeComponent();

        BackgroundColor = Color.FromArgb("#101114");
    }

    // =========================================================
    // MENU
    // =========================================================

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet(
            "Bedrock Pack Studio",
            "İptal",
            null,
            "📂 Proje Aç",
            "📦 Yeni Pack",
            "🖼️ Texture Editor",
            "</> Kod Editor",
            "⚙️ Ayarlar"
        );

        switch (action)
        {
            case "🖼️ Texture Editor":
                await OpenTextureEditor();
                break;

            case "</> Kod Editor":
                await OpenCodeEditor();
                break;

            case "📦 Yeni Pack":
                await DisplayAlert(
                    "Yeni Pack",
                    "Yeni resource pack oluşturma sistemi hazırlanıyor.",
                    "Tamam"
                );
                break;

            case "📂 Proje Aç":
                await DisplayAlert(
                    "Proje Aç",
                    "Proje açma sistemi sonraki aşamada eklenecek.",
                    "Tamam"
                );
                break;

            case "⚙️ Ayarlar":
                await DisplayAlert(
                    "Ayarlar",
                    "Tema, Minecraft sürümü ve editör ayarları burada olacak.",
                    "Tamam"
                );
                break;
        }
    }

    private async void OnMoreClicked(object sender, EventArgs e)
    {
        await DisplayActionSheet(
            "Seçenekler",
            "Kapat",
            null,
            "ℹ️ Hakkında",
            "🐛 Hata Raporla"
        );
    }

    // =========================================================
    // HOME
    // =========================================================

    private async void OnHomeClicked(object sender, EventArgs e)
    {
        if (MainScroll != null)
        {
            await MainScroll.ScrollToAsync(
                0,
                0,
                false
            );
        }
    }

    private async void OnPackClicked(object sender, EventArgs e)
    {
        await DisplayAlert(
            "Pack",
            "Pack Explorer sonraki aşamada burada olacak.",
            "Tamam"
        );
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await DisplayAlert(
            "Ayarlar",
            "Tema, Minecraft sürümü ve editör ayarları burada olacak.",
            "Tamam"
        );
    }

    // =========================================================
    // FILES
    // =========================================================

    private async void OnFilesClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet(
            "Dosyalar",
            "İptal",
            null,
            "📁 textures",
            "📄 manifest.json",
            "🖼️ pack_icon.png"
        );

        if (action == "📄 manifest.json")
        {
            await OpenCodeEditor();
        }
        else if (action == "📁 textures")
        {
            await OpenTextureEditor();
        }
    }

    // =========================================================
    // TEXTURE EDITOR
    // =========================================================

    private async void OnTextureClicked(object sender, EventArgs e)
    {
        await OpenTextureEditor();
    }

    private async Task OpenTextureEditor()
    {
        await Navigation.PushAsync(
            new TextureEditorPage()
        );
    }

    // =========================================================
    // CODE EDITOR
    // =========================================================

    private async void OnCodeClicked(object sender, EventArgs e)
    {
        await OpenCodeEditor();
    }

    private async Task OpenCodeEditor()
    {
        await Navigation.PushAsync(
            new CodeEditorPage()
        );
    }

    // =========================================================
    // MCPACK BUILD
    // =========================================================

    private async void OnBuildClicked(object sender, EventArgs e)
    {
        try
        {
            string tempDir = Path.Combine(
                FileSystem.CacheDirectory,
                "Pack_" + Guid.NewGuid().ToString("N")
            );

            Directory.CreateDirectory(tempDir);

            var manifest = new
            {
                format_version = 2,

                header = new
                {
                    name = "Test Pack",
                    description =
                        "Bedrock Pack Studio tarafından oluşturuldu.",
                    uuid = Guid.NewGuid().ToString(),
                    version = new[] { 1, 0, 0 },
                    min_engine_version = new[] { 1, 20, 0 }
                },

                modules = new[]
                {
                    new
                    {
                        type = "resources",
                        uuid = Guid.NewGuid().ToString(),
                        version = new[] { 1, 0, 0 }
                    }
                }
            };

            string manifestPath = Path.Combine(
                tempDir,
                "manifest.json"
            );

            string json = JsonSerializer.Serialize(
                manifest,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            );

            await File.WriteAllTextAsync(
                manifestPath,
                json
            );

            string finalPath = Path.Combine(
                FileSystem.AppDataDirectory,
                "Test_Pack.mcpack"
            );

            if (File.Exists(finalPath))
            {
                File.Delete(finalPath);
            }

            ZipFile.CreateFromDirectory(
                tempDir,
                finalPath
            );

            Directory.Delete(
                tempDir,
                true
            );

            _lastGeneratedPackPath = finalPath;

            if (PackStatusLabel != null)
            {
                PackStatusLabel.Text = "✓ .mcpack hazır";
                PackStatusLabel.TextColor =
                    Color.FromArgb("#4EC9B0");
            }

            await DisplayAlert(
                "Başarılı 🎉",
                "Test_Pack.mcpack oluşturuldu!",
                "Tamam"
            );

            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title = "Minecraft'a Aktar",
                    File = new ShareFile(finalPath)
                }
            );
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Hata",
                ex.Message,
                "Tamam"
            );
        }
    }
}
