using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.FloatingActionButton;
using MyBudget.Adapters;
using MyBudget.Models;
using MyBudget.Services;

namespace MyBudget.Fragments
{
    public class DashboardFragment : AndroidX.Fragment.App.Fragment
    {
        private DatabaseService? _dbService;
        private ExpenseAdapter? _expenseAdapter;
        private RecyclerView? rvRecentExpenses;

        private TextView? tvDate, tvDailyLimit, tvTodaySpent, tvRemainingToday, tvProgressPercent;
        private TextView? tvMonthlyBudget, tvMonthlySpent, tvAvgDaily, tvAvgTrend, tvWarningText;
        private ProgressBar? progressDaily, progressMonthly;
        private LinearLayout? layoutWarning;
        private FloatingActionButton? fabAddExpense;

        public override View? OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.fragment_dashboard, container, false);
            _dbService = new DatabaseService();

            rvRecentExpenses = view.FindViewById<RecyclerView>(Resource.Id.rvRecentExpenses);
            rvRecentExpenses.SetLayoutManager(new LinearLayoutManager(Context));
            _expenseAdapter = new ExpenseAdapter(new List<Expense>());
            rvRecentExpenses.SetAdapter(_expenseAdapter);

            tvDate = view.FindViewById<TextView>(Resource.Id.tvDate);
            tvDailyLimit = view.FindViewById<TextView>(Resource.Id.tvDailyLimit);
            tvTodaySpent = view.FindViewById<TextView>(Resource.Id.tvTodaySpent);
            tvRemainingToday = view.FindViewById<TextView>(Resource.Id.tvRemainingToday);
            tvProgressPercent = view.FindViewById<TextView>(Resource.Id.tvProgressPercent);

            tvMonthlyBudget = view.FindViewById<TextView>(Resource.Id.tvMonthlyBudget);
            tvMonthlySpent = view.FindViewById<TextView>(Resource.Id.tvMonthlySpent);
            tvAvgDaily = view.FindViewById<TextView>(Resource.Id.tvAvgDaily);
            tvAvgTrend = view.FindViewById<TextView>(Resource.Id.tvAvgTrend);
            tvWarningText = view.FindViewById<TextView>(Resource.Id.tvWarningText);

            progressDaily = view.FindViewById<ProgressBar>(Resource.Id.progressDaily);
            progressMonthly = view.FindViewById<ProgressBar>(Resource.Id.progressMonthly);
            layoutWarning = view.FindViewById<LinearLayout>(Resource.Id.layoutWarning);

            fabAddExpense = view.FindViewById<FloatingActionButton>(Resource.Id.fabAddExpense);

            tvDate.Text = DateTime.Now.ToString("dddd, MMMM d, yyyy");

            fabAddExpense.Click += (s, e) => {
                StartActivity(new Intent(Context, typeof(AddExpenseActivity)));
            };

            _expenseAdapter.ItemClick += OnExpenseClicked;

            return view;
        }

        public override async void OnResume()
        {
            base.OnResume();
            await LoadDashboardDataAsync();
        }

        private async Task LoadDashboardDataAsync()
        {
            var allExpenses = await _dbService.GetExpensesAsync();
            var settings = await _dbService.GetBudgetSettingsAsync();

            decimal budgetLimit = settings?.Amount ?? 20000m;
            bool isWeekly = settings?.Period == BudgetPeriod.Weekly;

            int daysInPeriod = isWeekly ? 7 : DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
            decimal dailyLimit = budgetLimit / daysInPeriod;

            var todayExpenses = allExpenses.Where(e => e.Date.Date == DateTime.Today).ToList();
            var periodExpenses = isWeekly
                ? allExpenses.Where(e => e.Date >= DateTime.Now.AddDays(-7)).ToList() 
                : allExpenses.Where(e => e.Date.Month == DateTime.Now.Month && e.Date.Year == DateTime.Now.Year).ToList();

            decimal spentToday = todayExpenses.Sum(e => e.Amount);
            decimal spentPeriod = periodExpenses.Sum(e => e.Amount);
            decimal remainingToday = Math.Max(0, dailyLimit - spentToday);

            int elapsedDays = isWeekly ? ((int)DateTime.Now.DayOfWeek == 0 ? 7 : (int)DateTime.Now.DayOfWeek) : DateTime.Now.Day;
            decimal avgDaily = elapsedDays > 0 ? spentPeriod / elapsedDays : spentPeriod;

            RequireActivity().RunOnUiThread(() =>
            {
                string periodLabel = isWeekly ? "Weekly Budget" : "Monthly Budget";

                tvMonthlyBudget.Text = $"₱ {budgetLimit:N0}";
                tvMonthlySpent.Text = $"₱ {spentPeriod:N2} spent this {(isWeekly ? "week" : "month")}";

                tvDailyLimit.Text = $"Daily limit: ₱ {dailyLimit:N2}";
                tvTodaySpent.Text = $"₱ {spentToday:N2}";
                tvRemainingToday.Text = $"₱ {remainingToday:N2}";

                int dailyPercent = dailyLimit > 0 ? (int)((spentToday / dailyLimit) * 100) : 0;
                progressDaily.Progress = Math.Min(dailyPercent, 100);
                tvProgressPercent.Text = $"{dailyPercent}% of daily budget used";

                if (spentToday > dailyLimit)
                {
                    layoutWarning.Visibility = ViewStates.Visible;
                    tvWarningText.Text = $"You've exceeded your daily limit by ₱ {(spentToday - dailyLimit):N2}";
                }
                else
                {
                    layoutWarning.Visibility = ViewStates.Gone;
                }

                int periodPercent = budgetLimit > 0 ? (int)((spentPeriod / budgetLimit) * 100) : 0;
                progressMonthly.Progress = Math.Min(periodPercent, 100);

                tvAvgDaily.Text = $"₱ {avgDaily:N2}";
                if (avgDaily > dailyLimit)
                {
                    tvAvgTrend.Text = "↗ Over budget";
                    tvAvgTrend.SetTextColor(Android.Graphics.Color.ParseColor("#DC2626"));
                }
                else
                {
                    tvAvgTrend.Text = "↘ On track";
                    tvAvgTrend.SetTextColor(Android.Graphics.Color.ParseColor("#059669"));
                }

                var recentList = allExpenses.OrderByDescending(e => e.Date).Take(5).ToList();
                _expenseAdapter.UpdateData(recentList);
            });
        }

        private void OnExpenseClicked(object sender, (Expense expense, View view) data)
        {
            var ctx = Context ?? RequireActivity();
            var menu = new PopupMenu(ctx, data.view);
            menu.MenuItemClick += async (send, args) =>
            {
                if (args.Item.TitleFormatted.ToString() == "Delete")
                {
                    await _dbService.DeleteExpenseAsync(data.expense);
                    await LoadDashboardDataAsync();
                }
                else
                {
                    var intent = new Intent(ctx, typeof(AddExpenseActivity));
                    intent.PutExtra("ExpenseId", data.expense.Id);
                    StartActivity(intent);
                }
            };
            menu.Menu.Add("Edit");
            menu.Menu.Add("Delete");
            menu.Show();
        }
    }
}