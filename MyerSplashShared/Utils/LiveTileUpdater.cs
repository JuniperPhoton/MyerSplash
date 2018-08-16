using JP.Utils.Debug;
using MyerSplash.Data;
using NotificationsExtensions.Tiles;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Notifications;

namespace MyerSplashShared.Utils
{
    public static class LiveTileUpdater
    {
        public static async Task UpdateImagesTileAsync()
        {
            try
            {
                var tile = new TileBinding();
                var photosContent = new TileBindingContentPhotos();

                var url = UnsplashImageFactory.CreateTodayHighlightImage().Urls.Full;

                photosContent.Images.Add(new TileImageSource(url));

                tile.Content = photosContent;

                var tileContent = new TileContent
                {
                    Visual = new TileVisual()
                };
                tileContent.Visual.Branding = TileBranding.NameAndLogo;
                tileContent.Visual.TileMedium = tile;
                tileContent.Visual.TileWide = tile;
                tileContent.Visual.TileLarge = tile;

                await ClearAllTileFile();

                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                TileUpdateManager.CreateTileUpdaterForApplication().Update(
                    new TileNotification(tileContent.GetXml()));
            }
            catch (Exception e)
            {
                var task = Logger.LogAsync(e);
            }
        }

        public async static Task ClearAllTileFile()
        {
            var folder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("temptile", CreationCollisionOption.OpenIfExists);
            var files = await folder.GetFilesAsync(Windows.Storage.Search.CommonFileQuery.DefaultQuery);
            files.ToList().ForEach(async f => await f.DeleteAsync(StorageDeleteOption.PermanentDelete));
        }

        public static void CleanUpTile()
        {
            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
        }
    }
}