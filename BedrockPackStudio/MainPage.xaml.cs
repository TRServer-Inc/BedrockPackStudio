using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

namespace BedrockPackStudio;

public partial class MainPage : ContentPage
{
    private string _lastGeneratedPackPath = string.Empty;

    private string WorkspaceRoot =>
        Path.Combine(
            FileSystem.AppDataDirectory,
            "Projects");

    private string? CurrentProject =>
        ProjectContext.CurrentProjectPath;

    public MainPage()
    {
        InitializeComponent();

        BackgroundColor =
            Color.FromArgb("#101114");

        Directory.CreateDirectory(
            WorkspaceRoot);

        LoadLastProject();
        RefreshProjectUI();
    }

    // =========================================================
    // PROJECT STARTUP
    // =========================================================

    private void LoadLastProject()
    {
        string last =
            Preferences.Default.Get(
                "last_project",
                string.Empty);

        if (!string.IsNullOrWhiteSpace(last) &&
            Directory.Exists(last))
        {
            ProjectContext.CurrentProjectPath =
                last;
        }
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
                "📥 Pack İçe Aktar",
                "📦 Yeni Pack",

                "📁 Dosyalar",
                "📝 Kod Editörü",
                "🖼️ Texture Editörü",

                "📥 Mojang Texture",
                "📦 .MCPACK Oluştur",

                "💾 Kaydet",
                "⚙️ Ayarlar",
                "ℹ️ Hakkında");

        switch (action)
        {
            case "📂 Proje Aç":
                await OpenProject();
                break;

            case "📥 Pack İçe Aktar":
                await ImportPack();
                break;

            case "📦 Yeni Pack":
                await CreateNewPack();
                break;

            case "📁 Dosyalar":
                await OpenFilesMenu();
                break;

            case "📝 Kod Editörü":
                await OpenCodeEditor();
                break;

            case "🖼️ Texture Editörü":
                await OpenTextureEditor();
                break;

            case "📥 Mojang Texture":
                await ScrollToTextureSection();
                break;

            case "📦 .MCPACK Oluştur":
                await BuildMcpack();
                break;

            case "💾 Kaydet":
                await SaveProject();
                break;

            case "⚙️ Ayarlar":
                await OpenSettings();
                break;

            case "ℹ️ Hakkında":
                await ShowAbout();
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
                "🔄 Projeyi Yenile",
                "📦 .mcpack Oluştur",
                "🧹 Projeyi Kapat",
                "ℹ️ Hakkında");

