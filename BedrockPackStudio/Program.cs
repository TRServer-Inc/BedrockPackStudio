using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;

namespace BedrockPackStudio
{
    public static class Program
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>();

            return builder.Build();
        }
    }
}
