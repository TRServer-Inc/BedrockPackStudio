using System.IO.Compression;
using System.Text.Json;

namespace BedrockPackStudio;

public partial class MainPage : ContentPage
{
    private static readonly HttpClient Http = new();

    private string WorkspaceRoot =>
        Path.Combine(FileSystem.AppDataDirectory, "Projects");

    private string? CurrentProject =>
        ProjectContext.CurrentProjectPath;

    public MainPage()
    {
        InitializeComponent();

        TextureCategoryPicker.SelectedIndex = 0;

        Directory.CreateDirectory(WorkspaceRoot);

        LoadLastProject();
        RefreshProjectUI();
    }

    // =========================================================
    // STARTUP
    // =========================================================

    private void LoadLastProject()
    {
        string? last =
            Preferences.Default.Get("last_project", string.Empty);

        if (!string.IsNullOrWhiteSpace(last) &&
            Directory.Exists(last))
        {
            ProjectContext.CurrentProjectPath = last;
        }
    }

    // =========================================================
    // MENU
    // =========================================================

    private async void OnMenuClicked(
        object sender,
        EventArgs e)
    {
        string action = await DisplayActionSheet(
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

            "💾 Projeyi Kaydet",
            "⚙️ Ayarlar",
            "ℹ️ Hakkında"
        );

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

            case "💾 Projeyi Kaydet":
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
        string action = await DisplayActionSheet(
            "Diğer",
            "Kapat",
            null,
            "🔄 Projeyi Yenile",
            "📦 .mcpack Oluştur",
            "🗑️ Projeyi Kapat",
            "ℹ️ Hakkında"
        );

        switch (action)
        {
            case "🔄 Projeyi Yenile":
                RefreshProjectUI();
                break;

            case "📦 .mcpack Oluştur":
                await BuildMcpack();
                break;

            case "🗑️ Projeyi Kapat":
                CloseProject();
                break;

            case "ℹ️ Hakkında":
                await ShowAbout();
                break;
        }
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
        string? name = await DisplayPrompt(
            "Yeni Pack",
            "Pack adı:",
            "Bedrock Pack");

        if (string.IsNullOrWhiteSpace(name))
            return;

        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        string folder =
            Path.Combine(
                WorkspaceRoot,
                name);

        if (Directory.Exists(folder))
        {
            await DisplayAlert(
                "Pack zaten var",
                "Bu isimde bir proje zaten bulunuyor.",
                "Tamam");

            return;
        }

        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(
            Path.Combine(folder, "textures"));
        Directory.CreateDirectory(
            Path.Combine(folder, "textures", "blocks"));
        Directory.CreateDirectory(
            Path.Combine(folder, "textures", "items"));

        string headerUuid =
            Guid.NewGuid().ToString();

        string moduleUuid =
            Guid.NewGuid().ToString();

        var manifest = new
        {
            format_version = 2,

            header = new
            {
                name,
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
            Path.Combine(folder, "manifest.json"),
            json);

        ProjectContext.CurrentProjectPath =
            folder;

        Preferences.Default.Set(
            "last_project",
            folder);

        Log($"Yeni pack oluşturuldu: {name}");

        RefreshProjectUI();

        await DisplayAlert(
            "Başarılı 🎉",
            "Yeni resource pack oluşturuldu.",
            "Tamam");
    }

    // =========================================================
    // IMPORT MCPACK
    // =========================================================

    private async Task ImportPack()
    {
        try
        {
            FileResult? result =
                await FilePicker.Default.PickAsync(
                    new PickOptions
                    {
                        PickerTitle =
                            "Minecraft pack seç",
                        FileTypes =
                            new FilePickerFileType(
                                new Dictionary<DevicePlatform, IEnumerable<string>>
                                {
                                    {
                                        DevicePlatform.Android,
                                        new[]
                                        {
                                            "application/zip",
                                            "application/octet-stream",
                                            "*/*"
                                        }
                                    }
                                })
                    });

            if (result == null)
                return;

            string destinationName =
                Path.GetFileNameWithoutExtension(
                    result.FileName);

            foreach (char c in Path.GetInvalidFileNameChars())
                destinationName =
                    destinationName.Replace(c, '_');

            string destination =
                Path.Combine(
                    WorkspaceRoot,
                    destinationName);

            int suffix = 1;

            while (Directory.Exists(destination))
            {
                destination =
                    Path.Combine(
                        WorkspaceRoot,
                        $"{destinationName}_{suffix++}");
            }

            Directory.CreateDirectory(destination);

            using Stream input =
                await result.OpenReadAsync();

            string tempZip =
                Path.Combine(
                    FileSystem.CacheDirectory,
                    Guid.NewGuid() + ".zip");

            using (FileStream output =
                   File.Create(tempZip))
            {
                await input.CopyToAsync(output);
            }

            ZipFile.ExtractToDirectory(
                tempZip,
                destination);

            File.Delete(tempZip);

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

            Preferences.Default.Set(
                "last_project",
                destination);

            Log($"Pack içe aktarıldı: {result.FileName}");

            RefreshProjectUI();

            await DisplayAlert(
                "Başarılı 🎉",
                "Pack projeye aktarıldı.",
                "Tamam");
        }
        catch (Exception ex)
        {
            Log("Import hatası: " + ex.Message);

            await DisplayAlert(
                "Hata",
                ex.Message,
                "Tamam");
        }
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
                    "Bu dosyanın gerçek dosya yolu alınamadı. Pack'i .mcpack olarak içe aktarmayı deneyebilirsin.",
                    "Tamam");

                return;
            }

