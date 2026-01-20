using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;

namespace AndroidApp1
{
    [Activity(Label = "MyBudget", MainLauncher = false)]
    public class MainActivity : Activity
    {
        double weeklyBudget = 0;
        double monthlyBudget = 0;
        double remainingWeekly = 0;
        double remainingMonthly = 0;
        bool lowBalanceNotified = false;

        List<Expense> expenseListData = new List<Expense>();
        ExpenseAdapter adapter;
        DatabaseHelper db;

        const string CHANNEL_ID = "budget_channel";

        // UI references
        TextView? monthlyBalanceText;
        TextView? weeklyBalanceText;
        ProgressBar? progressBar;
        TextView? spentTodayText;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            RequestWindowFeature(Android.Views.WindowFeatures.NoTitle);
            SetContentView(Resource.Layout.activity_main);
            new Handler(Looper.MainLooper).PostDelayed(() => {
                RefreshData();
                UpdateBalance();
            }, 100);

            // 1. CONNECT UI
            spentTodayText = FindViewById<TextView>(Resource.Id.spentTodayText);
            monthlyBalanceText = FindViewById<TextView>(Resource.Id.monthlyBalanceText);
            weeklyBalanceText = FindViewById<TextView>(Resource.Id.weeklyBalanceText);
            progressBar = FindViewById<ProgressBar>(Resource.Id.budgetProgress);
            ListView? expenseListView = FindViewById<ListView>(Resource.Id.expenseListView);
            EditText? budgetInput = FindViewById<EditText>(Resource.Id.budgetInput);
            Button? setBudgetBtn = FindViewById<Button>(Resource.Id.setBudgetBtn);

            // 2. STATUS BAR COLOR
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                Window.SetStatusBarColor(Color.ParseColor("#1B5E20"));

            CreateNotificationChannel();
            db = new DatabaseHelper(this);

            // 3. ESSENTIALS BUTTONS
            Button? btnMonthly = FindViewById<Button>(Resource.Id.btnEssentialMonthly);
            btnMonthly.Click += (s, e) => {
                ShowEssentialDialog("MONTHLY");
            };

            Button? btnWeekly = FindViewById<Button>(Resource.Id.btnEssentialWeekly);
            btnWeekly.Click += (s, e) => {
                ShowEssentialDialog("WEEKLY");
            };

            // 4. CATEGORY BUTTONS
            FindViewById<Button>(Resource.Id.btnHealth).Click += (s, e) => ShowAmountDialog("HEALTH");
            FindViewById<Button>(Resource.Id.btnPets).Click += (s, e) => ShowAmountDialog("PETS");
            FindViewById<Button>(Resource.Id.btnFood).Click += (s, e) => ShowAmountDialog("FOOD");
            FindViewById<Button>(Resource.Id.btnTransport).Click += (s, e) => ShowAmountDialog("TRANSPORT");
            FindViewById<Button>(Resource.Id.btnEntertainment).Click += (s, e) => ShowAmountDialog("ENTERTAINMENT");
            FindViewById<Button>(Resource.Id.btnOthers).Click += (s, e) => ShowAmountDialog("OTHERS");

            Button startNewMonthBtn = FindViewById<Button>(Resource.Id.resetWeekBtn);
            startNewMonthBtn.Click += (s, e) => {
                new AlertDialog.Builder(this)
                    .SetTitle("End Current Month?")
                    .SetMessage("A report will be saved to your Downloads folder before clearing data.")
                    .SetPositiveButton("Export & Clear", (sender, args) => {
                        string savedFile = ExportReceipt();

                        if (savedFile != null)
                        {
                            db.DeleteAllExpenses();
                            RefreshData();
                            UpdateBalance();
                            adapter.NotifyDataSetChanged();
                            Toast.MakeText(this, $"Saved: {savedFile}", ToastLength.Long).Show();
                        }
                        else
                        {
                            Toast.MakeText(this, "Failed to export. Reset cancelled.", ToastLength.Short).Show();
                        }
                    })
                    .SetNegativeButton("Cancel", (IDialogInterfaceOnClickListener)null)
                    .Show();
            };

