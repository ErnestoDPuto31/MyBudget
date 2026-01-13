using Android.App;
using Android.OS;
using Android.Widget;
using Android.Graphics;
using System.Collections.Generic;

namespace AndroidApp1
{
    [Activity(Label = "MyBudget")]
    public class MainActivity : Activity
    {
        double weeklyBudget = 0;
        double remainingBalance = 0;
        bool lowBalanceNotified = false;

        List<Expense> expenseListData = new List<Expense>();
        ExpenseAdapter adapter;
        DatabaseHelper db;

        const string CHANNEL_ID = "budget_channel";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
            {
                if (CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Android.Content.PM.Permission.Granted)
                {
                    RequestPermissions(new string[] { Android.Manifest.Permission.PostNotifications }, 1001);
                }
            }

            CreateNotificationChannel();

            db = new DatabaseHelper(this);

            // CONNECT UI
            EditText budgetInput = FindViewById<EditText>(Resource.Id.budgetInput);
            Button setBudgetBtn = FindViewById<Button>(Resource.Id.setBudgetBtn);
            TextView balanceText = FindViewById<TextView>(Resource.Id.balanceText);
            ProgressBar progressBar = FindViewById<ProgressBar>(Resource.Id.budgetProgress);
            EditText expenseAmount = FindViewById<EditText>(Resource.Id.expenseAmount);
            EditText expenseDesc = FindViewById<EditText>(Resource.Id.expenseDesc);
            Spinner categorySpinner = FindViewById<Spinner>(Resource.Id.categorySpinner);
            Button addExpenseBtn = FindViewById<Button>(Resource.Id.addExpenseBtn);
            ListView expenseListView = FindViewById<ListView>(Resource.Id.expenseListView);

            // Spinner
            var categories = new List<string> { "Food", "Transport", "Entertainment", "Others" };
            var spinnerAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, categories);
            spinnerAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            categorySpinner.Adapter = spinnerAdapter;

            // Load saved budget
            weeklyBudget = db.GetBudget();

            // Load saved expenses
            expenseListData.Clear();
            expenseListData.AddRange(db.GetAllExpenses());

            // Compute remaining balance
            remainingBalance = weeklyBudget;
            foreach (var exp in expenseListData)
            {
                remainingBalance -= exp.Amount;
            }

            // Set adapter
            adapter = new ExpenseAdapter(this, expenseListData);
            expenseListView.Adapter = adapter;

            // Update balance display
            UpdateBalance(balanceText, progressBar);


            // SET BUDGET
            setBudgetBtn.Click += (s, e) =>
            {
                if (double.TryParse(budgetInput.Text, out weeklyBudget))
                {
                    remainingBalance = weeklyBudget;
                    db.SaveBudget(weeklyBudget);
                    UpdateBalance(balanceText, progressBar);
                }
                else
                {
                    Toast.MakeText(this, "Enter a valid budget", ToastLength.Short).Show();
                }
            };

            // ADD EXPENSE
            addExpenseBtn.Click += (s, e) =>
            {
                if (double.TryParse(expenseAmount.Text, out double amount) && !string.IsNullOrEmpty(expenseDesc.Text))
                {
                    string desc = expenseDesc.Text;
                    string category = categorySpinner.SelectedItem.ToString();

                    Expense newExp = new Expense { Description = desc, Amount = amount, Category = category };
                    db.AddExpense(newExp);

                    // reload expenses from DB
                    expenseListData.Clear();
                    expenseListData.AddRange(db.GetAllExpenses());
                    adapter.NotifyDataSetChanged();

                    remainingBalance -= amount;
                    UpdateBalance(balanceText, progressBar);

                    expenseAmount.Text = "";
                    expenseDesc.Text = "";
                }
            };

