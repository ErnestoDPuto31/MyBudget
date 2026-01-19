using Android.App;
using Android.OS;
using Android.Widget;
using Android.Graphics;
using System.Collections.Generic;
using System;

namespace AndroidApp1
{
    [Activity(Label = "MyBudget", MainLauncher = false)]
    public class MainActivity : Activity
    {
        double weeklyBudget = 0;
        double remainingBalance = 0;
        bool lowBalanceNotified = false;

        List<Expense> expenseListData = new List<Expense>();
        ExpenseAdapter adapter;
        DatabaseHelper db;

        const string CHANNEL_ID = "budget_channel";

        // Global UI references for updating
        TextView balanceText;
        ProgressBar progressBar;
        TextView spentTodayText;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            RequestWindowFeature(Android.Views.WindowFeatures.NoTitle);
            SetContentView(Resource.Layout.activity_main);
            spentTodayText = FindViewById<TextView>(Resource.Id.spentTodayText);

            // Handle Notification Permissions for Android 13+
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                if (CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Android.Content.PM.Permission.Granted)
                {
                    RequestPermissions(new string[] { Android.Manifest.Permission.PostNotifications }, 1001);
                }
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                // Sets the status bar to match your dark green primary color
                Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#1B5E20"));
            }

            CreateNotificationChannel();
            db = new DatabaseHelper(this);

            // CONNECT UI
            balanceText = FindViewById<TextView>(Resource.Id.balanceText);
            progressBar = FindViewById<ProgressBar>(Resource.Id.budgetProgress);
            ListView expenseListView = FindViewById<ListView>(Resource.Id.expenseListView);
            EditText budgetInput = FindViewById<EditText>(Resource.Id.budgetInput);
            Button setBudgetBtn = FindViewById<Button>(Resource.Id.setBudgetBtn);

            // CATEGORY BUTTONS - Finding them by ID
            FindViewById<Button>(Resource.Id.btnHealth).Click += (s, e) => ShowAmountDialog("HEALTH");
            FindViewById<Button>(Resource.Id.btnPets).Click += (s, e) => ShowAmountDialog("PETS");
            FindViewById<Button>(Resource.Id.btnFood).Click += (s, e) => ShowAmountDialog("FOOD");
            FindViewById<Button>(Resource.Id.btnTransport).Click += (s, e) => ShowAmountDialog("TRANSPORT");
            FindViewById<Button>(Resource.Id.btnEntertainment).Click += (s, e) => ShowAmountDialog("ENTERTAINMENT");
            FindViewById<Button>(Resource.Id.btnOthers).Click += (s, e) => ShowAmountDialog("OTHERS");

            // Load saved data
            weeklyBudget = db.GetBudget();
            RefreshData();

            adapter = new ExpenseAdapter(this, expenseListData);
            expenseListView.Adapter = adapter;
            UpdateBalance();

            expenseListView.ItemLongClick += (s, e) => {
                var item = expenseListData[e.Position];

                new AlertDialog.Builder(this)
                    .SetTitle("Delete Expense?")
                    .SetMessage($"Remove {item.Category} - ₱{item.Amount}?")
                    .SetPositiveButton("Delete", (sender, args) => {
                        db.DeleteExpense(item.Id); // Deletes from SQLite
                        RefreshData();             // Recalculates remainingBalance
                        UpdateBalance();           // Updates UI colors/progress
                        adapter.NotifyDataSetChanged(); // Updates the List visually
                        Toast.MakeText(this, "Expense Deleted", ToastLength.Short).Show();
                    })
                    .SetNegativeButton("Cancel", (sender, args) => { })
                    .Show();
            };

            setBudgetBtn.Click += (s, e) =>
            {
                if (double.TryParse(budgetInput.Text, out weeklyBudget))
                {
                    db.SaveBudget(weeklyBudget);
                    RefreshData();
                    UpdateBalance();
                    Toast.MakeText(this, "Budget Updated", ToastLength.Short).Show();
                }
            };