            // 5. LOAD DATA
            weeklyBudget = db.GetBudget();
            monthlyBudget = weeklyBudget * 4;

            adapter = new ExpenseAdapter(this, expenseListData);
            expenseListView.Adapter = adapter;

            RefreshData();
            UpdateBalance();

            // 6. BUDGET SET BUTTON
            setBudgetBtn.Click += (s, e) => {
                if (double.TryParse(budgetInput.Text, out double inputAmount))
                {
                    // Now treating input as MONTHLY
                    monthlyBudget = inputAmount;
                    weeklyBudget = monthlyBudget / 4;

                    // Save the weekly portion to the database (so your existing logic works)
                    db.SaveBudget(weeklyBudget);

                    RefreshData();
                    UpdateBalance();

                    budgetInput.Text = ""; // Clear input
                    Toast.MakeText(this, "Monthly Allowance Updated", ToastLength.Short).Show();
                }
            };

            // 7. LIST VIEW DELETE
            expenseListView.ItemLongClick += (s, e) => {
                var item = expenseListData[e.Position];
                new AlertDialog.Builder(this)
                    .SetTitle("Delete Expense?")
                    .SetMessage($"Remove {item.Description}?")
                    .SetPositiveButton("Delete", (sender, args) => {
                        db.DeleteExpense(item.Id);
                        RefreshData();
                        UpdateBalance();
                        adapter.NotifyDataSetChanged();

                        ShowDeleteToast($"{item.Description} removed");
                    })
                    .SetNegativeButton("Cancel", (IDialogInterfaceOnClickListener)null)
                    .Show();
            };
        }

        // --- DIALOG LOGIC ---
        private void ShowAmountDialog(string category)
        {
            var dialogView = LayoutInflater.Inflate(Resource.Layout.dialog_amount_grid, null);
            var dialog = new AlertDialog.Builder(this).SetView(dialogView).Create();

            dialogView.FindViewById<TextView>(Resource.Id.dialogTitle).Text = $"Select Amount for {category}";
            var grid = dialogView.FindViewById<GridLayout>(Resource.Id.amountGrid);
            var customBtn = dialogView.FindViewById<Button>(Resource.Id.btnCustomAmount);

            string[] amounts = { "10", "15", "20", "25", "50", "100", "200", "500", "1000" };

            foreach (var amt in amounts)
            {
                Button btn = new Button(this);
                var param = new GridLayout.LayoutParams();
                param.Width = 0;
                param.Height = ViewGroup.LayoutParams.WrapContent;
                param.SetMargins(10, 10, 10, 10);
                param.ColumnSpec = GridLayout.InvokeSpec(GridLayout.Undefined, 1f);
                btn.LayoutParameters = param;

                btn.Text = $"₱{amt}";
                btn.SetBackgroundColor(Color.ParseColor("#E8F5E9"));
                btn.SetTextColor(Color.ParseColor("#2E7D32"));
                btn.SetTypeface(Typeface.Create("sans-serif-medium", TypefaceStyle.Normal), TypefaceStyle.Normal);

                btn.Click += (s, e) => {
                    dialog.Dismiss();
                    ShowNameDialog(category, double.Parse(amt));
                };
                grid.AddView(btn);
            }

            customBtn.Click += (s, e) => {
                dialog.Dismiss();
                ShowCustomAmountDialog(category);
            };
            dialog.Show();
        }
        private void ShowCustomAmountDialog(string category)
        {
            var inputBg = new Android.Graphics.Drawables.GradientDrawable();
            inputBg.SetCornerRadius(20f);
            inputBg.SetStroke(3, Color.ParseColor("#E0E0E0"));
            inputBg.SetColor(Color.ParseColor("#FAFAFA"));

            EditText input = new EditText(this)
            {
                Hint = "0.00",
                InputType = Android.Text.InputTypes.ClassNumber | Android.Text.InputTypes.NumberFlagDecimal,
                Background = inputBg
            };
            input.SetTypeface(Typeface.Create("sans-serif-black", TypefaceStyle.Normal), TypefaceStyle.Normal);
            input.SetTextSize(Android.Util.ComplexUnitType.Sp, 28); 
            input.SetTextColor(Color.ParseColor("#2E7D32")); 
            input.Gravity = GravityFlags.Center;
            input.SetPadding(30, 40, 30, 40);

            var container = new FrameLayout(this);
            var lp = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            lp.SetMargins(60, 40, 60, 40);
            container.LayoutParameters = lp;
            container.AddView(input);

            new AlertDialog.Builder(this)
                .SetTitle("Custom Amount")
                .SetView(container)
                .SetPositiveButton("NEXT", (s, e) => {
                    if (double.TryParse(input.Text, out double val)) ShowNameDialog(category, val);
                })
                .SetNegativeButton("CANCEL", (IDialogInterfaceOnClickListener)null)
                .Show();
        }

