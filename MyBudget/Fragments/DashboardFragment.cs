using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.FloatingActionButton;
using MyBudget.Models;
using MyBudget.Services;

namespace MyBudget.Fragments
{
    public class DashboardFragment : AndroidX.Fragment.App.Fragment
    {
        private TextView? tvDailyLimit, tvTodaySpent, tvPeriodLabel, tvPeriodAmount, tvPeriodSpent;
        private ProgressBar? progressDaily;
        private FloatingActionButton? fabAddExpense;
        private DatabaseService? _dbService;
        private RecyclerView? rvRecentExpenses;
        private Adapters.ExpenseAdapter? expenseAdapter;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(MyBudget.Resource.Layout.fragment_dashboard, container, false);
            _dbService = new DatabaseService();

            tvDailyLimit = view.FindViewById<TextView>(MyBudget.Resource.Id.tvDailyLimit);
            tvTodaySpent = view.FindViewById<TextView>(MyBudget.Resource.Id.tvTodaySpent);
            tvPeriodLabel = view.FindViewById<TextView>(MyBudget.Resource.Id.tvPeriodLabel);
            tvPeriodAmount = view.FindViewById<TextView>(MyBudget.Resource.Id.tvPeriodAmount);
            tvPeriodSpent = view.FindViewById<TextView>(MyBudget.Resource.Id.tvPeriodSpent);
            progressDaily = view.FindViewById<ProgressBar>(MyBudget.Resource.Id.progressDaily);
            fabAddExpense = view.FindViewById<FloatingActionButton>(MyBudget.Resource.Id.fabAddExpense);

            rvRecentExpenses = view.FindViewById<RecyclerView>(Resource.Id.rvRecentExpenses);
            rvRecentExpenses.SetLayoutManager(new LinearLayoutManager(Context));
            expenseAdapter = new Adapters.ExpenseAdapter(new List<Expense>());
            rvRecentExpenses.SetAdapter(expenseAdapter);

            fabAddExpense.Click += (sender, e) =>
            {
                var intent = new Android.Content.Intent(RequireActivity(), typeof(AddExpenseActivity));
                StartActivity(intent);
            };

            expenseAdapter.ItemClick += (s, data) => {
                var ctx = Context ?? RequireActivity();
                var menu = new PopupMenu(ctx, data.view);

                menu.MenuItemClick += async (send, args) => {
                    var title = args.Item.TitleFormatted.ToString();

                    if (title == "Delete")
                    {
                        if (data.expense != null && _dbService != null)
                        {
                            await _dbService.DeleteExpenseAsync(data.expense);
                            await LoadDashboardDataAsync();
                        }
                    }
                    else
                    {
                        var intent = new Android.Content.Intent(ctx, typeof(AddExpenseActivity));
                        intent.PutExtra("ExpenseId", data.expense.Id);
                        StartActivity(intent);
                    }
                };
                menu.Menu.Add("Edit");
                menu.Menu.Add("Delete");
                menu.Show();
            };

            return view;
        }

        public override async void OnResume()
        {
            base.OnResume();
            try
            {
                await LoadDashboardDataAsync();
            }
            catch (Exception ex)
            {
                Activity?.RunOnUiThread(() =>
                {
                    Toast.MakeText(RequireActivity(), $"Crash Prevented: {ex.Message}", ToastLength.Long).Show();
                });
            }
        }

        private async Task LoadDashboardDataAsync()
        {
            var settings = await _dbService.GetBudgetSettingsAsync();
            var allExpenses = await _dbService.GetExpensesAsync();

            int daysDivider = settings.Period == BudgetPeriod.Weekly ? 7 : 30;
            decimal dailyLimit = settings.Amount / daysDivider;

            var today = DateTime.Today;
            decimal todaySpent = allExpenses
                .Where(e => e.Date.Date == today)
                .Sum(e => e.Amount);

            DateTime periodStart = settings.Period == BudgetPeriod.Weekly
                ? today.AddDays(-(int)today.DayOfWeek)
                : new DateTime(today.Year, today.Month, 1);

            decimal periodSpent = allExpenses
                .Where(e => e.Date >= periodStart)
                .Sum(e => e.Amount);

            var recentExpenses = allExpenses.OrderByDescending(e => e.Date).Take(5).ToList();


            RequireActivity().RunOnUiThread(() =>
            {
                tvDailyLimit.Text = $"Daily limit: ₱{dailyLimit:N2}";
                tvTodaySpent.Text = $"₱{todaySpent:N2}";

                tvPeriodLabel.Text = settings.Period == BudgetPeriod.Weekly ? "Weekly Budget" : "Monthly Budget";
                tvPeriodAmount.Text = $"₱{settings.Amount:N2}";
                tvPeriodSpent.Text = $"₱{periodSpent:N2}";

                int progressPercentage = dailyLimit > 0 ? (int)((todaySpent / dailyLimit) * 100) : 0;
                progressDaily.Progress = Math.Min(progressPercentage, 100);

                if (expenseAdapter == null)
                {
                    expenseAdapter = new Adapters.ExpenseAdapter(recentExpenses);
                    rvRecentExpenses.SetAdapter(expenseAdapter);
                }
                else
                {
                    expenseAdapter.UpdateData(recentExpenses);
                }
            });
        }
    }
}