            Button resetBtn = FindViewById<Button>(Resource.Id.resetWeekBtn);
            resetBtn.Click += (s, e) => {
                new AlertDialog.Builder(this)
                    .SetTitle("End of Week")
                    .SetMessage("Would you like to save a receipt before clearing all data?")
                    .SetPositiveButton("Save & Reset", (sender, args) => {
                        string file = ExportReceipt();
                        if (file != null)
                        {
                            Toast.MakeText(this, $"Receipt Saved: {file}", ToastLength.Long).Show();
                            PerformReset();
                        }
                        else
                        {
                            Toast.MakeText(this, "Nothing to save!", ToastLength.Short).Show();
                        }
                    })
                    .SetNeutralButton("Reset Anyway", (sender, args) => {
                        PerformReset();
                    })
                    .SetNegativeButton("Cancel", (sender, args) => { })
                    .Show();
            };

            void PerformReset()
            {
                var all = db.GetAllExpenses();
                foreach (var item in all) db.DeleteExpense(item.Id);

                lowBalanceNotified = false;
                RefreshData();
                UpdateBalance();
                adapter.NotifyDataSetChanged();
                Toast.MakeText(this, "Weekly data cleared", ToastLength.Short).Show();
            }

            RefreshData();
            UpdateBalance();
        }

        private void ShowAmountDialog(string category)
        {
            var amounts = new string[] { "10.00 PHP", "15.00 PHP", "20.00 PHP", "25.00 PHP", "50.00 PHP", "200.00 PHP", "500.00", "Custom Amount" };

            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetTitle($"Select Amount for {category}");
            builder.SetItems(amounts, (sender, args) =>
            {
                string selected = amounts[args.Which];

                if (selected == "Custom Amount")
                {
                    ShowCustomAmountDialog(category);
                }
                else
                {
                    double val = double.Parse(selected.Split(' ')[0]);
                    // NEW: Instead of saving immediately, ask for the Name
                    ShowNameDialog(category, val);
                }
            });
            builder.Show();
        }

        private void ShowCustomAmountDialog(string category)
        {
            EditText input = new EditText(this)
            {
                InputType = Android.Text.InputTypes.ClassNumber | Android.Text.InputTypes.NumberFlagDecimal,
                Hint = "0.00"
            };

            new AlertDialog.Builder(this)
                .SetTitle("Enter Custom Amount")
                .SetView(input)
                .SetPositiveButton("Next", (s, e) => {
                    if (double.TryParse(input.Text, out double val))
                        ShowNameDialog(category, val); 
                })
                .SetNegativeButton("Cancel", (s, e) => { })
                .Show();
        }
        private void ShowNameDialog(string category, double amount)
        {
            EditText input = new EditText(this)
            {
                Hint = "Cost Name",
                InputType = Android.Text.InputTypes.TextFlagCapWords
            };
            input.SetPadding(50, 40, 50, 40);

            new AlertDialog.Builder(this)
                .SetTitle("Item Name")
                .SetMessage($"What did you buy under {category}?")
                .SetView(input)
                .SetPositiveButton("Save", (s, e) => {
                    string title = string.IsNullOrWhiteSpace(input.Text) ? category : input.Text;
                    SaveExpense(title, category, amount); // Pass title to save
                })
                .SetNegativeButton("Back", (s, e) => ShowAmountDialog(category))
                .Show();
        }

        private void SaveExpense(string title, string category, double amount)
        {
            Expense newExp = new Expense
            {
                Description = title, // Mapping the name to Description
                Amount = amount,
                Category = category,
                Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)
            };

            db.AddExpense(newExp);
            RefreshData();
            UpdateBalance();
            adapter.NotifyDataSetChanged();
            Toast.MakeText(this, $"Saved {title} (₱{amount})", ToastLength.Short).Show();
        }

        void RefreshData()
        {
            expenseListData.Clear();
            var allExpenses = db.GetAllExpenses() ?? new List<Expense>();
            expenseListData.AddRange(allExpenses);

            double totalToday = 0;
            remainingBalance = weeklyBudget;

            // Get today's date at midnight (00:00:00) for a clean comparison
            DateTime today = DateTime.Today;

            foreach (var exp in allExpenses)
            {
                remainingBalance -= exp.Amount;

                if (DateTime.TryParse(exp.Date, out DateTime expenseDate))
                {
                    if (expenseDate.Date == today)
                    {
                        totalToday += exp.Amount;
                    }
                }
                else
                {
                    string todayStr = today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    if (exp.Date != null && exp.Date.Trim().StartsWith(todayStr))
                    {
                        totalToday += exp.Amount;
                    }
                }
            }

            // Update UI
            if (spentTodayText != null)
            {
                spentTodayText.Text = $"₱{totalToday:F2}";
            }
        }

        void UpdateBalance()
        {
            balanceText.Text = $"Remaining: ₱{remainingBalance:F2}";
            if (weeklyBudget > 0)
            {
                int progress = (int)((remainingBalance / weeklyBudget) * 100);
                progressBar.Progress = Math.Max(0, progress);

                if (progress >= 50) balanceText.SetTextColor(Color.ParseColor("#4CAF50"));
                else if (progress >= 20) balanceText.SetTextColor(Color.ParseColor("#FFC107"));
                else
                {
                    balanceText.SetTextColor(Color.ParseColor("#F44336"));
                    if (!lowBalanceNotified)
                    {
                        lowBalanceNotified = true;
                        ShowLowBudgetNotification(remainingBalance);
                    }
                }
            }
        }

        private void CreateNotificationChannel()
        {
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                var channelName = "Budget Alerts";
                var channelDescription = "Notifications for low budget alerts";
                var importance = NotificationImportance.High;

                var channel = new NotificationChannel(CHANNEL_ID, channelName, importance)
                {
                    Description = channelDescription
                };

                var notificationManager = (NotificationManager)GetSystemService(NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }
        }
        private void ShowLowBudgetNotification(double remaining)
        {
            Android.Content.Intent intent = new Android.Content.Intent(this, typeof(MainActivity));
            intent.SetFlags(Android.Content.ActivityFlags.ClearTop | Android.Content.ActivityFlags.SingleTop);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var builder = new Notification.Builder(this, CHANNEL_ID)
                .SetContentTitle("Low Budget Alert!")
                .SetContentText($"You only have ₱{remaining:F2} left for this week.")
                .SetLargeIcon(BitmapFactory.DecodeResource(Resources, Resource.Mipmap.appicon))
                .SetSmallIcon(Resource.Mipmap.notificationicon)
                .SetContentIntent(pendingIntent)
                .SetAutoCancel(true);

            var notificationManager = NotificationManager.FromContext(this);
            notificationManager.Notify(1, builder.Build());
        }
        private string ExportReceipt()
        {
            try
            {
                var allExpenses = db.GetAllExpenses();
                if (allExpenses.Count == 0) return null;

                System.Text.StringBuilder receipt = new System.Text.StringBuilder();
                receipt.AppendLine("============================");
                receipt.AppendLine("      MYBUDGET RECEIPT      ");
                receipt.AppendLine($"   Date: {DateTime.Now:MMMM dd, yyyy}");
                receipt.AppendLine("============================");
                receipt.AppendLine(string.Format("{0,-15} {1,10}", "ITEM", "AMOUNT"));
                receipt.AppendLine("----------------------------");

                double total = 0;
                foreach (var exp in allExpenses)
                {
                    string desc = exp.Description.Length > 15 ? exp.Description.Substring(0, 12) + "..." : exp.Description;
                    receipt.AppendLine(string.Format("{0,-15} ₱{1,10:F2}", desc, exp.Amount));
                    total += exp.Amount;
                }

                receipt.AppendLine("----------------------------");
                receipt.AppendLine(string.Format("{0,-15} ₱{1,10:F2}", "TOTAL SPENT", total));
                receipt.AppendLine("============================");
                receipt.AppendLine("   End of Weekly Report   ");

                // Save to Downloads folder
                string filename = $"Receipt_{DateTime.Now:yyyyMMdd_HHmm}.txt";
                string path = System.IO.Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath, filename);

                System.IO.File.WriteAllText(path, receipt.ToString());
                return filename;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Export error: " + ex.Message);
                return null;
            }
        }
    }
}