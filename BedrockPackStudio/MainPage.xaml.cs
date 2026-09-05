using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.ApplicationModel;

namespace BedrockPackStudio
{
    public partial class MainPage : ContentPage
    {
        private Entry _packNameEntry = null!;
        private Editor _packDescEditor = null!;
        private Entry _packVersionEntry = null!;
        private Label _statusLabel = null!;
        private string _lastGeneratedPackPath = string.Empty;

        public MainPage()
        {
            InitializeComponent();
            BuildUI();
        }

        private void BuildUI()
        {
            BackgroundColor = Color.FromArgb("#121212");

            _packNameEntry = new Entry
            {
                Placeholder = "Örn: Custom Pack",
                PlaceholderColor = Color.FromArgb("#666666"),
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#2D2D2D")
            };

            _packDescEditor = new Editor
            {
                Placeholder = "Paket açıklamasını buraya yazın...",
                PlaceholderColor = Color.FromArgb("#666666"),
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#2D2D2D"),
                HeightRequest = 80
            };

            _packVersionEntry = new Entry
            {
                Text = "1.0.0",
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#2D2D2D")
            };

            var buildButton = new Button
            {
                Text = "📦 .mcpack Oluştur",
                BackgroundColor = Color.FromArgb("#0E639C"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 8,
                HeightRequest = 50
            };
            buildButton.Clicked += OnBuildPackClicked;

            var exportButton = new Button
            {
                Text = "📁 Minecraft'a Aktar / Paylaş",
                BackgroundColor = Color.FromArgb("#3C3C3C"),
                TextColor = Colors.White,
                CornerRadius = 8,
                HeightRequest = 45
            };
            exportButton.Clicked += OnExportClicked;

            _statusLabel = new Label
            {
                Text = "Hazır.",
                TextColor = Color.FromArgb("#4EC9B0"),
                FontSize = 13,
                HorizontalOptions = LayoutOptions.Center
            };

            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Padding = 20,
                    Spacing = 12,
                    Children =
                    {
                        new Label { Text = "Bedrock Pack Studio", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = "Minecraft Bedrock Paketi Oluşturucu", FontSize = 12, TextColor = Color.FromArgb("#AAAAAA"), HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = "Paket Adı", TextColor = Color.FromArgb("#4EC9B0"), FontAttributes = FontAttributes.Bold },
                        _packNameEntry,
                        new Label { Text = "Paket Açıklaması", TextColor = Color.FromArgb("#4EC9B0"), FontAttributes = FontAttributes.Bold },
                        _packDescEditor,
                        new Label { Text = "Versiyon", TextColor = Color.FromArgb("#4EC9B0"), FontAttributes = FontAttributes.Bold },
                        _packVersionEntry,
                        buildButton,
                        exportButton,
                        _statusLabel
                    }
                }
            };
        }

        private async void OnBuildPackClicked(object? sender, EventArgs? e)
        {
            string packName = _packNameEntry.Text?.Trim() ?? "";
            string packDesc = _packDescEditor.Text?.Trim() ?? "";
            string packVersion = _packVersionEntry.Text?.Trim() ?? "1.0.0";

            if (string.IsNullOrEmpty(packName))
            {
                await DisplayAlert("Hata", "Paket adı boş olamaz!", "Tamam");
                return;
            }

            try
            {
                _statusLabel.Text = "Oluşturuluyor...";
                _statusLabel.TextColor = Colors.Yellow;

                string tempDir = Path.Combine(FileSystem.CacheDirectory, "Pack_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                var manifest = new
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

                File.WriteAllText(Path.Combine(tempDir, "manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

                string finalPath = Path.Combine(FileSystem.AppDataDirectory, $"{packName.Replace(" ", "_")}.mcpack");
                if (File.Exists(finalPath)) File.Delete(finalPath);

                ZipFile.CreateFromDirectory(tempDir, finalPath);
                Directory.Delete(tempDir, true);

                _lastGeneratedPackPath = finalPath;
                _statusLabel.Text = "Başarıyla oluşturuldu!";
                _statusLabel.TextColor = Colors.LightGreen;

                await DisplayAlert("Başarılı", "Paket hazır!", "Tamam");
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Hata oluştu!";
                _statusLabel.TextColor = Colors.Red;
                await DisplayAlert("Hata", ex.Message, "Tamam");
            }
        }

        private async void OnExportClicked(object? sender, EventArgs? e)
        {
            if (string.IsNullOrEmpty(_lastGeneratedPackPath) || !File.Exists(_lastGeneratedPackPath))
            {
                await DisplayAlert("Uyarı", "Önce paket oluşturmalısın!", "Tamam");
                return;
            }

            try
            {
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Minecraft'a Aktar",
                    File = new ShareFile(_lastGeneratedPackPath)
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", ex.Message, "Tamam");
            }
        }

        private int[] ParseVersion(string v)
        {
            try
            {
                var p = v.Split('.');
                return new[] { int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]) };
            }
            catch { return new[] { 1, 0, 0 }; }
        }
    }
}