            // EDIT / DELETE
            expenseListView.ItemClick += (s, e) =>
            {
                var selectedExpense = expenseListData[e.Position];

                AlertDialog.Builder dialog = new AlertDialog.Builder(this);
                dialog.SetTitle("Edit or Delete");
                dialog.SetMessage($"{selectedExpense.Description} - ₱{selectedExpense.Amount}");

                dialog.SetPositiveButton("Edit", (sender, args) =>
                {
                    LinearLayout layout = new LinearLayout(this) { Orientation = Orientation.Vertical };
                    EditText inputDesc = new EditText(this) { Text = selectedExpense.Description };
                    EditText inputAmount = new EditText(this)
                    {
                        Text = selectedExpense.Amount.ToString(),
                        InputType = Android.Text.InputTypes.NumberFlagDecimal | Android.Text.InputTypes.ClassNumber
                    };
                    layout.AddView(inputDesc);
                    layout.AddView(inputAmount);

                    AlertDialog.Builder editDialog = new AlertDialog.Builder(this);
                    editDialog.SetTitle("Edit Expense");
                    editDialog.SetView(layout);
                    editDialog.SetPositiveButton("Update", (s2, a2) =>
                    {
                        if (double.TryParse(inputAmount.Text, out double newAmount))
                        {
                            remainingBalance += selectedExpense.Amount - newAmount;
                            selectedExpense.Description = inputDesc.Text;
                            selectedExpense.Amount = newAmount;
                            db.UpdateExpense(selectedExpense);

                            expenseListData.Clear();
                            expenseListData.AddRange(db.GetAllExpenses());
                            adapter.NotifyDataSetChanged();

                            UpdateBalance(balanceText, progressBar);
                        }
                    });
                    editDialog.SetNegativeButton("Cancel", (s2, a2) => { });
                    editDialog.Show();
                });

                dialog.SetNegativeButton("Delete", (sender, args) =>
                {
                    remainingBalance += selectedExpense.Amount;
                    db.DeleteExpense(selectedExpense.Id);

                    expenseListData.Clear();
                    expenseListData.AddRange(db.GetAllExpenses());
                    adapter.NotifyDataSetChanged();

                    UpdateBalance(balanceText, progressBar);
                });

                dialog.SetNeutralButton("Cancel", (sender, args) => { });
                dialog.Show();
            };
        }

        // CREATE NOTIFICATION CHANNEL

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

        // SHOW LOW BUDGET NOTIFICATION
        private void ShowLowBudgetNotification(double remaining)
        {
            var builder = new Notification.Builder(this, CHANNEL_ID)
                .SetContentTitle("Low Budget Alert!")
                .SetContentText($"You only have ₱{remaining:F2} left for this week.")
                .SetLargeIcon(BitmapFactory.DecodeResource(Resources, Resource.Mipmap.appicon))
                .SetSmallIcon(Resource.Mipmap.notificationicon) // your app icon
                .SetAutoCancel(true);

            var notificationManager = NotificationManager.FromContext(this);
            notificationManager.Notify(1, builder.Build());
        }

        // UPDATE BALANCE DISPLAY
        void UpdateBalance(TextView balanceText, ProgressBar progressBar)
        {
            balanceText.Text = $"Remaining: ₱{remainingBalance:F2}";

            if (weeklyBudget > 0)
            {
                int progress = (int)((remainingBalance / weeklyBudget) * 100);
                progressBar.Progress = progress;

                if (progress >= 50)
                {
                    balanceText.SetTextColor(Color.ParseColor("#4CAF50"));
                    lowBalanceNotified = false; // reset
                }
                else if (progress >= 20)
                {
                    balanceText.SetTextColor(Color.ParseColor("#FFC107"));
                    lowBalanceNotified = false; // reset
                }
                else
                {
                    balanceText.SetTextColor(Color.ParseColor("#F44336"));

                    // Only notify once when below 20%
                    if (!lowBalanceNotified)
                    {
                        lowBalanceNotified = true;
                        ShowLowBudgetNotification(remainingBalance);
                    
                    }
                }
            }
            else
            {
                progressBar.Progress = 0;
                balanceText.SetTextColor(Color.ParseColor("#000000"));
                lowBalanceNotified = false;
            }
        }
    }
}
