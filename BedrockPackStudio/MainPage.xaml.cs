using System;
using System.IO;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;

namespace BedrockPackStudio
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnBuildAndExportClicked(object sender, EventArgs e)
        {
            try
            {
                string cacheDir = FileSystem.CacheDirectory;
                string mcpackPath = Path.Combine(cacheDir, "BedrockPack.mcpack");

                if (File.Exists(mcpackPath))
                {
                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = "Minecraft ile Aç",
                        File = new ShareFile(mcpackPath, "application/x-minecraft-pack")
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", ex.Message, "Tamam");
            }
        }
    }
}
