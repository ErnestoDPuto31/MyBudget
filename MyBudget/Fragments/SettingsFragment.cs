using Android.App;
using Android.Content;
using Android.Views;
using AndroidX.Work;
using Google.Android.Material.Button;
using Google.Android.Material.SwitchMaterial;
using Google.Android.Material.TextField;
using MyBudget.Models;
using MyBudget.Services;
using System.Text;

namespace MyBudget.Fragments
{
    public class SettingsFragment : AndroidX.Fragment.App.Fragment
    {
        private RadioGroup rgBudgetPeriod;
        private RadioButton rbWeekly, rbMonthly;
        private TextInputEditText etBudgetAmount;
        private TextView tvPreviewAmount;
        private MaterialButton btnSaveSettings, btnExportData;

        // Notification UI
        private SwitchMaterial switchDailyReminder;
        private LinearLayout layoutReminderTime;
        private TextView tvReminderTime;

        private DatabaseService _dbService;
        private BudgetSettings _currentSettings;
        private ISharedPreferences _prefs;
        private WorkManager _workManager;

        private const string ReminderEnabledKey = "ReminderEnabled";
        private const string ReminderHourKey = "ReminderHour";
        private const string ReminderMinuteKey = "ReminderMinute";

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.fragment_settings, container, false);
            _dbService = new DatabaseService();
            _prefs = Application.Context.GetSharedPreferences("MyBudgetPrefs", FileCreationMode.Private);
            _workManager = WorkManager.GetInstance(RequireContext());

            rgBudgetPeriod = view.FindViewById<RadioGroup>(Resource.Id.rgBudgetPeriod);
            rbWeekly = view.FindViewById<RadioButton>(Resource.Id.rbWeekly);
            rbMonthly = view.FindViewById<RadioButton>(Resource.Id.rbMonthly);
            etBudgetAmount = view.FindViewById<TextInputEditText>(Resource.Id.etBudgetAmount);
            tvPreviewAmount = view.FindViewById<TextView>(Resource.Id.tvPreviewAmount);
            btnSaveSettings = view.FindViewById<MaterialButton>(Resource.Id.btnSaveSettings);
            btnExportData = view.FindViewById<MaterialButton>(Resource.Id.btnExportData);

            switchDailyReminder = view.FindViewById<SwitchMaterial>(Resource.Id.switchDailyReminder);
            layoutReminderTime = view.FindViewById<LinearLayout>(Resource.Id.layoutReminderTime);
            tvReminderTime = view.FindViewById<TextView>(Resource.Id.tvReminderTime);

            etBudgetAmount.TextChanged += (s, e) => UpdateLivePreview();
            rgBudgetPeriod.CheckedChange += (s, e) => UpdateLivePreview();
            btnSaveSettings.Click += async (s, e) => await SaveSettingsAsync();
            btnExportData.Click += async (s, e) => await ExportDataAsync();

            switchDailyReminder.CheckedChange += OnReminderSwitchChanged;
            layoutReminderTime.Click += OnSetTimeClicked;

