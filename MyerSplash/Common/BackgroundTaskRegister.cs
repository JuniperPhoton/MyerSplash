using BackgroundTask;
using MyerSplashCustomControl;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace MyerSplash.Common
{
    public static class BackgroundTaskRegister
    {
        private static string NAME => "WallpaperAutoChangeTask";
        private static uint PERIOD_HOUR_MINS => 60;

        public static async Task RegisterAsync()
        {
            if (IsBackgroundTaskRegistered())
            {
                Debug.WriteLine("IsBackgroundTaskRegistered: true");
                return;
            }
            if(AppSettings.Instance.BackgroundWallpaperSource == 0)
            {
                return;
            }
            uint period;
            switch (AppSettings.Instance.BackgroundCheckingInterval)
            {
                case 0:
                    period = PERIOD_HOUR_MINS;
                    break;
                case 1:
                    period = PERIOD_HOUR_MINS * 2;
                    break;
                case 2:
                    period = PERIOD_HOUR_MINS * 4;
                    break;
                default:
                    period = PERIOD_HOUR_MINS * 8;
                    break;
            };
            await RegisterBackgroundTask(typeof(WallpaperAutoChangeTask),
                                                    new TimeTrigger(period, false),
                                                    null);
        }

        public static async Task UnregisterAsync()
        {
            var status = await BackgroundExecutionManager.RequestAccessAsync();
            if (status != BackgroundAccessStatus.AlwaysAllowed
                && status != BackgroundAccessStatus.AllowedSubjectToSystemPolicy)
            {
                ToastService.SendToast(ResourcesHelper.GetResString("BackgroundRegisterFailed"), TimeSpan.FromMilliseconds(5000));
                return;
            }

            foreach (var cur in BackgroundTaskRegistration.AllTasks)
            {
                if (cur.Value.Name == NAME)
                {
                    cur.Value.Unregister(true);
                }
            }

            Debug.WriteLine($"===================unregistered===================");
        }

        private static async Task<BackgroundTaskRegistration> RegisterBackgroundTask(Type taskEntryPoint,
                                                                IBackgroundTrigger trigger,
                                                                IBackgroundCondition condition)
        {
            var status = await BackgroundExecutionManager.RequestAccessAsync();
            if (status != BackgroundAccessStatus.AlwaysAllowed
                && status != BackgroundAccessStatus.AllowedSubjectToSystemPolicy)
            {
                ToastService.SendToast(ResourcesHelper.GetResString("BackgroundRegisterFailed"), TimeSpan.FromMilliseconds(5000));
                return null;
            }

            foreach (var cur in BackgroundTaskRegistration.AllTasks)
            {
                if (cur.Value.Name == NAME)
                {
                    cur.Value.Unregister(true);
                }
            }

            var builder = new BackgroundTaskBuilder
            {
                Name = NAME,
                TaskEntryPoint = taskEntryPoint.FullName
            };

            builder.SetTrigger(trigger);

            if (condition != null)
            {
                builder.AddCondition(condition);
            }

            BackgroundTaskRegistration task = builder.Register();

            Debug.WriteLine($"===================Task {NAME} registered successfully===================");

            return task;
        }

        private static bool IsBackgroundTaskRegistered()
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == NAME)
                {
                    return true;
                }
            }

            return false;
        }
    }
}