        switch (action)
        {
            case "🔄 Projeyi Yenile":
                RefreshProjectUI();
                break;

            case "📦 .mcpack Oluştur":
                await BuildMcpack();
                break;

            case "🧹 Projeyi Kapat":
                CloseProject();
                break;

            case "ℹ️ Hakkında":
                await ShowAbout();
                break;
        }
    }

    // =========================================================
    // HOME
    // =========================================================

    private void OnHomeClicked(
        object sender,
        EventArgs e)
    {
        MainScroll.ScrollToAsync(
            0,
            0,
            false);
    }

    private async void OnPackClicked(
        object sender,
        EventArgs e)
    {
        if (!EnsureProject())
            return;

        await ScrollToFiles();
    }

    private async void OnSettingsClicked(
        object sender,
        EventArgs e)
    {
        await OpenSettings();
    }

    // =========================================================
    // NEW PACK
    // =========================================================

    private async void OnNewPackClicked(
        object sender,
        EventArgs e)
    {
        await CreateNewPack();
    }

    private async Task CreateNewPack()
    {
        string? name =
            await DisplayPrompt(
                "Yeni Pack",
                "Pack adı:",
                "My Resource Pack");

        if (string.IsNullOrWhiteSpace(name))
            return;

        name = SanitizeFileName(name);

        string folder =
            Path.Combine(
                WorkspaceRoot,
                name);

        if (Directory.Exists(folder))
        {
            await DisplayAlert(
                "Pack zaten var",
                "Bu isimde bir proje zaten mevcut.",
                "Tamam");

            return;
        }

        Directory.CreateDirectory(folder);

        Directory.CreateDirectory(
            Path.Combine(
                folder,
                "textures"));

        Directory.CreateDirectory(
            Path.Combine(
                folder,
                "textures",
                "blocks"));

        Directory.CreateDirectory(
            Path.Combine(
                folder,
                "textures",
                "items"));

        string headerUuid =
            Guid.NewGuid().ToString();

        string moduleUuid =
            Guid.NewGuid().ToString();

        var manifest = new
        {
            format_version = 2,

            header = new
            {
                name = name,
                description =
                    "Bedrock Pack Studio ile oluşturuldu.",
                uuid = headerUuid,
                version = new[] { 1, 0, 0 },
                min_engine_version =
                    new[] { 1, 20, 0 }
            },

            modules = new[]
            {
                new
                {
                    type = "resources",
                    uuid = moduleUuid,
                    version = new[] { 1, 0, 0 }
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
                folder,
                "manifest.json"),
            json);

        ProjectContext.CurrentProjectPath =
            folder;

        Preferences.Default.Set(
            "last_project",
            folder);

        Log(
            $"Yeni pack oluşturuldu: {name}");

        RefreshProjectUI();

        await DisplayAlert(
            "Başarılı 🎉",
            "Resource pack oluşturuldu.",
            "Tamam");
    }

    // =========================================================
    // OPEN PROJECT
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

            string path =
                result.FullPath;

            if (string.IsNullOrWhiteSpace(path) ||
                !File.Exists(path))
            {
                await DisplayAlert(
                    "Android",
                    "Dosyanın gerçek yolu alınamadı. .mcpack içe aktarmayı kullanabilirsin.",
                    "Tamam");

                return;
            }

            string? folder =
                Path.GetDirectoryName(path);

            if (string.IsNullOrWhiteSpace(folder))
                return;

            string manifest =
                Path.Combine(
                    folder,
                    "manifest.json");

            if (!File.Exists(manifest))
            {
                await DisplayAlert(
                    "Geçersiz Proje",
                    "manifest.json bulunamadı.",
                    "Tamam");

                return;
            }

            ProjectContext.CurrentProjectPath =
                folder;

            ProjectContext.CurrentFilePath =
                manifest;

            Preferences.Default.Set(
                "last_project",
                folder);

            Log("Proje açıldı.");

            RefreshProjectUI();
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

    // =========================================================
    // IMPORT MCPACK
    // =========================================================

    private async Task ImportPack()
    {
        try
        {
            FileResult? result =
                await FilePicker.Default.PickAsync();

            if (result == null)
                return;

            string projectName =
                SanitizeFileName(
                    Path.GetFileNameWithoutExtension(
                        result.FileName));

            string destination =
                Path.Combine(
                    WorkspaceRoot,
                    projectName);

            int number = 1;

            while (Directory.Exists(destination))
            {
                destination =
                    Path.Combine(
                        WorkspaceRoot,
                        $"{projectName}_{number}");

                number++;
            }

            Directory.CreateDirectory(
                destination);

            string temporaryZip =
                Path.Combine(
                    FileSystem.CacheDirectory,
                    Guid.NewGuid() + ".zip");

            await using (
                Stream input =
                    await result.OpenReadAsync())
            await using (
                FileStream output =
                    File.Create(
                        temporaryZip))
            {
                await input.CopyToAsync(output);
            }

            ZipFile.ExtractToDirectory(
                temporaryZip,
                destination);

            File.Delete(
                temporaryZip);

            string? manifest =
                FindFile(
                    destination,
                    "manifest.json");

            if (manifest == null)
            {
                Directory.Delete(
                    destination,
                    true);

                await DisplayAlert(
                    "Geçersiz Pack",
                    "manifest.json bulunamadı.",
                    "Tamam");

                return;
            }

            ProjectContext.CurrentProjectPath =
                destination;

            ProjectContext.CurrentFilePath =
                manifest;

            Preferences.Default.Set(
                "last_project",
                destination);

            Log(
                $"Pack içe aktarıldı: {result.FileName}");

            RefreshProjectUI();

            await DisplayAlert(
                "Başarılı 🎉",
                "Pack projeye aktarıldı.",
                "Tamam");
        }
        catch (Exception ex)
        {
            Log(
                "Import hatası: " +
                ex.Message);

            await DisplayAlert(
                "Import Hatası",
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
        await OpenFilesMenu();
    }

    private async Task OpenFilesMenu()
    {
        if (!EnsureProject())
            return;

        string action =
            await DisplayActionSheet(
                "Proje Dosyaları",
                "İptal",
                null,
                "📄 manifest.json",
                "📁 textures",
                "📁 textures/blocks",
                "📁 textures/items",
                "➕ Yeni Dosya",
                "📁 Yeni Klasör",
                "🗑️ Dosya Sil");

        switch (action)
        {
            case "📄 manifest.json":
                await OpenFileInEditor(
                    Path.Combine(
                        CurrentProject!,
                        "manifest.json"));
                break;

            case "📁 textures":
                await ShowFolderFiles(
                    Path.Combine(
                        CurrentProject!,
                        "textures"));
                break;

            case "📁 textures/blocks":
                await ShowFolderFiles(
                    Path.Combine(
                        CurrentProject!,
                        "textures",
                        "blocks"));
                break;

            case "📁 textures/items":
                await ShowFolderFiles(
                    Path.Combine(
                        CurrentProject!,
                        "textures",
                        "items"));
                break;

            case "➕ Yeni Dosya":
                await CreateNewFile();
                break;

            case "📁 Yeni Klasör":
                await CreateNewFolder();
                break;

            case "🗑️ Dosya Sil":
                await DeleteFile();
                break;
        }
    }

    private async Task ShowFolderFiles(
        string folder)
    {
        if (!Directory.Exists(folder))
        {
            await DisplayAlert(
                "Klasör",
                "Klasör bulunamadı.",
                "Tamam");

            return;
        }

        string[] files =
            Directory.GetFiles(
                folder,
                "*",
                SearchOption.TopDirectoryOnly);

        if (files.Length == 0)
        {
            await DisplayAlert(
                "Klasör boş",
                "Bu klasörde dosya yok.",
                "Tamam");

            return;
        }

        string[] names =
            files
                .Select(Path.GetFileName)
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .ToArray();

        string selected =
            await DisplayActionSheet(
                Path.GetFileName(folder),
                "Kapat",
                null,
                names);

        if (selected == "Kapat" ||
            string.IsNullOrWhiteSpace(selected))
            return;

        string path =
            Path.Combine(
                folder,
                selected);

        if (IsImage(path))
        {
            ProjectContext.CurrentTexturePath =
                path;

            await OpenTextureEditor();
        }
        else
        {
            await OpenFileInEditor(path);
        }
    }

    private async Task CreateNewFile()
    {
        if (!EnsureProject())
            return;

        string? name =
            await DisplayPrompt(
                "Yeni Dosya",
                "Dosya adı:",
                "example.json");

        if (string.IsNullOrWhiteSpace(name))
            return;

        string path =
            Path.Combine(
                CurrentProject!,
                name);

        if (File.Exists(path))
        {
            await DisplayAlert(
                "Hata",
                "Bu dosya zaten mevcut.",
                "Tamam");

            return;
        }

        string extension =
            Path.GetExtension(name);

        string content =
            extension.Equals(
                ".json",
                StringComparison.OrdinalIgnoreCase)
                ? "{}"
                : string.Empty;

        await File.WriteAllTextAsync(
            path,
            content);

        ProjectContext.CurrentFilePath =
            path;

        Log(
            $"Dosya oluşturuldu: {name}");

        RefreshProjectUI();

        await OpenFileInEditor(path);
    }

    private async Task CreateNewFolder()
    {
        if (!EnsureProject())
            return;

        string? name =
            await DisplayPrompt(
                "Yeni Klasör",
                "Klasör adı:",
                "textures");

        if (string.IsNullOrWhiteSpace(name))
            return;

        string path =
            Path.Combine(
                CurrentProject!,
                name);

        Directory.CreateDirectory(path);

        Log(
            $"Klasör oluşturuldu: {name}");

        RefreshProjectUI();
    }

    private async Task DeleteFile()
    {
        if (!EnsureProject())
            return;

        string[] files =
            Directory.GetFiles(
                CurrentProject!,
                "*",
                SearchOption.AllDirectories);

        if (files.Length == 0)
        {
            await DisplayAlert(
                "Dosyalar",
                "Silinecek dosya yok.",
                "Tamam");

            return;
        }

        string[] names =
            files
                .Select(x =>
                    Path.GetRelativePath(
                        CurrentProject!,
                        x))
                .ToArray();

        string selected =
            await DisplayActionSheet(
                "Dosya Sil",
                "İptal",
                null,
                names);

        if (selected == "İptal" ||
            string.IsNullOrWhiteSpace(selected))
            return;

        string path =
            Path.Combine(
                CurrentProject!,
                selected);

        bool confirm =
            await DisplayAlert(
                "Dosya Sil",
                $"{selected} silinsin mi?",
                "Sil",
                "İptal");

        if (!confirm)
            return;

        File.Delete(path);

        if (ProjectContext.CurrentFilePath
            ?.Equals(
                path,
                StringComparison.OrdinalIgnoreCase)
            == true)
        {
            ProjectContext.CurrentFilePath =
                null;
        }

        Log(
            $"Dosya silindi: {selected}");

        RefreshProjectUI();
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
        if (!EnsureProject())
            return;

        string? file =
            ProjectContext.CurrentFilePath;

        if (string.IsNullOrWhiteSpace(file) ||
            !File.Exists(file))
        {
            file =
                Path.Combine(
                    CurrentProject!,
                    "manifest.json");
        }

        if (!File.Exists(file))
        {
            await DisplayAlert(
                "Kod Editörü",
                "Açılacak dosya bulunamadı.",
                "Tamam");

            return;
        }

        ProjectContext.CurrentFilePath =
            file;

        await Navigation.PushAsync(
            new CodeEditorPage());
    }

    private async Task OpenFileInEditor(
        string path)
    {
        if (!File.Exists(path))
            return;

        ProjectContext.CurrentFilePath =
            path;

        await Navigation.PushAsync(
            new CodeEditorPage());
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
        if (!EnsureProject())
            return;

        await Navigation.PushAsync(
            new TextureEditorPage());
    }

    // =========================================================
    // MOJANG TEXTURE
    // =========================================================

    private async void OnDownloadTextureClicked(
        object sender,
        EventArgs e)
    {
        await DownloadTexture();
    }

    private async Task DownloadTexture()
    {
        if (!EnsureProject())
            return;

        string name =
            TextureSearchEntry.Text?
                .Trim()
                .ToLowerInvariant() ?? "";

        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert(
                "Texture",
                "Texture adı gir.",
                "Tamam");

            return;
        }

        if (!name.EndsWith(
                ".png",
                StringComparison.OrdinalIgnoreCase))
        {
            name += ".png";
        }

        string category =
            TextureCategoryPicker.SelectedIndex == 1
                ? "items"
                : "blocks";

        string url =
            $"https://raw.githubusercontent.com/Mojang/bedrock-samples/main/resource_pack/textures/{category}/{name}";

        try
        {
            Log(
                $"Texture indiriliyor: {name}");

            byte[] data =
                await new HttpClient()
                    .GetByteArrayAsync(url);

            string folder =
                Path.Combine(
                    CurrentProject!,
                    "textures",
                    category);

            Directory.CreateDirectory(folder);

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

            RefreshProjectUI();

            await DisplayAlert(
                "Başarılı 🎨",
                $"{name} indirildi.",
                "Aç");

            await OpenTextureEditor();
        }
        catch (Exception ex)
        {
            Log(
                "Texture hatası: " +
                ex.Message);

            await DisplayAlert(
                "Texture bulunamadı",
                "Texture indirilemedi.",
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
        await BuildMcpack();
    }

    private async Task BuildMcpack()
    {
        if (!EnsureProject())
            return;

        try
        {
            string manifest =
                Path.Combine(
                    CurrentProject!,
                    "manifest.json");

            if (!File.Exists(manifest))
            {
                await DisplayAlert(
                    "Build",
                    "manifest.json bulunamadı.",
                    "Tamam");

                return;
            }

            string json =
                await File.ReadAllTextAsync(
                    manifest);

            using JsonDocument document =
                JsonDocument.Parse(json);

            JsonElement root =
                document.RootElement;

            if (!root.TryGetProperty(
                    "header",
                    out JsonElement header))
            {
                await DisplayAlert(
                    "Build",
                    "manifest.json içinde header yok.",
                    "Tamam");

                return;
            }

            string packName =
                "BedrockPack";

            if (header.TryGetProperty(
                    "name",
                    out JsonElement name))
            {
                packName =
                    name.GetString()
                    ?? "BedrockPack";
            }

            string finalPath =
                Path.Combine(
                    FileSystem.AppDataDirectory,
                    SanitizeFileName(
                        packName) +
                    ".mcpack");

            if (File.Exists(finalPath))
                File.Delete(finalPath);

            ZipFile.CreateFromDirectory(
                CurrentProject!,
                finalPath,
                CompressionLevel.Fastest,
                false);

            _lastGeneratedPackPath =
                finalPath;

            PackStatusLabel.Text =
                "✓ .mcpack hazır";

            PackStatusLabel.TextColor =
                Color.FromArgb("#4EC9B0");

            Log(
                $".mcpack oluşturuldu: {Path.GetFileName(finalPath)}");

            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title =
                        "Minecraft'a Aktar",
                    File =
                        new ShareFile(finalPath)
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
    // SAVE
    // =========================================================

    private async Task SaveProject()
    {
        if (!EnsureProject())
            return;

        await DisplayAlert(
            "Proje",
            "Dosyalar çalışma alanına kaydediliyor.",
            "Tamam");

        Log("Proje kaydedildi.");
    }

    // =========================================================
    // REFRESH
    // =========================================================

    private async void OnRefreshClicked(
        object sender,
        EventArgs e)
    {
        RefreshProjectUI();

        await Task.CompletedTask;
    }

    private void RefreshProjectUI()
    {
        ProjectFilesLayout.Children.Clear();

        if (!ProjectContext.HasProject)
        {
            ProjectNameLabel.Text =
                "Proje Yok";

            ProjectTypeLabel.Text =
                "Resource Pack";

            PackStatusLabel.Text =
                "Yeni pack oluştur veya içe aktar";

            HeaderProjectLabel.Text =
                "Mobil Edition";

            return;
        }

        string project =
            ProjectContext.CurrentProjectPath!;

        string projectName =
            Path.GetFileName(project);

        ProjectNameLabel.Text =
            projectName;

        HeaderProjectLabel.Text =
            projectName;

        string manifest =
            Path.Combine(
                project,
                "manifest.json");

        if (File.Exists(manifest))
        {
            try
            {
                using JsonDocument document =
                    JsonDocument.Parse(
                        File.ReadAllText(
                            manifest));

                ProjectTypeLabel.Text =
                    "Resource Pack";

                PackStatusLabel.Text =
                    "v" +
                    GetVersion(
                        document.RootElement);
            }
            catch
            {
                PackStatusLabel.Text =
                    "manifest.json okunamadı";
            }
        }

        AddFileButton(
            "📄",
            "manifest.json",
            manifest);

        string textures =
            Path.Combine(
                project,
                "textures");

        if (Directory.Exists(textures))
        {
            AddFolderButton(
                "📁",
                "textures");

            string blocks =
                Path.Combine(
                    textures,
                    "blocks");

            string items =
                Path.Combine(
                    textures,
                    "items");

            if (Directory.Exists(blocks))
            {
                AddFolderButton(
                    "🧱",
                    "textures/blocks");
            }

            if (Directory.Exists(items))
            {
                AddFolderButton(
                    "🎒",
                    "textures/items");
            }
        }

        string[] rootFiles =
            Directory.GetFiles(
                project,
                "*",
                SearchOption.TopDirectoryOnly);

        foreach (string file in rootFiles)
        {
            if (Path.GetFileName(file)
                .Equals(
                    "manifest.json",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            AddFileButton(
                GetFileIcon(file),
                Path.GetFileName(file),
                file);
        }
    }

    private void AddFileButton(
        string icon,
        string name,
        string path)
    {
        if (!File.Exists(path))
            return;

        Button button =
            new()
            {
                Text =
                    $"{icon}   {name}",

                HorizontalOptions =
                    LayoutOptions.Fill,

                HeightRequest = 52,

                Padding =
                    new Thickness(
                        15,
                        0),

                BackgroundColor =
                    Colors.Transparent,

                TextColor =
                    Colors.White
            };

        button.Clicked +=
            async (_, _) =>
            {
                if (IsImage(path))
                {
                    ProjectContext.CurrentTexturePath =
                        path;

                    await OpenTextureEditor();
                }
                else
                {
                    await OpenFileInEditor(
                        path);
                }
            };

        ProjectFilesLayout.Children.Add(
            button);

        ProjectFilesLayout.Children.Add(
            new BoxView
            {
                HeightRequest = 1,

                BackgroundColor =
                    Color.FromArgb(
                        "#292D34")
            });
    }

    private void AddFolderButton(
        string icon,
        string name)
    {
        Button button =
            new()
            {
                Text =
                    $"{icon}   {name}",

                HorizontalOptions =
                    LayoutOptions.Fill,

                HeightRequest = 52,

                Padding =
                    new Thickness(
                        15,
                        0),

                BackgroundColor =
                    Colors.Transparent,

                TextColor =
                    Colors.White
            };

        button.Clicked +=
            async (_, _) =>
            {
                string relative =
                    name.Replace(
                        '/',
                        Path.DirectorySeparatorChar);

                string path =
                    Path.Combine(
                        CurrentProject!,
                        relative);

                await ShowFolderFiles(
                    path);
            };

        ProjectFilesLayout.Children.Add(
            button);

        ProjectFilesLayout.Children.Add(
            new BoxView
            {
                HeightRequest = 1,

                BackgroundColor =
                    Color.FromArgb(
                        "#292D34")
            });
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
                "🧹 Projeyi Kapat",
                "🗑️ Çalışma Alanını Temizle");

        switch (action)
        {
            case "🧹 Projeyi Kapat":
                CloseProject();
                break;

            case "🗑️ Çalışma Alanını Temizle":

                bool confirm =
                    await DisplayAlert(
                        "Dikkat",
                        "Tüm çalışma alanı silinecek.",
                        "Sil",
                        "İptal");

                if (!confirm)
                    break;

                if (Directory.Exists(
                        WorkspaceRoot))
                {
                    Directory.Delete(
                        WorkspaceRoot,
                        true);
                }

                Directory.CreateDirectory(
                    WorkspaceRoot);

                ProjectContext.Clear();

                Preferences.Default.Remove(
                    "last_project");

                RefreshProjectUI();

                Log(
                    "Çalışma alanı temizlendi.");

                break;
        }
    }

    // =========================================================
    // ABOUT
    // =========================================================

    private async Task ShowAbout()
    {
        await DisplayAlert(
            "Bedrock Pack Studio",
            "Mobil Edition\n\n" +
            "Minecraft Bedrock Resource Pack Editor\n\n" +
            "Proje • Kod • Texture • MCPACK",
            "Tamam");
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private bool EnsureProject()
    {
        if (ProjectContext.HasProject)
            return true;

        _ = DisplayAlert(
            "Proje Yok",
            "Önce bir resource pack oluştur veya içe aktar.",
            "Tamam");

        return false;
    }

    private void CloseProject()
    {
        ProjectContext.Clear();

        Preferences.Default.Remove(
            "last_project");

        RefreshProjectUI();

        Log(
            "Proje kapatıldı.");
    }

    private async Task ScrollToTextureSection()
    {
        await MainScroll.ScrollToAsync(
            TextureSearchEntry,
            ScrollToPosition.Center,
            true);
    }

    private async Task ScrollToFiles()
    {
        await MainScroll.ScrollToAsync(
            ProjectFilesLayout,
            ScrollToPosition.Center,
            true);
    }

    private void Log(string message)
    {
        string line =
            $"[{DateTime.Now:HH:mm:ss}] {message}";

        if (string.IsNullOrWhiteSpace(
            LogLabel.Text) ||
            LogLabel.Text == "Hazır.")
        {
            LogLabel.Text =
                line;
        }
        else
        {
            LogLabel.Text +=
                Environment.NewLine +
                line;
        }
    }

    private static string? FindFile(
        string root,
        string fileName)
    {
        return Directory
            .GetFiles(
                root,
                fileName,
                SearchOption.AllDirectories)
            .FirstOrDefault();
    }

    private static bool IsImage(
        string path)
    {
        string extension =
            Path.GetExtension(path)
                .ToLowerInvariant();

        return extension is
            ".png" or
            ".jpg" or
            ".jpeg";
    }

    private static string GetFileIcon(
        string path)
    {
        if (IsImage(path))
            return "🖼️";

        string extension =
            Path.GetExtension(path)
                .ToLowerInvariant();

        return extension switch
        {
            ".json" => "📄",
            ".txt" => "📝",
            ".lang" => "🌐",
            _ => "📄"
        };
    }

    private static string GetVersion(
        JsonElement root)
    {
        try
        {
            JsonElement version =
                root
                    .GetProperty("header")
                    .GetProperty("version");

            return string.Join(
                ".",
                version
                    .EnumerateArray()
                    .Select(x =>
                        x.GetInt32()));
        }
        catch
        {
            return "1.0.0";
        }
    }

    private static string SanitizeFileName(
        string value)
    {
        foreach (char c in
                 Path.GetInvalidFileNameChars())
        {
            value =
                value.Replace(
                    c,
                    '_');
        }

        return value;
    }

    private static async Task<string?> DisplayPrompt(
        string title,
        string message,
        string initial)
    {
        return await Application.Current!
            .Windows[0]
            .Page!
            .DisplayPromptAsync(
                title,
                message,
                "Tamam",
                "İptal",
                initialValue: initial);
    }
}