            LoadNotificationSettings();
            return view;
        }

        public override async void OnResume()
        {
            base.OnResume();
            await LoadCurrentSettingsAsync();
        }

        private async Task LoadCurrentSettingsAsync()
        {
            _currentSettings = await _dbService.GetBudgetSettingsAsync();
            RequireActivity().RunOnUiThread(() =>
            {
                if (_currentSettings.Period == BudgetPeriod.Weekly) rbWeekly.Checked = true;
                else rbMonthly.Checked = true;

                etBudgetAmount.Text = _currentSettings.Amount.ToString("0.00");
                UpdateLivePreview();
            });
        }

        private void UpdateLivePreview()
        {
            if (decimal.TryParse(etBudgetAmount.Text, out decimal currentAmount) && currentAmount > 0)
            {
                int days = rbWeekly.Checked ? 7 : 30;
                tvPreviewAmount.Text = $"₱{(currentAmount / days):N2}";
            }
            else tvPreviewAmount.Text = "₱0.00";
        }

        private async Task SaveSettingsAsync()
        {
            if (decimal.TryParse(etBudgetAmount.Text, out decimal newAmount) && newAmount > 0)
            {
                _currentSettings.Amount = newAmount;
                _currentSettings.Period = rbWeekly.Checked ? BudgetPeriod.Weekly : BudgetPeriod.Monthly;
                await _dbService.UpdateBudgetSettingsAsync(_currentSettings);
                Toast.MakeText(RequireActivity(), "Settings saved!", ToastLength.Short).Show();
            }
        }

        private async Task ExportDataAsync()
        {
            try
            {
                var allExpenses = await _dbService.GetExpensesAsync();
                if (allExpenses == null || allExpenses.Count == 0) return;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("=== MYBUDGET EXPENSES ===");
                foreach (var expense in allExpenses.OrderByDescending(e => e.Date))
                {
                    sb.AppendLine($"{expense.Date:yyyy-MM-dd} | {expense.Category} | ₱{expense.Amount:0.00}");
                }

                string downloadsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
                string filePath = Path.Combine(downloadsPath, $"MyBudget_{DateTime.Now:yyyyMMdd}.txt");
                File.WriteAllText(filePath, sb.ToString());

                Toast.MakeText(RequireActivity(), "Exported to Downloads!", ToastLength.Long).Show();
            }
            catch (Exception ex)
            {
                Toast.MakeText(RequireActivity(), $"Export failed: {ex.Message}", ToastLength.Long).Show();
            }
        }

        private void LoadNotificationSettings()
        {
            switchDailyReminder.Checked = _prefs.GetBoolean(ReminderEnabledKey, false);
            int hour = _prefs.GetInt(ReminderHourKey, 20); // Default 8 PM
            int min = _prefs.GetInt(ReminderMinuteKey, 0);

            DateTime dt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, min, 0);
            tvReminderTime.Text = dt.ToString("h:mm tt");
            layoutReminderTime.Visibility = switchDailyReminder.Checked ? ViewStates.Visible : ViewStates.Gone;
        }

        private void OnReminderSwitchChanged(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            layoutReminderTime.Visibility = e.IsChecked ? ViewStates.Visible : ViewStates.Gone;
            if (e.IsChecked) ScheduleReminder();
            else _workManager.CancelUniqueWork("DailyReminder");

            _prefs.Edit().PutBoolean(ReminderEnabledKey, e.IsChecked).Apply();
        }

        private void OnSetTimeClicked(object sender, EventArgs e)
        {
            int currentHour = _prefs.GetInt(ReminderHourKey, 20);
            int currentMin = _prefs.GetInt(ReminderMinuteKey, 0);

            var picker = new TimePickerDialog(RequireContext(), (s, args) =>
            {
                _prefs.Edit().PutInt(ReminderHourKey, args.HourOfDay).PutInt(ReminderMinuteKey, args.Minute).Apply();
                LoadNotificationSettings(); 
                if (switchDailyReminder.Checked) ScheduleReminder(); 
            }, currentHour, currentMin, false);

            picker.Show();
        }

        private void ScheduleReminder()
        {
            int hour = _prefs.GetInt(ReminderHourKey, 20);
            int min = _prefs.GetInt(ReminderMinuteKey, 0);

            DateTime now = DateTime.Now;
            DateTime target = new DateTime(now.Year, now.Month, now.Day, hour, min, 0);
            if (now > target) target = target.AddDays(1); 

            TimeSpan delay = target - now;

            var workRequest = (PeriodicWorkRequest)new PeriodicWorkRequest.Builder(
                    typeof(ReminderWorker),
                    1,
                    Java.Util.Concurrent.TimeUnit.Days)
                .SetInitialDelay((long)delay.TotalMilliseconds, Java.Util.Concurrent.TimeUnit.Milliseconds)
                .AddTag("DailyReminder")
                .Build();

            _workManager.EnqueueUniquePeriodicWork(
                "DailyReminder",
                ExistingPeriodicWorkPolicy.Replace,
                workRequest
            );
        }
    }
}