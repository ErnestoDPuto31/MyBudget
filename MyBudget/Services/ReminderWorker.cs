using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Work;

namespace MyBudget.Services
{
    public class ReminderWorker : Worker
    {
        public const string ChannelId = "daily_reminder_channel";
        private const int NotificationId = 1001;

        public ReminderWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
        }

        public override ListenableWorker.Result DoWork()
        {
            try
            {
                CreateNotificationChannel();
                ShowNotification();

                return ListenableWorker.Result.InvokeSuccess();
            }
            catch (Exception)
            {
                return ListenableWorker.Result.InvokeFailure();
            }
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(ChannelId, "MyBudget Reminders", NotificationImportance.Default)
                {
                    Description = "Daily reminder to log your expenses."
                };

                var notificationManager = (NotificationManager)Application.Context.GetSystemService(Context.NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }
        }

        private void ShowNotification()
        {
            var intent = new Intent(Application.Context, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
            var pendingIntent = PendingIntent.GetActivity(
                Application.Context,
                0,
                intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var builder = new NotificationCompat.Builder(Application.Context, ChannelId)
                .SetSmallIcon(Android.Resource.Drawable.IcMenuAgenda)
                .SetContentTitle("Time to update MyBudget!")
                .SetContentText("Log your expenses for today to stay on track.")
                .SetPriority(NotificationCompat.PriorityDefault)
                .SetContentIntent(pendingIntent)
                .SetAutoCancel(true);

            var notificationManager = (NotificationManager)Application.Context.GetSystemService(Context.NotificationService);
            notificationManager.Notify(NotificationId, builder.Build());
        }
    }
}