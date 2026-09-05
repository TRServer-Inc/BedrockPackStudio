using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using System.IO.Compression;
using System.Text.Json;

namespace BedrockPackStudio
{
    public class MainPage : ContentPage
    {
        private Grid _mainLayout;
        private bool _uiLoaded = false;

        private Editor _projectNameEditor;
        private Image _packIconSourceImage;
        private string _selectedImagePath = string.Empty;

        public MainPage()
        {
            // Arka planı siyah kalmasın diye açıkça beyaz yapıyoruz
            BackgroundColor = Colors.White;

            _mainLayout = new Grid
            {
                RowDefinitions = new RowDefinitionCollection { new RowDefinition(GridLength.Star) },
                ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition(GridLength.Star) },
                BackgroundColor = Colors.White
            };

            Content = _mainLayout;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Mavi splash ekrandan sonra UI rendering'i ana thread'e alarak siyah ekrana düşmesini engelliyoruz
            if (!_uiLoaded)
            {
                Dispatcher.Dispatch(() =>
                {
                    try
                    {
                        BuildUI();
                        _uiLoaded = true;
                    }
                    catch (Exception ex)
                    {
                        AddErrorLabel(ex.Message);
                    }
                });
            }
        }

        private void BuildUI()
        {
            var verticalStackLayout = new VerticalStackLayout
            {
                Spacing = 20,
                Padding = 20,
                VerticalOptions = LayoutOptions.Center
            };

            var appLogo = new Image
            {
                Source = "appiconfg.png",
                HeightRequest = 120,
                WidthRequest = 120,
                HorizontalOptions = LayoutOptions.Center
            };
            verticalStackLayout.Add(appLogo);

            var packStudioTitle = new Label
            {
                Text = "PackStudio V1.0",
                TextColor = Colors.Black,
                FontAttributes = FontAttributes.Bold,
                FontSize = 32,
                HorizontalOptions = LayoutOptions.Center
            };
            verticalStackLayout.Add(packStudioTitle);

            var packDescriptionLabel = new Label
            {
                Text = "Minecraft Bedrock paketi oluşturucu.",
                TextColor = Colors.DarkGray,
                FontSize = 16,
                HorizontalOptions = LayoutOptions.Center
            };
            verticalStackLayout.Add(packDescriptionLabel);

            _projectNameEditor = new Editor
            {
                Placeholder = "Proje Adı",
                PlaceholderColor = Colors.LightGray,
                TextColor = Colors.Black,
                HeightRequest = 60,
                HorizontalOptions = LayoutOptions.Fill
            };
            verticalStackLayout.Add(_projectNameEditor);

            _packIconSourceImage = new Image
            {
                Source = "packicon_source_logo_demo.png",
                HeightRequest = 100,
                WidthRequest = 100,
                HorizontalOptions = LayoutOptions.Center
            };
            verticalStackLayout.Add(_packIconSourceImage);

            var packIconSourceSelectButton = new Button
            {
                Text = "Resim Seç",
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#512BD4"),
                CornerRadius = 10,
                HeightRequest = 50,
                HorizontalOptions = LayoutOptions.Center
            };
            packIconSourceSelectButton.Clicked += OnSelectImageClicked;
            verticalStackLayout.Add(packIconSourceSelectButton);

            var createNewPackButton = new Button
            {
                Text = "Paket Oluştur",
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#FF512BD4"),
                CornerRadius = 10,
                HeightRequest = 60,
                HorizontalOptions = LayoutOptions.Fill
            };
            createNewPackButton.Clicked += OnCreatePackClicked;
            verticalStackLayout.Add(createNewPackButton);

            _mainLayout.Clear();
            _mainLayout.Add(verticalStackLayout);
        }

        private async void OnSelectImageClicked(object? sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Bir simge resmi seçin",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    _selectedImagePath = result.FullPath;
                    _packIconSourceImage.Source = ImageSource.FromFile(_selectedImagePath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Resim seçilemedi: {ex.Message}", "Tamam");
            }
        }

        private async void OnCreatePackClicked(object? sender, EventArgs e)
        {
            string packName = _projectNameEditor.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(packName))
            {
                await DisplayAlert("Hata", "Lütfen bir proje adı girin.", "Tamam");
                return;
            }

            try
            {
                string tempDir = Path.Combine(FileSystem.CacheDirectory, "pack_build_" + Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                var manifest = new
                {
                    format_version = 2,
                    header = new
                    {
                        name = packName,
                        description = "BedrockPackStudio ile oluşturuldu.",
                        uuid = Guid.NewGuid().ToString(),
                        version = new[] { 1, 0, 0 },
                        min_engine_version = new[] { 1, 16, 0 }
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

                string manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(tempDir, "manifest.json"), manifestJson);

                if (!string.IsNullOrEmpty(_selectedImagePath) && File.Exists(_selectedImagePath))
                {
                    File.Copy(_selectedImagePath, Path.Combine(tempDir, "pack_icon.png"), true);
                }

                string zipPath = Path.Combine(FileSystem.CacheDirectory, $"{packName}.mcpack");
                if (File.Exists(zipPath)) File.Delete(zipPath);

                ZipFile.CreateFromDirectory(tempDir, zipPath);
                Directory.Delete(tempDir, true);

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "MCPACK Paketini Paylaş",
                    File = new ShareFile(zipPath)
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Paket oluşturulurken hata çıktı: {ex.Message}", "Tamam");
            }
        }

        private void AddErrorLabel(string message)
        {
            _mainLayout.Clear();
            _mainLayout.Add(new Label
            {
                Text = $"Açılış hatası: {message}",
                TextColor = Colors.Red,
                FontSize = 18,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            });
        }
    }
}