            string? folder =
                Path.GetDirectoryName(path);

            if (folder == null)
                return;

            string? manifest =
                FindFile(folder, "manifest.json");

            if (manifest == null)
            {
                await DisplayAlert(
                    "Geçersiz Proje",
                    "manifest.json bulunamadı.",
                    "Tamam");

                return;
            }

            ProjectContext.CurrentProjectPath =
                folder;

            Preferences.Default.Set(
                "last_project",
                folder);

            Log("Proje açıldı.");

            RefreshProjectUI();
        }
        catch (Exception ex)
        {
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
        if (!EnsureProject())
            return;

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
        Directory.CreateDirectory(folder);

        string[] files =
            Directory.GetFiles(
                folder,
                "*",
                SearchOption.TopDirectoryOnly);

        if (files.Length == 0)
        {
            await DisplayAlert(
                "Klasör boş",
                "Bu klasörde dosya bulunmuyor.",
                "Tamam");

            return;
        }

        string[] names =
            files.Select(Path.GetFileName)
                 .Where(x => x != null)
                 .Cast<string>()
                 .ToArray();

        string selected =
            await DisplayActionSheet(
                Path.GetFileName(folder),
                "Kapat",
                null,
                names);

        if (selected == "Kapat")
            return;

        string selectedPath =
            Path.Combine(
                folder,
                selected);

        if (selectedPath.EndsWith(
                ".png",
                StringComparison.OrdinalIgnoreCase) ||
            selectedPath.EndsWith(
                ".jpg",
                StringComparison.OrdinalIgnoreCase) ||
            selectedPath.EndsWith(
                ".jpeg",
                StringComparison.OrdinalIgnoreCase))
        {
            ProjectContext.CurrentTexturePath =
                selectedPath;

            await OpenTextureEditor();
        }
        else
        {
            await OpenFileInEditor(
                selectedPath);
        }
    }

    private async Task CreateNewFile()
    {
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
                "Bu dosya zaten var.",
                "Tamam");

            return;
        }

        await File.WriteAllTextAsync(
            path,
            "{}");

        Log($"Dosya oluşturuldu: {name}");

        RefreshProjectUI();

        await OpenFileInEditor(path);
    }

    private async Task CreateNewFolder()
    {
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

        Log($"Klasör oluşturuldu: {name}");

        RefreshProjectUI();
    }

    private async Task DeleteFile()
    {
        string[] files =
            Directory.GetFiles(
                CurrentProject!,
                "*",
                SearchOption.AllDirectories);

        if (files.Length == 0)
            return;

        string[] names =
            files.Select(x =>
                    Path.GetRelativePath(
                        CurrentProject!,
                        x))
                .ToArray();

        string selected =
            await DisplayActionSheet(
                "Silinecek dosya",
                "İptal",
                null,
                names);

        if (selected == "İptal")
            return;

        string path =
            Path.Combine(
                CurrentProject!,
                selected);

        bool confirm =
            await DisplayAlert(
                "Sil",
                $"{selected} silinsin mi?",
                "Sil",
                "İptal");

        if (!confirm)
            return;

        File.Delete(path);

        Log($"Dosya silindi: {selected}");

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

        string manifest =
            Path.Combine(
                CurrentProject!,
                "manifest.json");

        if (!File.Exists(manifest))
        {
            await DisplayAlert(
                "Dosya yok",
                "manifest.json bulunamadı.",
                "Tamam");

            return;
        }

        ProjectContext.CurrentFilePath =
            manifest;

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
    // MOJANG TEXTURE DOWNLOAD
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

        if (!name.EndsWith(".png"))
            name += ".png";

        string category =
            TextureCategoryPicker.SelectedIndex == 1
                ? "items"
                : "blocks";

        string url =
            $"https://raw.githubusercontent.com/Mojang/bedrock-samples/main/resource_pack/textures/{category}/{name}";

        try
        {
            Log($"Texture indiriliyor: {name}");

            byte[] data =
                await Http.GetByteArrayAsync(url);

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

            Log($"Texture indirildi: {name}");

            RefreshProjectUI();

            await DisplayAlert(
                "Başarılı 🎨",
                $"{name} indirildi.",
                "Aç");

            await OpenTextureEditor();
        }
        catch
        {
            await DisplayAlert(
                "Texture bulunamadı",
                "Mojang Bedrock Samples içerisinde bu texture bulunamadı.",
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
                    "Hata",
                    "manifest.json bulunamadı.",
                    "Tamam");

                return;
            }

            string json =
                await File.ReadAllTextAsync(
                    manifest);

            JsonDocument document =
                JsonDocument.Parse(json);

            if (!document.RootElement
                    .TryGetProperty(
                        "header",
                        out _))
            {
                await DisplayAlert(
                    "Hata",
                    "manifest.json geçerli bir Bedrock pack manifesti değil.",
                    "Tamam");

                return;
            }

            string name =
                GetPackName(
                    document.RootElement);

            string output =
                Path.Combine(
                    FileSystem.AppDataDirectory,
                    $"{SanitizeFileName(name)}.mcpack");

            if (File.Exists(output))
                File.Delete(output);

            ZipFile.CreateFromDirectory(
                CurrentProject!,
                output,
                CompressionLevel.Fastest,
                false);

            PackStatusLabel.Text =
                "✓ .mcpack hazır";

            PackStatusLabel.TextColor =
                Color.FromArgb("#4EC9B0");

            Log(
                $".mcpack oluşturuldu: {Path.GetFileName(output)}");

            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title =
                        "Minecraft'a Aktar",
                    File =
                        new ShareFile(output)
                });
        }
        catch (Exception ex)
        {
            Log("Build hatası: " + ex.Message);

            await DisplayAlert(
                "Build Hatası",
                ex.Message,
                "Tamam");
        }
    }

    private static string GetPackName(
        JsonElement root)
    {
        if (root.TryGetProperty(
                "header",
                out JsonElement header) &&
            header.TryGetProperty(
                "name",
                out JsonElement name))
        {
            return name.GetString()
                   ?? "BedrockPack";
        }

        return "BedrockPack";
    }

    // =========================================================
    // SAVE
    // =========================================================

    private async Task SaveProject()
    {
        if (!EnsureProject())
            return;

        await DisplayAlert(
            "Kaydedildi",
            "Proje dosyaları zaten çalışma alanına kaydediliyor.",
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
                "Yeni proje oluştur veya içe aktar";

            HeaderProjectLabel.Text =
                "Mobil Edition";

            return;
        }

        string project =
            ProjectContext.CurrentProjectPath!;

        string name =
            Path.GetFileName(project);

        ProjectNameLabel.Text =
            name;

        HeaderProjectLabel.Text =
            name;

        string manifest =
            Path.Combine(
                project,
                "manifest.json");

        if (File.Exists(manifest))
        {
            try
            {
                using JsonDocument doc =
                    JsonDocument.Parse(
                        File.ReadAllText(manifest));

                ProjectTypeLabel.Text =
                    "Resource Pack";

                string version =
                    GetVersion(
                        doc.RootElement);

                PackStatusLabel.Text =
                    $"v{version}";
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

        AddFolderButton(
            "📁",
            "textures");

        string textures =
            Path.Combine(
                project,
                "textures");

        if (Directory.Exists(textures))
        {
            AddFolderButton(
                "🧱",
                "textures/blocks");

            AddFolderButton(
                "🎒",
                "textures/items");
        }

        string[] files =
            Directory.GetFiles(
                project,
                "*",
                SearchOption.TopDirectoryOnly);

        foreach (string file in files)
        {
            if (Path.GetFileName(file)
                .Equals(
                    "manifest.json",
                    StringComparison.OrdinalIgnoreCase))
                continue;

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
                HorizontalContentAlignment =
                    TextAlignment.Start,
                HeightRequest = 52,
                Padding = new Thickness(
                    15,
                    0),
                BackgroundColor =
                    Colors.Transparent,
                TextColor =
                    Colors.White
            };

        button.Clicked += async (_, _) =>
        {
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
        };

        ProjectFilesLayout.Children.Add(button);

        ProjectFilesLayout.Children.Add(
            new BoxView
            {
                HeightRequest = 1,
                BackgroundColor =
                    Color.FromArgb("#292D34")
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
                HorizontalContentAlignment =
                    TextAlignment.Start,
                HeightRequest = 52,
                Padding = new Thickness(
                    15,
                    0),
                BackgroundColor =
                    Colors.Transparent,
                TextColor =
                    Colors.White
            };

        button.Clicked += async (_, _) =>
        {
            string path =
                Path.Combine(
                    CurrentProject!,
                    name.Replace(
                        '/',
                        Path.DirectorySeparatorChar));

            if (Directory.Exists(path))
                await ShowFolderFiles(path);
        };

        ProjectFilesLayout.Children.Add(button);

        ProjectFilesLayout.Children.Add(
            new BoxView
            {
                HeightRequest = 1,
                BackgroundColor =
                    Color.FromArgb("#292D34")
            });
    }

    // =========================================================
    // BOTTOM NAV
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

        await MainScroll.ScrollToAsync(
            ProjectFilesLayout,
            ScrollToPosition.Start,
            true);
    }

    private async void OnSettingsClicked(
        object sender,
        EventArgs e)
    {
        await OpenSettings();
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
                "🗑️ Tüm Çalışma Alanını Temizle");

        switch (action)
        {
            case "🧹 Projeyi Kapat":
                CloseProject();
                break;

            case "🗑️ Tüm Çalışma Alanını Temizle":
                bool confirm =
                    await DisplayAlert(
                        "Dikkat",
                        "Çalışma alanındaki tüm projeler silinecek.",
                        "Sil",
                        "İptal");

                if (confirm)
                {
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

                    Log("Çalışma alanı temizlendi.");
                }

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
            "Proje yönetimi • Kod editörü • Texture editörü • .mcpack",
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
            "Proje yok",
            "Önce yeni bir pack oluştur veya mevcut bir pack'i içe aktar.",
            "Tamam");

        return false;
    }

    private void CloseProject()
    {
        ProjectContext.Clear();

        Preferences.Default.Remove(
            "last_project");

        RefreshProjectUI();

        Log("Proje kapatıldı.");
    }

    private async Task ScrollToTextureSection()
    {
        await MainScroll.ScrollToAsync(
            TextureSearchEntry,
            ScrollToPosition.Center,
            true);
    }

    private void Log(string message)
    {
        string line =
            $"[{DateTime.Now:HH:mm:ss}] {message}";

        if (string.IsNullOrWhiteSpace(
            LogLabel.Text))
        {
            LogLabel.Text = line;
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
        string ext =
            Path.GetExtension(path)
                .ToLowerInvariant();

        return ext is
            ".png" or
            ".jpg" or
            ".jpeg";
    }

    private static string GetFileIcon(
        string path)
    {
        if (IsImage(path))
            return "🖼️";

        string ext =
            Path.GetExtension(path)
                .ToLowerInvariant();

        return ext switch
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
                version.EnumerateArray()
                       .Select(x => x.GetInt32()));
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