        private void ShowNameDialog(string category, double amount)
        {
            var inputBg = new Android.Graphics.Drawables.GradientDrawable();
            inputBg.SetCornerRadius(20f);
            inputBg.SetStroke(3, Color.ParseColor("#E0E0E0"));
            inputBg.SetColor(Color.ParseColor("#FAFAFA"));

            EditText input = new EditText(this)
            {
                Hint = "What did you buy?",
                InputType = Android.Text.InputTypes.TextFlagCapWords,
                Background = inputBg
            };
            input.SetTypeface(Typeface.Create("sans-serif-medium", TypefaceStyle.Normal), TypefaceStyle.Normal);
            input.SetPadding(40, 40, 40, 40);

            var container = new FrameLayout(this);
            var lp = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            lp.SetMargins(60, 40, 60, 20);
            container.LayoutParameters = lp;
            container.AddView(input);

            new AlertDialog.Builder(this)
                .SetTitle($"Item for {category}")
                .SetMessage($"Total: ₱{amount:N2}")
                .SetView(container)
                .SetPositiveButton("SAVE", (s, e) => {
                    string title = string.IsNullOrWhiteSpace(input.Text) ? category : input.Text;

                    SaveExpense(title, category, amount);
                    ShowSuccessToast($"{title} recorded ✓");
                })
                .SetNegativeButton("BACK", (s, e) => ShowAmountDialog(category))
                .Show();
        }

        private void ShowEssentialDialog(string type)
        {
            LinearLayout container = new LinearLayout(this);
            container.Orientation = Orientation.Vertical;
            container.SetPadding(60, 40, 60, 40);

            var inputBg = new GradientDrawable();
            inputBg.SetCornerRadius(15f);
            inputBg.SetStroke(2, Color.ParseColor("#E0E0E0"));
            inputBg.SetColor(Color.ParseColor("#F9F9F9"));

            // Name Field
            EditText nameInput = new EditText(this)
            {
                Hint = "What is this bill for?",
                Background = inputBg.GetConstantState().NewDrawable() 
            };
            nameInput.SetTypeface(Typeface.Create("sans-serif", TypefaceStyle.Normal), TypefaceStyle.Normal);
            nameInput.SetPadding(30, 30, 30, 30);
            EditText amountInput = new EditText(this)
            {
                Hint = "0.00",
                Background = inputBg,
                InputType = Android.Text.InputTypes.ClassNumber | Android.Text.InputTypes.NumberFlagDecimal
            };
            amountInput.SetTypeface(Typeface.Create("sans-serif-black", TypefaceStyle.Normal), TypefaceStyle.Normal);
            amountInput.SetTextSize(Android.Util.ComplexUnitType.Sp, 24);
            amountInput.SetTextColor(Color.ParseColor("#2c9e3b"));
            amountInput.SetPadding(30, 30, 30, 30);

            var spacer = new View(this);
            spacer.LayoutParameters = new LinearLayout.LayoutParams(1, 30);

            container.AddView(nameInput);
            container.AddView(spacer);
            container.AddView(amountInput);

            new AlertDialog.Builder(this)
                .SetTitle($"Add {type} Essential")
                .SetView(container)
                .SetPositiveButton("SAVE", (s, e) => {
                    if (double.TryParse(amountInput.Text, out double amt))
                    {
                        string name = string.IsNullOrWhiteSpace(nameInput.Text) ? "Essential" : nameInput.Text;
                        SaveExpense($"[{type}] {name}", "ESSENTIAL", amt);

                        // TRIGGER OUR CUSTOM SUCCESS TOAST
                        ShowSuccessToast($"{name} saved!");
                    }
                })
                .SetNegativeButton("CANCEL", (IDialogInterfaceOnClickListener)null)
                .Show();
        }

