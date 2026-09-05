using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Maui.Controls;

namespace BedrockPackStudio
{
    public partial class MainPage : ContentPage
    {
        private string _lastGeneratedPackPath = string.Empty;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnBuildPackClicked(object sender, EventArgs e)
        {
            string packName = PackNameEntry.Text?.Trim() ?? "";
            string packDesc = PackDescriptionEditor.Text?.Trim() ?? "";
            string packVersion = PackVersionEntry.Text?.Trim() ?? "1.0.0";

            if (string.IsNullOrEmpty(packName))
            {
                await DisplayAlert("Hata", "Lütfen bir paket adı girin!", "Tamam");
                return;
            }

            try
            {
                StatusLabel.Text = "Paket hazırlanıyor...";
                StatusLabel.TextColor = Colors.Yellow;

                // Geçici çalışma dizini
                string tempDir = Path.Combine(FileSystem.CacheDirectory, "PackBuild_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                // manifest.json oluşturma
                var manifestData = new
                {
                    format_version = 2,
                    header = new
                    {
                        name = packName,
                        description = packDesc,
                        uuid = Guid.NewGuid().ToString(),
                        version = ParseVersion(packVersion),
                        min_engine_version = new[] { 1, 20, 0 }
                    },
                    modules = new[]
                    {
                        new
                        {
                            type = "resources",
                            uuid = Guid.NewGuid().ToString(),
                            version = ParseVersion(packVersion)
                        }
                    }
                };

                string manifestJson = JsonSerializer.Serialize(manifestData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(tempDir, "manifest.json"), manifestJson);

                // Hedef .mcpack dosyası
                string outputDir = FileSystem.AppDataDirectory;
                string packFileName = $"{packName.Replace(" ", "_")}.mcpack";
                string finalPackPath = Path.Combine(outputDir, packFileName);

                if (File.Exists(finalPackPath))
                    File.Delete(finalPackPath);

                // Zip olarak paketle (.mcpack)
                ZipFile.CreateFromDirectory(tempDir, finalPackPath);

                // Temizlik
                Directory.Delete(tempDir, true);

                _lastGeneratedPackPath = finalPackPath;
                StatusLabel.Text = $"Başarıyla Oluşturuldu: {packFileName}";
                StatusLabel.TextColor = Colors.LightGreen;

                await DisplayAlert("Başarılı", $"Paket oluşturuldu!\nKonum: {packFileName}", "Harika");
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "Hata oluştu!";
                StatusLabel.TextColor = Colors.Red;
                await DisplayAlert("Hata", $"Paket oluşturulurken bir sorun çıktı: {ex.Message}", "Tamam");
            }
        }

        private async void OnExportClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_lastGeneratedPackPath) || !File.Exists(_lastGeneratedPackPath))
            {
                await DisplayAlert("Uyarı", "Önce bir paket oluşturmalısınız!", "Tamam");
                return;
            }

            try
            {
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Minecraft Paketini Paylaş / İçe Aktar",
                    File = new ShareFile(_lastGeneratedPackPath)
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Dosya paylaşılamadı: {ex.Message}", "Tamam");
            }
        }

        private int[] ParseVersion(string versionStr)
        {
            try
            {
                var parts = versionStr.Split('.');
                int major = parts.Length > 0 ? int.Parse(parts[0]) : 1;
                int minor = parts.Length > 1 ? int.Parse(parts[1]) : 0;
                int patch = parts.Length > 2 ? int.Parse(parts[2]) : 0;
                return new[] { major, minor, patch };
            }
            catch
            {
                return new[] { 1, 0, 0 };
            }
        }
    }
}
