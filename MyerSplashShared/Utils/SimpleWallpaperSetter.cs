using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System.UserProfile;

namespace MyerSplashShared.Utils
{
    public static class SimpleWallpaperSetter
    {
        public static async Task<bool> DownloadAndSetAsync(string url)
        {
            if (!UserProfilePersonalizationSettings.IsSupported())
            {
                return false;
            }

            try
            {
                if (!string.IsNullOrEmpty(url))
                {
                    var client = new HttpClient();

                    var fileName = Path.GetFileName(url);

                    var pictureLib = await KnownFolders.PicturesLibrary.CreateFolderAsync("MyerSplash", 
                        CreationCollisionOption.OpenIfExists);
                    var targetFolder = await pictureLib.CreateFolderAsync("Auto-change wallpapers", 
                        CreationCollisionOption.OpenIfExists);

                    var localFolder = ApplicationData.Current.LocalFolder;

                    // Download
                    if (!(await localFolder.TryGetItemAsync(fileName) is StorageFile file))
                    {
                        Debug.WriteLine($"===========url {url}==============");

                        await LiveTileUpdater.UpdateLiveTileAsync();

                        var imageResp = await client.GetAsync(url);
                        using (var stream = await imageResp.Content.ReadAsStreamAsync())
                        {
                            Debug.WriteLine($"===========download complete==============");

                            file = await targetFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                            var bytes = new byte[stream.Length];
                            await stream.ReadAsync(bytes, 0, (int)stream.Length);
                            await FileIO.WriteBytesAsync(file, bytes);

                            // File must be in local folder
                            file = await file.CopyAsync(localFolder, fileName, NameCollisionOption.ReplaceExisting);

                            Debug.WriteLine($"===========save complete==============");
                        }
                        if (file != null)
                        {
                            var setResult = false;
                            var value = (int)ApplicationData.Current.LocalSettings.Values["BackgroundWallpaperSource"];
                            switch (value)
                            {
                                case 0:
                                    break;
                                case 1:
                                    setResult = await UserProfilePersonalizationSettings.Current.TrySetWallpaperImageAsync(file);
                                    break;
                                case 2:
                                    setResult = await UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync(file);
                                    break;
                                case 3:
                                    var setDesktopResult = await UserProfilePersonalizationSettings.Current.TrySetWallpaperImageAsync(file);
                                    var setLockscreenResult = await UserProfilePersonalizationSettings.Current.TrySetLockScreenImageAsync(file);
                                    setResult = setDesktopResult && setLockscreenResult;
                                    break;
                            }
                            Debug.WriteLine($"===========TrySetWallpaperImageAsync result{setResult}=============");
                            return setResult;
                        }
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"===========TrySetWallpaperImageAsync failed {e.Message}=============");
                return false;
            }
        }
    }
}