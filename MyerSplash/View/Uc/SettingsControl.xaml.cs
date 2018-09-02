using MyerSplash.Common;
using MyerSplash.Common.Composition;
using MyerSplash.ViewModel;
using MyerSplashShared.Utils;
using Windows.ApplicationModel;
using Windows.UI.Composition;
using Windows.UI.Xaml;

namespace MyerSplash.View.Uc
{
    public sealed partial class SettingsControl : NavigableUserControl
    {
        private SettingsViewModel SettingsVM { get; set; }

        private Visual _compactModeOffHintImage;
        private Visual _compactModeOnHintImage;
        private Compositor _compositor;

        public SettingsControl()
        {
            this.InitializeComponent();
            if (!DesignMode.DesignModeEnabled)
            {
                this.DataContext = SettingsVM = new SettingsViewModel();
            }

            if (DeviceUtil.IsXbox)
            {
                AutoChangeSP.Visibility = Visibility.Collapsed;
                CompatModeGrid.Visibility = Visibility.Collapsed;
            }

            _compactModeOffHintImage = CompactModeOffHintImage.GetVisual();
            _compactModeOnHintImage = CompactModeOnHintImage.GetVisual();
            _compositor = _compactModeOffHintImage.Compositor;

            _compactModeOffHintImage.ImplicitAnimations = ImplicitAnimationFactory.CreateCommonOpacityAnimationCollection(_compositor);
            _compactModeOnHintImage.ImplicitAnimations = ImplicitAnimationFactory.CreateCommonOpacityAnimationCollection(_compositor);
        }

        public override async void OnPresented()
        {
            base.OnPresented();
            Window.Current.SetTitleBar(DummyTitleBar);
            UpdateHintImageVisualState();
            await SettingsVM.CalculateCacheAsync();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Presented = false;
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateHintImageVisualState();
        }

        private void UpdateHintImageVisualState()
        {
            if (_compactModeOnHintImage == null || _compactModeOffHintImage == null) return;
            _compactModeOnHintImage.Opacity = CompactModeSwitch.IsOn ? 1f : 0.2f;
            _compactModeOffHintImage.Opacity = CompactModeSwitch.IsOn ? 0.2f : 1f;
        }

        private void CompactModeOffHintImage_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            CompactModeSwitch.IsOn = false;
        }

        private void CompactModeOnHintImage_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            CompactModeSwitch.IsOn = true;
        }
    }
}