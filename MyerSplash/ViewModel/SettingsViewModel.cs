using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MyerSplash.Common;
using MyerSplash.View.Uc;
using MyerSplashCustomControl;
using MyerSplashShared.Utils;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace MyerSplash.ViewModel
{
    public class SettingsViewModel : ViewModelBase
    {
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

        private RelayCommand _diagnoseCommand;
        public RelayCommand DiagnoseCommand
        {
            get
            {
                if (_diagnoseCommand != null) return _diagnoseCommand;
                return _diagnoseCommand = new RelayCommand(async () =>
                {
                    var uc = new NetworkDiagnosisDialog();
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

        private bool _clearCacheCommandEnabled;
        public bool ClearCacheCommandEnabled
        {
            get
            {
                return _clearCacheCommandEnabled;
            }
            set
            {
                if (_clearCacheCommandEnabled != value)
                {
                    _clearCacheCommandEnabled = value;
                    RaisePropertyChanged(() => ClearCacheCommandEnabled);
                }
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

        private string _cacheHint = "Clean up cache";
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

        public SettingsViewModel()
        {
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

            ToastService.SendToast("Temp files have been cleaned up.");
        }

        public async Task CalculateCacheAsync()
        {
            ClearCacheCommandEnabled = false;

            ulong size = 0;
            var tempFiles = await CacheUtil.GetTempFolder().GetItemsAsync();
            foreach (var file in tempFiles)
            {
                var properties = await file.GetBasicPropertiesAsync();
                size += properties.Size;
                CacheHint = $"Clean up cache ({(size / (1024 * 1024)).ToString("f0")} MB)";
            }

            ClearCacheCommandEnabled = true;
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