        private void ShowSuccessToast(string message)
        {
            LinearLayout toastLayout = new LinearLayout(this);
            toastLayout.Orientation = Orientation.Horizontal;
            toastLayout.SetPadding(40, 20, 40, 20);
            toastLayout.SetGravity(GravityFlags.CenterVertical);

            var background = new GradientDrawable();
            background.SetCornerRadius(50f); 
            background.SetColor(Color.ParseColor("#2E7D32")); 
            toastLayout.Background = background;

            TextView icon = new TextView(this);
            icon.Text = "✓";
            icon.SetTextColor(Color.White);
            icon.SetTypeface(Typeface.DefaultBold, TypefaceStyle.Bold);
            icon.SetPadding(0, 0, 20, 0);
            toastLayout.AddView(icon);

            TextView text = new TextView(this);
            text.Text = message;
            text.SetTextColor(Color.White);
            text.SetTypeface(Typeface.Create("sans-serif-medium", TypefaceStyle.Normal), TypefaceStyle.Normal);
            toastLayout.AddView(text);

            Toast customToast = new Toast(this);
            customToast.Duration = ToastLength.Short;
            customToast.View = toastLayout;
            customToast.Show();
        }
        private void ShowDeleteToast(string message)
        {
            LinearLayout toastLayout = new LinearLayout(this);
            toastLayout.Orientation = Orientation.Horizontal;
            toastLayout.SetPadding(40, 20, 40, 20);
            toastLayout.SetGravity(GravityFlags.CenterVertical);

            var background = new GradientDrawable();
            background.SetCornerRadius(50f);
            background.SetColor(Color.ParseColor("#D32F2F")); // Material Red
            toastLayout.Background = background;

            TextView icon = new TextView(this);
            icon.Text = "✕"; // Delete cross
            icon.SetTextColor(Color.White);
            icon.SetTypeface(Typeface.DefaultBold, TypefaceStyle.Bold);
            icon.SetPadding(0, 0, 20, 0);
            toastLayout.AddView(icon);

            TextView text = new TextView(this);
            text.Text = message;
            text.SetTextColor(Color.White);
            text.SetTypeface(Typeface.Create("sans-serif-medium", TypefaceStyle.Normal), TypefaceStyle.Normal);
            toastLayout.AddView(text);

            Toast deleteToast = new Toast(this);
            deleteToast.Duration = ToastLength.Short;
            deleteToast.View = toastLayout;
            deleteToast.Show();

            // Small vibration for deletion
            Vibrator vibrator = (Vibrator)GetSystemService(Context.VibratorService);
            vibrator.Vibrate(VibrationEffect.CreateOneShot(30, VibrationEffect.DefaultAmplitude));
        }

