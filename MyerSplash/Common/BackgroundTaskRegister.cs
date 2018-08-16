using BackgroundTask;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace MyerSplash.Common
{
    public static class BackgroundTaskRegister
    {
        private static string NAME => "WallpaperAutoChangeTask";
        private static uint PERIOD_MINS => 240; // 4 hours

        public static async Task RegisterAsync()
        {
            await RegisterBackgroundTask(typeof(WallpaperAutoChangeTask),
                                                    new TimeTrigger(PERIOD_MINS, false),
                                                    null);
        }

        public static async Task UnregisterAsync()
        {
            var status = await BackgroundExecutionManager.RequestAccessAsync();
            if (status == BackgroundAccessStatus.Unspecified || status == BackgroundAccessStatus.Denied)
            {
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

        public static async Task<BackgroundTaskRegistration> RegisterBackgroundTask(Type taskEntryPoint,
                                                                IBackgroundTrigger trigger,
                                                                IBackgroundCondition condition)
        {
            var status = await BackgroundExecutionManager.RequestAccessAsync();
            if (status == BackgroundAccessStatus.Unspecified || status == BackgroundAccessStatus.Denied)
            {
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
    }
}