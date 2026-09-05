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

        BackgroundColor =
            Color.FromArgb("#101114");

        Log("Bedrock Pack Studio başlatıldı.");
    }

    // =========================================================
    // MENU
    // =========================================================

    private async void OnMenuClicked(
        object sender,
        EventArgs e)
    {
        string action =
            await DisplayActionSheet(
                "Bedrock Pack Studio",
                "İptal",
                null,
                "📂 Proje Aç",
                "📦 Yeni Pack",
                "🖼️ Texture Editor",
                "</> Kod Editor",
                "⚙️ Ayarlar");

        switch (action)
        {
            case "📂 Proje Aç":
                await OpenProject();
                break;

            case "📦 Yeni Pack":
                await CreateNewPack();
                break;

            case "🖼️ Texture Editor":
                await OpenTextureEditor();
                break;

            case "</> Kod Editor":
                await OpenCodeEditor();
                break;

            case "⚙️ Ayarlar":
                await OpenSettings();
                break;
        }
    }

    private async void OnMoreClicked(
        object sender,
        EventArgs e)
    {
        string action =
            await DisplayActionSheet(
                "Seçenekler",
                "Kapat",
                null,
                "ℹ️ Hakkında",
                "🐛 Hata Raporla");

        switch (action)
        {
            case "ℹ️ Hakkında":

                await DisplayAlert(
                    "Bedrock Pack Studio",
                    "Minecraft Bedrock Resource Pack Editor\n\n" +
                    "Mobil Edition",
                    "Tamam");

                break;

            case "🐛 Hata Raporla":

                await DisplayAlert(
                    "Hata Raporla",
                    "Hata raporlama sistemi yakında eklenecek.",
                    "Tamam");

                break;
        }
    }

    // =========================================================
    // HOME
    // =========================================================

    private async void OnHomeClicked(
        object sender,
        EventArgs e)
    {
        await MainScroll.ScrollToAsync(
            0,
            0,
            false);
    }

    private async void OnPackClicked(
        object sender,
        EventArgs e)
    {
        await DisplayAlert(
            "Pack",
            "Pack Explorer burada olacak.",
            "Tamam");
    }

    private async void OnSettingsClicked(
        object sender,
        EventArgs e)
    {
        await OpenSettings();
    }

    // =========================================================
    // PROJECT
    // =========================================================

    private async void OnOpenProjectClicked(
        object sender,
        EventArgs e)
    {
        await OpenProject();
    }

    private async Task OpenProject()
    {
        try
        {
            FileResult? result =
                await FilePicker.Default.PickAsync(
                    new PickOptions
                    {
                        PickerTitle =
                            "manifest.json seç"
                    });

            if (result == null)
                return;

            Log(
                $"Seçilen dosya: {result.FileName}");

            await DisplayAlert(
                "Proje",
                $"{result.FileName} seçildi.\n\n" +
                "Android dosya erişimi için proje içe aktarma sistemi sonraki aşamada bağlanacak.",
                "Tamam");
        }
        catch (Exception ex)
        {
            Log(
                "Proje açma hatası: " +
                ex.Message);

            await DisplayAlert(
                "Hata",
                ex.Message,
                "Tamam");
        }
    }

    private async void OnNewPackClicked(
        object sender,
        EventArgs e)
    {
        await CreateNewPack();
    }

    private async Task CreateNewPack()
    {
        string packName =
            "My Resource Pack";

        string root =
            Path.Combine(
                FileSystem.AppDataDirectory,
                "Projects");

        string project =
            Path.Combine(
                root,
                packName);

        try
        {
            Directory.CreateDirectory(
                project);

            Directory.CreateDirectory(
                Path.Combine(
                    project,
                    "textures"));

            Directory.CreateDirectory(
                Path.Combine(
                    project,
                    "textures",
                    "blocks"));

            Directory.CreateDirectory(
                Path.Combine(
                    project,
                    "textures",
                    "items"));

            var manifest = new
            {
                format_version = 2,

                header = new
                {
                    name = packName,
                    description =
                        "Bedrock Pack Studio ile oluşturuldu.",
                    uuid =
                        Guid.NewGuid().ToString(),
                    version =
                        new[]
                        {
                            1,
                            0,
                            0
                        },
                    min_engine_version =
                        new[]
                        {
                            1,
                            20,
                            0
                        }
                },

                modules = new[]
                {
                    new
                    {
                        type = "resources",
                        uuid =
                            Guid.NewGuid().ToString(),
                        version =
                            new[]
                            {
                                1,
                                0,
                                0
                            }
                    }
                }
            };

            string json =
                JsonSerializer.Serialize(
                    manifest,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            await File.WriteAllTextAsync(
                Path.Combine(
                    project,
                    "manifest.json"),
                json);

            ProjectContext.CurrentProjectPath =
                project;

            ProjectContext.CurrentFilePath =
                Path.Combine(
                    project,
                    "manifest.json");

            ProjectContext.CurrentTexturePath =
                null;

            ProjectNameLabel.Text =
                packName;

            ProjectTypeLabel.Text =
                "Resource Pack";

            PackStatusLabel.Text =
                "v1.0.0";

            HeaderProjectLabel.Text =
                packName;

            Log(
                $"Yeni pack oluşturuldu: {packName}");

            await DisplayAlert(
                "Başarılı 🎉",
                "Yeni resource pack oluşturuldu.",
                "Tamam");
        }
        catch (Exception ex)
        {
            Log(
                "Pack oluşturma hatası: " +
                ex.Message);

            await DisplayAlert(
                "Hata",
                ex.Message,
                "Tamam");
        }
    }

    // =========================================================
    // FILES
    // =========================================================

    private async void OnFilesClicked(
        object sender,
        EventArgs e)
    {
        string action =
            await DisplayActionSheet(
                "Dosyalar",
                "İptal",
                null,
                "📁 textures",
                "📄 manifest.json",
                "🖼️ pack_icon.png");

        switch (action)
        {
            case "📄 manifest.json":
                await OpenCodeEditor();
                break;

            case "📁 textures":
                await OpenTextureEditor();
                break;
        }
    }

    // =========================================================
    // TEXTURE EDITOR
    // =========================================================

    private async void OnTextureClicked(
        object sender,
        EventArgs e)
    {
        await OpenTextureEditor();
    }

    private async Task OpenTextureEditor()
    {
        try
        {
            await Navigation.PushAsync(
                new TextureEditorPage());
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Texture Editor Hatası",
                ex.Message,
                "Tamam");
        }
    }

    // =========================================================
    // CODE EDITOR
    // =========================================================

    private async void OnCodeClicked(
        object sender,
        EventArgs e)
    {
        await OpenCodeEditor();
    }

    private async Task OpenCodeEditor()
    {
        try
        {
            await Navigation.PushAsync(
                new CodeEditorPage());
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Kod Editörü Hatası",
                ex.Message,
                "Tamam");
        }
    }

    // =========================================================
    // MOJANG TEXTURE
    // =========================================================

    private async void OnDownloadTextureClicked(
        object sender,
        EventArgs e)
    {
        string name =
            TextureSearchEntry.Text?
                .Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert(
                "Texture",
                "Önce texture adı yaz.",
                "Tamam");

            return;
        }

        string category =
            TextureCategoryPicker.SelectedIndex == 1
                ? "items"
                : "blocks";

        if (!name.EndsWith(
                ".png",
                StringComparison.OrdinalIgnoreCase))
        {
            name += ".png";
        }

        string url =
            "https://raw.githubusercontent.com/" +
            "Mojang/bedrock-samples/main/" +
            "resource_pack/textures/" +
            category +
            "/" +
            name;

        try
        {
            Log(
                $"Texture indiriliyor: {name}");

            using HttpClient client =
                new();

            byte[] data =
                await client.GetByteArrayAsync(
                    url);

            string folder =
                Path.Combine(
                    FileSystem.AppDataDirectory,
                    "Projects",
                    "My Resource Pack",
                    "textures",
                    category);

            Directory.CreateDirectory(
                folder);

            string path =
                Path.Combine(
                    folder,
                    name);

            await File.WriteAllBytesAsync(
                path,
                data);

            ProjectContext.CurrentTexturePath =
                path;

            Log(
                $"Texture indirildi: {name}");

            await DisplayAlert(
                "Başarılı 🎨",
                $"{name} indirildi.",
                "Tamam");
        }
        catch (Exception ex)
        {
            Log(
                "Texture indirme hatası: " +
                ex.Message);

            await DisplayAlert(
                "Texture Hatası",
                "Texture indirilemedi.\n\n" +
                ex.Message,
                "Tamam");
        }
    }

    // =========================================================
    // MCPACK
    // =========================================================

    private async void OnBuildClicked(
        object sender,
        EventArgs e)
    {
        try
        {
            string? project =
                ProjectContext.CurrentProjectPath;

            if (string.IsNullOrWhiteSpace(project) ||
                !Directory.Exists(project))
            {
                await DisplayAlert(
                    "Build",
                    "Önce bir pack oluştur.",
                    "Tamam");

                return;
            }

            string manifest =
                Path.Combine(
                    project,
                    "manifest.json");

            if (!File.Exists(manifest))
            {
                await DisplayAlert(
                    "Build",
                    "manifest.json bulunamadı.",
                    "Tamam");

                return;
            }

            string finalPath =
                Path.Combine(
                    FileSystem.AppDataDirectory,
                    "BedrockPack.mcpack");

            if (File.Exists(finalPath))
                File.Delete(finalPath);

            ZipFile.CreateFromDirectory(
                project,
                finalPath,
                CompressionLevel.Fastest,
                false);

            _lastGeneratedPackPath =
                finalPath;

            PackStatusLabel.Text =
                "✓ .mcpack hazır";

            PackStatusLabel.TextColor =
                Color.FromArgb(
                    "#4EC9B0");

            Log(
                ".mcpack oluşturuldu.");

            await DisplayAlert(
                "Başarılı 🎉",
                "BedrockPack.mcpack oluşturuldu.",
                "Tamam");

            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title =
                        "Minecraft'a Aktar",

                    File =
                        new ShareFile(
                            finalPath)
                });
        }
        catch (Exception ex)
        {
            Log(
                "Build hatası: " +
                ex.Message);

            await DisplayAlert(
                "Build Hatası",
                ex.Message,
                "Tamam");
        }
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    private async Task OpenSettings()
    {
        string action =
            await DisplayActionSheet(
                "Ayarlar",
                "Kapat",
                null,
                "🌙 Koyu Tema",
                "ℹ️ Hakkında");

        switch (action)
        {
            case "🌙 Koyu Tema":

                await DisplayAlert(
                    "Tema",
                    "Koyu tema zaten aktif.",
                    "Tamam");

                break;

            case "ℹ️ Hakkında":

                await DisplayAlert(
                    "Bedrock Pack Studio",
                    "Minecraft Bedrock Resource Pack Editor\n\n" +
                    "Mobil Edition",
                    "Tamam");

                break;
        }
    }

    // =========================================================
    // REFRESH
    // =========================================================

    private async void OnRefreshClicked(
        object sender,
        EventArgs e)
    {
        Log("Proje görünümü yenilendi.");

        await Task.CompletedTask;
    }

    // =========================================================
    // LOG
    // =========================================================

    private void Log(
        string message)
    {
        string line =
            $"[{DateTime.Now:HH:mm:ss}] {message}";

        if (string.IsNullOrWhiteSpace(
                LogLabel.Text) ||
            LogLabel.Text == "Hazır.")
        {
            LogLabel.Text =
                line;

            return;
        }

        LogLabel.Text +=
            Environment.NewLine +
            line;
    }
}
