using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using JP.Utils.Debug;
using MyerSplash.Common;
using MyerSplash.Common.Composition;
using MyerSplash.Model;
using MyerSplash.ViewModel;
using MyerSplashCustomControl;
using MyerSplashShared.Utils;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace MyerSplash.View.Uc
{
    public sealed partial class PhotoDetailControl : UserControl, INotifyPropertyChanged
    {
        public DownloadsViewModel DownloadsVM
        {
            get
            {
                return SimpleIoc.Default.GetInstance<DownloadsViewModel>();
            }
        }

        public event EventHandler<EventArgs> OnHidden;

        public event PropertyChangedEventHandler PropertyChanged;

        private Compositor _compositor;
        private Visual _detailGridVisual;
        private Visual _maskBorderGridVisual;
        private Visual _shareBtnVisual;
        private Visual _infoGridVisual;
        private Visual _loadingPath;
        private Visual _flipperVisual;
        private Visual _taskbarImageVisual;
        private Visual _lockScreenImageVisual;
        private Visual _previewBtnVisual;
        private Visual _setAsSPVisual;
        private Visual _exifInfoVisual;
        private Visual _operationSPVisual;
        private Visual _detailContentGridVisual;

        private CancellationTokenSource _cts;
        private int _showingPreview = 0;

        private bool _showingExif;
        private bool _hideAfterHidingExif;
        private bool _detailContentGirdSizeSpecified;
        private bool _animating;

        private float _listItemAspectRatio = 1.5f;

        private ImageItem _currentImage;
        public ImageItem CurrentImage
        {
            get
            {
                return _currentImage;
            }
            set
            {
                if (_currentImage != value)
                {
                    _currentImage = value;
                    RaisePropertyChanged(nameof(CurrentImage));
                }
            }
        }

        public bool IsShown { get; set; }

        private void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public PhotoDetailControl()
        {
            InitializeComponent();
            InitComposition();
            this.DataContext = this;

            Messenger.Default.Register<GenericMessage<string>>(this, MessengerTokens.REPORT_DOWNLOADED, msg =>
               {
                   var id = msg.Content;
                   if (id == CurrentImage?.Image.ID)
                   {
                       FlipperControl.DisplayIndex = (int)DownloadStatus.Ok;
                   }
               });

            Messenger.Default.Register<GenericMessage<string>>(this, MessengerTokens.REPORT_FAILURE, msg =>
            {
                var id = msg.Content;
                if (id == CurrentImage?.Image.ID)
                {
                    FlipperControl.DisplayIndex = (int)DownloadStatus.Pending;
                }
            });

            Window.Current.CoreWindow.SizeChanged += CoreWindow_SizeChanged;

            if (DeviceUtil.IsXbox)
            {
                SetAsBothBtn.Visibility = Visibility.Collapsed;
                SetAsLockScreenBtn.Visibility = Visibility.Collapsed;
            }
        }

        private async void CoreWindow_SizeChanged(CoreWindow sender, WindowSizeChangedEventArgs args)
        {
            await UpdateDetailContentGridSizeAsync(false);
        }

        private async Task UpdateDetailContentGridSizeAsync(bool awaitForSizeChanged)
        {
            var size = GetTargetSize();
            DetailContentGrid.Width = size.X;
            DetailContentGrid.Height = size.Y;

            DetailContentGrid.Clip = new RectangleGeometry()
            {
                Rect = new Rect(0, 0, size.X, size.Y)
            };

            _detailContentGirdSizeSpecified = true;

            if (awaitForSizeChanged)
            {
                await DetailContentGrid.WaitForSizeChangedAsync();
            }
        }

        private void InitComposition()
        {
            _compositor = this.GetVisual().Compositor;
            _detailGridVisual = DetailGrid.GetVisual();
            _maskBorderGridVisual = MaskBorder.GetVisual();
            _infoGridVisual = InfoGrid.GetVisual();
            _loadingPath = LoadingPath.GetVisual();
            _shareBtnVisual = ShareBtn.GetVisual();
            _flipperVisual = FlipperControl.GetVisual();
            _taskbarImageVisual = TaskBarImage.GetVisual();
            _lockScreenImageVisual = LockImage.GetVisual();
            _previewBtnVisual = PreviewBtn.GetVisual();
            _setAsSPVisual = SetAsSP.GetVisual();
            _exifInfoVisual = ExifInfoGrid.GetVisual();
            _operationSPVisual = OperationSP.GetVisual();
            _detailContentGridVisual = DetailContentGrid.GetVisual();

            ResetVisualInitState();
        }

        private void ResetVisualInitState()
        {
            _infoGridVisual.SetTranslation(new Vector3(0f, -100f, 0));
            _shareBtnVisual.SetTranslation(new Vector3(150f, 0f, 0f));
            _flipperVisual.SetTranslation(new Vector3(170f, 0f, 0f));
            _detailGridVisual.Opacity = 0;
            _taskbarImageVisual.Opacity = 0;
            _lockScreenImageVisual.Opacity = 0;
            _previewBtnVisual.Opacity = 1;
            _setAsSPVisual.Opacity = 0;
            _setAsSPVisual.SetTranslation(new Vector3(0f, 150f, 0f));
            _exifInfoVisual.SetTranslation(new Vector3(0f, 200f, 0f));
            _maskBorderGridVisual.Opacity = 0;

            PhotoSV.ChangeView(null, 0, null);
            StartLoadingAnimation();
        }

        private void MaskBorder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (_showingExif)
            {
                _hideAfterHidingExif = true;
                ToggleExifInfo(false);
            }
            else
            {
                Hide();
            }
        }

        private void TogglePreviewButtonAnimation(bool show)
        {
            _previewBtnVisual.StartBuildAnimation()
                .Animate(AnimateProperties.Opacity)
                .To(show ? 1 : 0)
                .Spend(300)
                .Start();
        }

        private FrameworkElement _listItem;
        private Visual _listItemVisual;

        /// <summary>
        /// Toggle the enter animation by passing a list item. 
        /// This control will take care of the rest part.
        /// </summary>
        /// <param name="listItem"></param>
        public async void Show(FrameworkElement listItem)
        {
            if (_animating) return;

            _animating = true;

            _listItem = listItem;
            _listItemVisual = _listItem.GetVisual();

            _listItemAspectRatio = (float)(_listItem.ActualWidth / _listItem.ActualHeight);

            await UpdateDetailContentGridSizeAsync(false);
            await ToggleListItemAnimationAsync(true);

            ToggleDetailGridAnimation(true);
        }

        public void Hide()
        {
            _animating = true;

            PhotoSV.ChangeView(null, 0, null);

            ToggleSetAsSP(false);

            DismissPreview();
            TogglePreviewButtonAnimation(false);

            ToggleFlipperControlAnimation(false);
            ToggleShareBtnAnimation(false);

            var batch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            ToggleInfoGridAnimation(false);
            batch.Completed += async (s, ex) =>
            {
                await HideInternalAsync();
            };
            batch.End();
        }

        private async Task HideInternalAsync()
        {
            if (_listItem == null) return;

            var innerBatch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            await ToggleListItemAnimationAsync(false);
            innerBatch.Completed += (ss, exx) =>
            {
                if (_listItem != null)
                {
                    _listItem.GetVisual().Opacity = 1f;
                    _listItem = null;
                }

                OnHidden?.Invoke(this, new EventArgs());
                ToggleDetailGridAnimation(false);
                FlipperControl.DisplayIndex = (int)DownloadStatus.Pending;

                _animating = false;
            };
            innerBatch.End();
        }

        private async Task ToggleListItemAnimationAsync(bool show)
        {
            _listItemVisual.Opacity = 0f;

            var targetImageSize = GetTargetImageSize();
            var targetImagePosition = GetTargetPosition();

            this.Visibility = Visibility.Visible;

            if (show && !_detailContentGirdSizeSpecified)
            {
                await UpdateDetailContentGridSizeAsync(true);
            }

            var listItemSize = new Vector2((float)_listItem.ActualWidth, (float)_listItem.ActualHeight);
            var listItemCenterPosition = _listItem.TransformToVisual(Window.Current.Content)
                                            .TransformPoint(new Point(listItemSize.X / 2, listItemSize.Y / 2));
            var targetSize = GetTargetImageSize();

            var offsetXAbs = (float)listItemCenterPosition.X - (targetImagePosition.X + targetImageSize.X / 2);
            var offsetYAbs = (float)listItemCenterPosition.Y - (targetImagePosition.Y + targetImageSize.Y / 2);

            var startX = show ? offsetXAbs : _detailContentGridVisual.GetTranslation().X;
            var startY = show ? offsetYAbs : _detailContentGridVisual.GetTranslation().Y;

            var endX = show ? 0f : offsetXAbs;
            var endY = show ? 0f : offsetYAbs;

            var startScale = show ? listItemSize.X / targetImageSize.X : 1f;
            var endScale = show ? 1f : listItemSize.X / targetImageSize.X;

            _detailGridVisual.Opacity = 1f;
            _detailContentGridVisual.CenterPoint = new Vector3(targetImageSize.X / 2f, targetImageSize.Y / 2f, 1f);

            var scaleAnim = _compositor.CreateVector3KeyFrameAnimation();
            scaleAnim.Duration = TimeSpan.FromMilliseconds(400);
            scaleAnim.InsertKeyFrame(0f, new Vector3(startScale, startScale, 1f));
            scaleAnim.InsertKeyFrame(1f, new Vector3(endScale, endScale, 1f));
            _detailContentGridVisual.StartAnimation("Scale", scaleAnim);

            var offsetAnim = _compositor.CreateVector3KeyFrameAnimation();
            offsetAnim.Duration = TimeSpan.FromMilliseconds(400);
            offsetAnim.InsertKeyFrame(0f, new Vector3(startX, startY, 0f));
            offsetAnim.InsertKeyFrame(1f, new Vector3(endX, endY, 0f));
            _detailContentGridVisual.StartAnimation("Translation", offsetAnim);
        }

        private void ToggleDetailGridAnimation(bool show)
        {
            IsShown = show;

            TogglePreviewButtonAnimation(show);

            var fadeAnimation = _compositor.CreateScalarKeyFrameAnimation();
            fadeAnimation.InsertKeyFrame(1f, show ? 1f : 0f);
            fadeAnimation.Duration = TimeSpan.FromMilliseconds(show ? 700 : 300);
            fadeAnimation.DelayTime = TimeSpan.FromMilliseconds(show ? 400 : 0);

            var batch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            _detailGridVisual.StartAnimation("Opacity", fadeAnimation);

            var maskFadeAnimation = _compositor.CreateScalarKeyFrameAnimation();
            maskFadeAnimation.InsertKeyFrame(1f, show ? 0.8f : 0f);
            maskFadeAnimation.Duration = TimeSpan.FromMilliseconds(show ? 700 : 300);
            maskFadeAnimation.DelayTime = TimeSpan.FromMilliseconds(show ? 400 : 0);

            _maskBorderGridVisual.StartAnimation("Opacity", maskFadeAnimation);

            if (show)
            {
                var task = CheckImageDownloadStatusAsync();
                var task2 = CurrentImage.GetExifInfoAsync();
                ToggleFlipperControlAnimation(true);
                ToggleShareBtnAnimation(true);
                ToggleInfoGridAnimation(true);
            }

            batch.Completed += (sender, e) =>
            {
                if (!show)
                {
                    ResetVisualInitState();
                    this.Visibility = Visibility.Collapsed;

                    ToggleElementsOpacity(true);
                }

                _animating = false;
            };
            batch.End();
        }

        private void ToggleFlipperControlAnimation(bool show)
        {
            var offsetAnimation = _compositor.CreateVector3KeyFrameAnimation();
            offsetAnimation.InsertKeyFrame(1f, new Vector3(show ? 0f : 100f, 0f, 0f));
            offsetAnimation.Duration = TimeSpan.FromMilliseconds(1000);
            offsetAnimation.DelayTime = TimeSpan.FromMilliseconds(show ? 500 : 0);

            _flipperVisual.StartAnimation(_flipperVisual.GetTranslationPropertyName(), offsetAnimation);
        }

        private void ToggleShareBtnAnimation(bool show)
        {
            var offsetAnimation = _compositor.CreateVector3KeyFrameAnimation();
            offsetAnimation.InsertKeyFrame(1f, new Vector3(show ? 0f : 150f, 0f, 0f));
            offsetAnimation.Duration = TimeSpan.FromMilliseconds(show ? 1000 : 400);
            offsetAnimation.DelayTime = TimeSpan.FromMilliseconds(show ? 400 : 0);

            _shareBtnVisual.StartAnimation(_shareBtnVisual.GetTranslationPropertyName(), offsetAnimation);
        }

        private void ToggleInfoGridAnimation(bool show)
        {
            var offsetAnimation = _compositor.CreateVector3KeyFrameAnimation();
            offsetAnimation.InsertKeyFrame(1f, new Vector3(0f, show ? 0f : -100f, 0f));
            offsetAnimation.Duration = TimeSpan.FromMilliseconds(500);
            offsetAnimation.DelayTime = TimeSpan.FromMilliseconds(show ? 500 : 0);

            _infoGridVisual.StartAnimation(_infoGridVisual.GetTranslationPropertyName(), offsetAnimation);
        }

        private async void DetailGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            await UpdateDetailContentGridSizeAsync(false);
        }

        private async Task CheckImageDownloadStatusAsync()
        {
            await CurrentImage.CheckAndGetDownloadedFileAsync();
            this.FlipperControl.DisplayIndex = (int)CurrentImage.DownloadStatus;
        }

        private async void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentImage.DownloadStatus == DownloadStatus.Ok)
            {
                return;
            }

            FlipperControl.DisplayIndex = (int)DownloadStatus.Downloading;

            try
            {
                _cts = new CancellationTokenSource();
                var item = new DownloadItem(CurrentImage);
                item = await DownloadsVM.AddDownloadingImageAsync(item);

                var savedResult = false;
                if (item != null)
                {
                    savedResult = await item.DownloadFullImageAsync(_cts);
                }

                //Still in this page
                if (IsShown && savedResult)
                {
                    CurrentImage.DownloadStatus = DownloadStatus.Ok;
                    FlipperControl.DisplayIndex = (int)DownloadStatus.Ok;
                    ToastService.SendToast("Saved :D", 1000);
                }
            }
            catch (OperationCanceledException)
            {
                FlipperControl.DisplayIndex = (int)DownloadStatus.Pending;
            }
            catch (Exception ex)
            {
                var task = Logger.LogAsync(ex);
                FlipperControl.DisplayIndex = (int)DownloadStatus.Pending;
                ToastService.SendToast($"Exception throws.{ex.Message}", 3000);
            }
        }

        private void StartLoadingAnimation()
        {
            var rotateAnimation = _compositor.CreateScalarKeyFrameAnimation();
            rotateAnimation.InsertKeyFrame(1, 360f, _compositor.CreateLinearEasingFunction());
            rotateAnimation.Duration = TimeSpan.FromMilliseconds(800);
            rotateAnimation.IterationBehavior = AnimationIterationBehavior.Forever;

            _loadingPath.CenterPoint = new Vector3((float)LoadingPath.ActualWidth / 2, (float)LoadingPath.ActualHeight / 2, 0f);

            _loadingPath.RotationAngleInDegrees = 0f;
            _loadingPath.StopAnimation("RotationAngleInDegrees");
            _loadingPath.StartAnimation("RotationAngleInDegrees", rotateAnimation);
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }

        private void DetailGrid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (Math.Abs(e.Cumulative.Translation.Y) > 30)
            {
                MaskBorder_Tapped(null, null);
            }
        }

        private void InfoPlaceHolderGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetInfoPlaceholderGridClip(true);
        }

        private void SetInfoPlaceholderGridClip(bool clip)
        {
            if (!clip)
            {
                InfoPlaceHolderGrid.ClearValue(ClipProperty);
                return;
            }
            InfoPlaceHolderGrid.Clip = new RectangleGeometry()
            {
                Rect = new Rect(0, 0, InfoPlaceHolderGrid.ActualWidth, InfoPlaceHolderGrid.ActualHeight)
            };
        }

        private void ToggleExifInfo(bool show)
        {
            _showingExif = show;
            if (show)
            {
                SetInfoPlaceholderGridClip(false);
                InfoPlaceHolderGrid.Background = CurrentImage.MajorColor;
                _exifInfoVisual.SetTranslation(new Vector3(0f, 100f, 0f));
            }
            else
            {
                InfoPlaceHolderGrid.Background = new SolidColorBrush(Colors.Transparent);
            }

            var showDurationForInfo = 600;
            var hideDurationForInfo = _hideAfterHidingExif ? 200 : 400;

            var showDurationForExif = 400;
            var hideDurationForExif = _hideAfterHidingExif ? 200 : 600;

            AutherNameBtn.BorderThickness = new Thickness(0, 0, 0, show ? 0 : 2);

            _infoGridVisual.StartBuildAnimation().Animate(AnimateProperties.TranslationY)
                .To(show ? -100f : 0f)
                .Spend(show ? showDurationForInfo : hideDurationForInfo)
                .Start()
                .OnCompleted += (s, e) =>
                  {
                      if (!show)
                      {
                          SetInfoPlaceholderGridClip(true);
                          if (_hideAfterHidingExif)
                          {
                              _hideAfterHidingExif = false;
                              Hide();
                          }
                      }
                  };

            _exifInfoVisual.StartBuildAnimation()
                .Animate(AnimateProperties.TranslationY)
                .To(show ? 0f : 100f)
                .Spend(show ? showDurationForExif : hideDurationForExif)
                .Start();

            _operationSPVisual.StartBuildAnimation().Animate(AnimateProperties.TranslationY)
                                        .To(show ? -100f : 0f)
                                        .Spend(show ? showDurationForInfo : hideDurationForInfo)
                                        .Start();

            SetAsGrid.GetVisual().StartBuildAnimation().Animate(AnimateProperties.TranslationY)
                                        .To(show ? -100f : 0f)
                                        .Spend(show ? showDurationForInfo : hideDurationForInfo)
                                        .Start();

            InfoBtn.GetVisual().CenterPoint = new Vector3((float)InfoBtn.ActualWidth / 2f, (float)InfoBtn.ActualHeight / 2f, 0);
            InfoBtn.GetVisual().StartBuildAnimation().Animate(AnimateProperties.RotationAngleInDegrees)
                .To(show ? 180f : 0f)
                .Spend(show ? showDurationForInfo : hideDurationForInfo)
                .Start();
        }

        private void TogglePreview()
        {
            ToggleSetAsSP(false);
            _showingPreview++;
            if (_showingPreview > 2)
            {
                _showingPreview = 0;
            }

            Visual fadingVisual = null;
            Visual showingVisual = null;
            switch (_showingPreview)
            {
                case 0:
                    {
                        fadingVisual = _lockScreenImageVisual;
                        showingVisual = null;
                    }
                    break;
                case 1:
                    {
                        fadingVisual = null;
                        showingVisual = _taskbarImageVisual;
                    }
                    break;
                case 2:
                    {
                        fadingVisual = _taskbarImageVisual;
                        showingVisual = _lockScreenImageVisual;
                    }
                    break;
                default:
                    break;
            }
            if (fadingVisual != null)
            {
                fadingVisual.StartBuildAnimation()
                    .Animate(AnimateProperties.Opacity)
                    .To(0)
                    .Spend(300)
                    .Start()
                    .OnCompleted += (sender, e) =>
                      {
                          if (_showingPreview == 2)
                          {
                              TaskBarImage.Visibility = Visibility.Collapsed;
                          }
                          else if (_showingPreview == 0)
                          {
                              LockImage.Visibility = Visibility.Collapsed;
                          }
                      };
            }
            if (showingVisual != null)
            {
                if (_showingPreview == 1)
                {
                    TaskBarImage.Visibility = Visibility.Visible;
                }
                else if (_showingPreview == 2)
                {
                    LockImage.Visibility = Visibility.Visible;
                }
                showingVisual.StartBuildAnimation()
                    .Animate(AnimateProperties.Opacity)
                    .To(1)
                    .Spend(300)
                    .Start();
            }
        }

        private void ToggleSetAsSP(bool show)
        {
            if (FlipperControl.DisplayIndex != 3 && FlipperControl.DisplayIndex != 2)
            {
                return;
            }
            if (show)
            {
                DismissPreview();
            }
            if (!show)
            {
                FlipperControl.DisplayIndex = 2;
            }
            SetAsSP.Visibility = Visibility.Visible;
            _setAsSPVisual.StartBuildAnimation()
                  .Animate(AnimateProperties.TranslationY)
                  .To(show ? 0 : 150f)
                  .Spend(500)
                  .Start()
                  .OnCompleted += (sender, e) =>
                  {
                      if (!show)
                      {
                          SetAsSP.Visibility = Visibility.Collapsed;
                      }
                  };
            _setAsSPVisual.StartBuildAnimation()
                  .Animate(AnimateProperties.Opacity)
                  .To(show ? 1 : 0)
                  .Spend(300)
                  .Start();
        }

        private void DismissPreview()
        {
            _showingPreview = 0;
            _taskbarImageVisual.StartBuildAnimation()
                   .Animate(AnimateProperties.Opacity)
                   .To(0)
                   .Spend(300)
                   .Start()
                   .OnCompleted += (sender, e) =>
                   {
                       TaskBarImage.Visibility = Visibility.Collapsed;
                   };
            _lockScreenImageVisual.StartBuildAnimation()
                  .Animate(AnimateProperties.Opacity)
                  .To(0)
                  .Spend(300)
                  .Start()
                  .OnCompleted += (sender, e) =>
                  {
                      LockImage.Visibility = Visibility.Collapsed;
                  };
        }

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_setAsSPVisual.Opacity == 0)
            {
                FlipperControl.DisplayIndex = 3;
                ToggleSetAsSP(true);
            }
            else
            {
                ToggleSetAsSP(false);
            }
        }

        private async void SetAsBackgroundBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentImage.DownloadedFile != null)
            {
                await WallpaperSettingHelper.SetAsBackgroundAsync(CurrentImage.DownloadedFile);
            }
        }

        private async void SetAsLockscreenBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentImage.DownloadedFile != null)
            {
                await WallpaperSettingHelper.SetAsLockscreenAsync(CurrentImage.DownloadedFile);
            }
        }

        private async void SetAsBothBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentImage.DownloadedFile != null)
            {
                await WallpaperSettingHelper.SetBothAsync(CurrentImage.DownloadedFile);
            }
        }

        private void PreviewBtn_Click(object sender, RoutedEventArgs e)
        {
            TogglePreview();
        }

        private void InfoBtn_Click(object sender, RoutedEventArgs e)
        {
            _showingExif = !_showingExif;
            ToggleExifInfo(_showingExif);
        }

        private void SetAsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetAsGrid.Clip = new RectangleGeometry()
            {
                Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height)
            };
        }

        private void ShareBtn_Click(object sender, RoutedEventArgs e)
        {
            ToggleSetAsSP(false);
        }

        private void AutherNameBtn_Click(object sender, RoutedEventArgs e)
        {
            ToggleSetAsSP(false);
        }

        private Vector2 GetTargetSize()
        {
            var windowWidth = Window.Current.Bounds.Width;
            var windowHeight = Window.Current.Bounds.Height;

            var height = Math.Min(windowWidth * (1 / _listItemAspectRatio), windowHeight - 200);
            var width = _listItemAspectRatio * height;

            var maxWidth = ResourcesHelper.GetDimentionInPixel("MaxWidthOfDetails");
            if (width >= maxWidth)
            {
                width = maxWidth;
                height = width / _listItemAspectRatio;
            }

            return new Vector2((float)width, (float)height + 100f);
        }

        public Vector2 GetTargetImageSize()
        {
            var windowWidth = Window.Current.Bounds.Width;
            var windowHeight = Window.Current.Bounds.Height;

            var height = Math.Min(windowWidth * (1 / _listItemAspectRatio), windowHeight - 200);
            var width = _listItemAspectRatio * height;

            var maxWidth = ResourcesHelper.GetDimentionInPixel("MaxWidthOfDetails");
            if (width >= maxWidth)
            {
                width = maxWidth;
                height = width / _listItemAspectRatio;
            }

            return new Vector2((float)width, (float)height);
        }

        public Vector2 GetTargetPosition()
        {
            var size = GetTargetSize();
            var x = (Window.Current.Bounds.Width - size.X) / 2;
            var y = (Window.Current.Bounds.Height - size.Y) / 2;
            return new Vector2((float)x, (float)y);
        }

        private void ToggleElementsOpacity(bool show)
        {
            InfoPlaceHolderGrid.GetVisual().Opacity = show ? 1f : 0f;
            OperationSP.GetVisual().Opacity = show ? 1f : 0f;
            PreviewBtn.GetVisual().Opacity = show ? 1f : 0f;
            SetAsGrid.GetVisual().Opacity = show ? 1f : 0f;
        }

        private void InfoPlaceHolderGrid_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            ToggleElementsOpacity(false);
            e.Handled = true;
        }

        private void InfoPlaceHolderGrid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            _detailContentGridVisual.SetTranslation(
                new Vector3((float)e.Cumulative.Translation.X, (float)e.Cumulative.Translation.Y, 1f));
        }

        private async void InfoPlaceHolderGrid_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            ToggleExifInfo(false);
            PhotoSV.ChangeView(null, 0, null);
            await HideInternalAsync();
        }
    }
}