        private void SaveExpense(string title, string category, double amount)
        {
            Expense newExp = new Expense
            {
                Description = title,
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

        // --- CALCULATION LOGIC ---

        void RefreshData()
        {
            expenseListData.Clear();
            var allExpenses = db.GetAllExpenses() ?? new List<Expense>();
            expenseListData.AddRange(allExpenses);

            double totalToday = 0;
            double totalSpentMonthlyEssentials = 0;
            double totalSpentRegularThisWeek = 0;
            double totalSpentRegularOtherWeeks = 0;

            DateTime today = DateTime.Today;
            int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
            int daysLeft = (daysInMonth - today.Day) + 1; // +1 to include today

            var daysLeftText = FindViewById<TextView>(Resource.Id.daysLeftText);
            if (daysLeftText != null)
            {
                daysLeftText.Text = $"{daysLeft} Days Remaining";

                // UI Touch: Turn text orange if less than 5 days left
                if (daysLeft <= 5)
                    daysLeftText.SetTextColor(Color.ParseColor("#F57C00"));
                else
                    daysLeftText.SetTextColor(Color.Gray);
            }

            // Calculate the start of the current week
            DateTime startOfWeek = today.AddDays(-7);

            foreach (var exp in allExpenses)
            {
                bool isMonthlyEssential = exp.Category == "ESSENTIAL" && exp.Description.Contains("[MONTHLY]");
                DateTime expDate;
                bool hasValidDate = DateTime.TryParse(exp.Date, out expDate);

                if (isMonthlyEssential)
                {
                    totalSpentMonthlyEssentials += exp.Amount;
                }
                else
                {
                    // If the expense happened within the last 7 days, it counts against THIS week
                    if (hasValidDate && expDate.Date >= startOfWeek)
                    {
                        totalSpentRegularThisWeek += exp.Amount;
                    }
                    else
                    {
                        // Older expenses only affect the monthly total
                        totalSpentRegularOtherWeeks += exp.Amount;
                    }
                }

                // Today's tracker
                if (hasValidDate && expDate.Date == today)
                    totalToday += exp.Amount;
            }

            // 1. Monthly Balance (Everything subtracted)
            remainingMonthly = monthlyBudget - (totalSpentMonthlyEssentials + totalSpentRegularThisWeek + totalSpentRegularOtherWeeks);

            // 2. Weekly Balance
            double monthlyLeftAfterBills = monthlyBudget - totalSpentMonthlyEssentials;
            remainingWeekly = (monthlyLeftAfterBills / 4) - totalSpentRegularThisWeek;

            if (spentTodayText != null) spentTodayText.Text = $"PHP {totalToday:F2}";
        }

        void UpdateBalance()
        {
            monthlyBalanceText.Text = $"PHP {remainingMonthly:N2}";
            weeklyBalanceText.Text = $"PHP {remainingWeekly:N2}";

            if (monthlyBudget > 0)
            {
                int progress = (int)((remainingMonthly / monthlyBudget) * 100);
                progressBar.Progress = Math.Clamp(progress, 0, 100);

                if (progress >= 50)
                    monthlyBalanceText.SetTextColor(Color.ParseColor("#2E7D32")); // Green
                else if (progress >= 20)
                    monthlyBalanceText.SetTextColor(Color.ParseColor("#FFC107")); // Amber
                else
                {
                    monthlyBalanceText.SetTextColor(Color.ParseColor("#F44336")); // Red
                    if (!lowBalanceNotified && remainingMonthly < (monthlyBudget * 0.1)) // Notify at 10%
                    {
                        lowBalanceNotified = true;
                        ShowLowBudgetNotification(remainingMonthly);
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
                if (allExpenses == null || allExpenses.Count == 0)
                {
                    RunOnUiThread(() => Toast.MakeText(this, "No data to export!", ToastLength.Short).Show());
                    return null;
                }

                System.Text.StringBuilder receipt = new System.Text.StringBuilder();
                receipt.AppendLine("============================");
                receipt.AppendLine("      MYBUDGET REPORT       "); // Renamed to Report
                receipt.AppendLine($"   Date: {DateTime.Now:MMMM dd, yyyy}");
                receipt.AppendLine("============================");
                receipt.AppendLine(string.Format("{0,-15} {1,10}", "ITEM", "AMOUNT"));
                receipt.AppendLine("----------------------------");

                double total = 0;
                foreach (var exp in allExpenses)
                {
                    // Truncate long descriptions to keep columns aligned
                    string desc = exp.Description.Length > 15 ? exp.Description.Substring(0, 12) + "..." : exp.Description;
                    receipt.AppendLine(string.Format("{0,-15} ₱{1,10:F2}", desc, exp.Amount));
                    total += exp.Amount;
                }

                receipt.AppendLine("----------------------------");
                receipt.AppendLine(string.Format("{0,-15} ₱{1,10:F2}", "TOTAL SPENT", total));
                receipt.AppendLine("============================");
                receipt.AppendLine("   End of Monthly Report    "); // Changed from Weekly to Monthly
                receipt.AppendLine("============================");

                // 1. Create unique filename using Month and Year
                string filename = $"Budget_Report_{DateTime.Now:MMM_yyyy_HHmm}.txt";

                // 2. Get the Downloads folder path
                string path = System.IO.Path.Combine(
                    Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath,
                    filename);

                // 3. Write the file
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