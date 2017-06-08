using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using MyerSplashShared.API;
using MyerSplashCustomControl;
using MyerSplash.Common;
using Windows.System;
using MyerSplash.UC;

namespace MyerSplash.ViewModel
{
    public class SettingsViewModel : ViewModelBase
    {
        public static string SAVING_POSITION = "SAVING_POSITION";
        public static string DEFAULT_SAVING_POSITION = "\\Pictures\\MyerSplash (Can't be modified)";

        private RelayCommand _backgroundWallpaperHelpCommand;
        public RelayCommand BackgroundWallpaperHelpCommand
        {
            get
            {
                if (_backgroundWallpaperHelpCommand != null) return _backgroundWallpaperHelpCommand;
                return _backgroundWallpaperHelpCommand = new RelayCommand(async () =>
                  {
                      var uc = new BackgroundHintDialog();
                      await PopupService.Instance.ShowAsync(uc);
                  });
            }
        }

        private RelayCommand _clearCacheCommand;
        public RelayCommand ClearCacheCommand
        {
            get
            {
                if (_clearCacheCommand != null) return _clearCacheCommand;
                return _clearCacheCommand = new RelayCommand(async () =>
                {
                    await ClearCacheAsync();
                });
            }
        }

        private RelayCommand _clearTempCommand;
        public RelayCommand ClearTempCommand
        {
            get
            {
                if (_clearTempCommand != null) return _clearTempCommand;
                return _clearTempCommand = new RelayCommand(async () =>
                  {
                      await ClearTempFileAsync();
                  });
            }
        }

        private string _savingPositionPath;
        public string SavingPositionPath
        {
            get
            {
                return _savingPositionPath;
            }
            set
            {
                if (_savingPositionPath != value)
                {
                    _savingPositionPath = value;
                    RaisePropertyChanged(() => SavingPositionPath);
                }
            }
        }

        private string _cacheHint;
        public string CacheHint
        {
            get
            {
                return _cacheHint;
            }
            set
            {
                if (_cacheHint != value)
                {
                    _cacheHint = value;
                    RaisePropertyChanged(() => CacheHint);
                }
            }
        }

        private RelayCommand _cpenSavingFolderCommand;
        public RelayCommand OpenSavingFolderCommand
        {
            get
            {
                if (_cpenSavingFolderCommand != null) return _cpenSavingFolderCommand;
                return _cpenSavingFolderCommand = new RelayCommand(async () =>
                  {
                      var folder = await AppSettings.Instance.GetSavingFolderAsync();
                      if (folder != null)
                      {
                          await Launcher.LaunchFolderAsync(folder);
                      }
                  });
            }
        }

        private RelayCommand _toggleScaleAnimationCommand;
        public RelayCommand ToggleScaleAnimationCommand
        {
            get
            {
                if (_toggleScaleAnimationCommand != null) return _toggleScaleAnimationCommand;
                return _toggleScaleAnimationCommand = new RelayCommand(() =>
                  {
                      App.AppSettings.EnableScaleAnimation = !App.AppSettings.EnableScaleAnimation;
                  });
            }
        }

        private RelayCommand _toggleLiveTileCommand;
        public RelayCommand ToggleLiveTileCommand
        {
            get
            {
                if (_toggleLiveTileCommand != null) return _toggleLiveTileCommand;
                return _toggleLiveTileCommand = new RelayCommand(() =>
                {
                    App.AppSettings.EnableTile = !App.AppSettings.EnableTile;
                });
            }
        }

        public SettingsViewModel()
        {
            SavingPositionPath = DEFAULT_SAVING_POSITION;
            CacheHint = "Clean up cache";
        }

        private async Task ClearTempFileAsync()
        {
            var folder = await AppSettings.Instance.GetSavingFolderAsync();
            var files = await folder.GetFilesAsync();
            if (files != null)
            {
                foreach (var file in files)
                {
                    var prop = await file.GetBasicPropertiesAsync();
                    if (file.Name.EndsWith(".tmp") || prop.Size == 0)
                    {
                        await file.DeleteAsync();
                    }
                }
            }

            ToastService.SendToast("Temp files have been cleaned.");
        }

        public async Task CalculateCacheAsync()
        {
            ulong size = 0;
            var tempFiles = await CacheUtil.GetTempFolder().GetItemsAsync();
            foreach (var file in tempFiles)
            {
                var properties = await file.GetBasicPropertiesAsync();
                size += properties.Size;
                CacheHint = $"Clean up cache ({(size / (1024 * 1024)).ToString("f0")} MB)";
            }
        }

        private async Task ClearCacheAsync()
        {
            CacheHint = $"Clean up cache (0 MB)";
            ToastService.SendToast("All clear.", TimeSpan.FromMilliseconds(1000));

            var localFiles = await CacheUtil.GetCachedFileFolder().GetItemsAsync();
            foreach (var file in localFiles)
            {
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }

            var tempFiles = await CacheUtil.GetTempFolder().GetItemsAsync();
            foreach (var file in tempFiles)
            {
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }

        }
    }
}
