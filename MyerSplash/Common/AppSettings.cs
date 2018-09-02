using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using MyerSplash.ViewModel;
using MyerSplashCustomControl;
using MyerSplashShared.Utils;
using System;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace MyerSplash.Common
{
    public class AppSettings : ViewModelBase
    {
        public ApplicationDataContainer LocalSettings { get; set; }

        private MainViewModel MainVM
        {
            get
            {
                return SimpleIoc.Default.GetInstance<MainViewModel>();
            }
        }

        public Windows.UI.Xaml.Media.Brush MainPageBackgroundBrush
        {
            get
            {
                if (EnableCompactMode)
                {
                    return new SolidColorBrush(Colors.Black);
                }
                else
                {
                    if (IsFcuOrAbove())
                    {
                        return App.Current.Resources["SystemControlChromeLowAcrylicWindowBrush"] as Windows.UI.Xaml.Media.Brush;
                    }
                    else
                    {
                        return App.Current.Resources["CustomAcrylicWindowBrush"] as Windows.UI.Xaml.Media.Brush;
                    }
                }
            }
        }

        public Windows.UI.Xaml.Media.Brush MainTopNavigationBackgroundBrush
        {
            get
            {
                if (EnableCompactMode)
                {
                    return App.Current.Resources["CustomAcrylicInAppBrushTrans"] as Windows.UI.Xaml.Media.Brush;
                }
                else
                {
                    if (IsFcuOrAbove())
                    {
                        return App.Current.Resources["SystemControlChromeLowAcrylicWindowBrush"] as Windows.UI.Xaml.Media.Brush;
                    }
                    else
                    {
                        return App.Current.Resources["AppBackgroundBrushDark"] as Windows.UI.Xaml.Media.Brush;
                    }
                }
            }
        }

        private Thickness _imageMargin;
        public Thickness ImageMargin
        {
            get
            {
                return _imageMargin;
            }
            set
            {
                if (value != _imageMargin)
                {
                    _imageMargin = value;
                    RaisePropertyChanged(() => ImageMargin);
                }
            }
        }

        private Thickness _imageListPadding;
        public Thickness ImageListPadding
        {
            get
            {
                return _imageListPadding;
            }
            set
            {
                if (value != _imageListPadding)
                {
                    _imageListPadding = value;
                    RaisePropertyChanged(() => ImageListPadding);
                }
            }
        }

        public bool EnableCompactMode
        {
            get
            {
                // No option for Xbox.
                if (DeviceUtil.IsXbox) return true;
                return ReadSettings(nameof(EnableCompactMode), false);
            }
            set
            {
                SaveSettings(nameof(EnableCompactMode), value);
                RaisePropertyChanged(() => EnableCompactMode);
                RaisePropertyChanged(() => MainPageBackgroundBrush);
                RaisePropertyChanged(() => MainTopNavigationBackgroundBrush);

                if (value)
                {
                    ImageMargin = new Thickness(0);
                    ImageListPadding = new Thickness(0);
                }
                else
                {
                    ImageMargin = new Thickness(8);
                    ImageListPadding = new Thickness(8, 0, 8, 0);
                }
            }
        }

        public bool EnableTile
        {
            get
            {
                return ReadSettings(nameof(EnableTile), true);
            }
            set
            {
                SaveSettings(nameof(EnableTile), value);
                RaisePropertyChanged(() => EnableTile);
                if (!value)
                {
                    LiveTileUpdater.CleanUpTile();
                }
            }
        }

        public bool EnableTodayRecommendation
        {
            get
            {
                return true;
            }
        }

        public bool EnableQuickDownload
        {
            get
            {
                return ReadSettings(nameof(EnableQuickDownload), false);
            }
            set
            {
                SaveSettings(nameof(EnableQuickDownload), value);
                RaisePropertyChanged(() => EnableQuickDownload);
            }
        }

        public bool EnableScaleAnimation
        {
            get
            {
                return ReadSettings(nameof(EnableScaleAnimation), true);
            }
            set
            {
                SaveSettings(nameof(EnableScaleAnimation), value);
                RaisePropertyChanged(() => EnableScaleAnimation);
            }
        }

        public string SaveFolderPath
        {
            get
            {
                return ReadSettings(nameof(SaveFolderPath), "");
            }
            set
            {
                SaveSettings(nameof(SaveFolderPath), value);
                RaisePropertyChanged(() => SaveFolderPath);
            }
        }

        public int DefaultCategory
        {
            get
            {
                return ReadSettings(nameof(DefaultCategory), 0);
            }
            set
            {
                SaveSettings(nameof(DefaultCategory), value);
                RaisePropertyChanged(() => DefaultCategory);
            }
        }

        public int BackgroundWallpaperSource
        {
            get
            {
                return ReadSettings(nameof(BackgroundWallpaperSource), 0);
            }
            set
            {
                SaveSettings(nameof(BackgroundWallpaperSource), value);
                RaisePropertyChanged(() => BackgroundWallpaperSource);
                switch (value)
                {
                    case 0:
                        var task0 = BackgroundTaskRegister.UnregisterAsync();
                        break;
                    case 1:
                    // fall through
                    case 2:
                    // fall through
                    case 3:
                        var task1 = BackgroundTaskRegister.RegisterAsync();
                        break;
                }
            }
        }

        public int LoadQuality
        {
            get
            {
                return 0;
            }
        }

        public int SaveQuality
        {
            get
            {
                return ReadSettings(nameof(SaveQuality), 1);
            }
            set
            {
                SaveSettings(nameof(SaveQuality), value);
                RaisePropertyChanged(() => SaveQuality);
            }
        }

        public AppSettings()
        {
            LocalSettings = ApplicationData.Current.LocalSettings;
            EnableCompactMode = EnableCompactMode;
        }

        public static bool IsFcuOrAbove()
        {
            return ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5);
        }

        public async Task<StorageFolder> GetSavingFolderAsync()
        {
            var folder = await KnownFolders.PicturesLibrary.CreateFolderAsync("MyerSplash", CreationCollisionOption.OpenIfExists);
            return folder;
        }

        public async Task<StorageFolder> GetWallpaperFolderAsync()
        {
            var folder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("WallpapersTemp", CreationCollisionOption.OpenIfExists);
            return folder;
        }

        private void SaveSettings(string key, object value)
        {
            LocalSettings.Values[key] = value;
        }

        private T ReadSettings<T>(string key, T defaultValue)
        {
            if (LocalSettings.Values.ContainsKey(key))
            {
                return (T)LocalSettings.Values[key];
            }
            if (defaultValue != null)
            {
                return defaultValue;
            }
            return default(T);
        }

        private static readonly Lazy<AppSettings> lazy = new Lazy<AppSettings>(() => new AppSettings());

        public static AppSettings Instance { get { return lazy.Value; } }
    }
}