using Android.Views;
using AndroidX.RecyclerView.Widget;
using MyBudget.Adapters;
using MyBudget.Models;
using MyBudget.Services;

using Microcharts;
using Microcharts.Droid;
using SkiaSharp;

namespace MyBudget.Fragments
{
    public class StatisticsFragment : AndroidX.Fragment.App.Fragment
    {
        private DatabaseService? _dbService;
        private CategoryBreakdownAdapter? _categoryAdapter;

        private TextView? tvMonthTotal, tvMonthCount, tvDailyAverage;
        private TextView? tvTopCategoryName, tvTopCategoryAmount, tvTotalCount;
        private ImageView? ivTopCategoryIcon;
        private RecyclerView? rvTopCategoriesBreakdown;

        private ChartView? chartWeekly;
        private ChartView? chartCategory;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.fragment_statistics, container, false);
            _dbService = new DatabaseService();

            tvMonthTotal = view.FindViewById<TextView>(Resource.Id.tvMonthTotal);
            tvMonthCount = view.FindViewById<TextView>(Resource.Id.tvMonthCount);
            tvDailyAverage = view.FindViewById<TextView>(Resource.Id.tvDailyAverage);
            tvTopCategoryName = view.FindViewById<TextView>(Resource.Id.tvTopCategoryName);
            tvTopCategoryAmount = view.FindViewById<TextView>(Resource.Id.tvTopCategoryAmount);
            tvTotalCount = view.FindViewById<TextView>(Resource.Id.tvTotalCount);
            ivTopCategoryIcon = view.FindViewById<ImageView>(Resource.Id.ivTopCategoryIcon);
            rvTopCategoriesBreakdown = view.FindViewById<RecyclerView>(Resource.Id.rvTopCategoriesBreakdown);

            chartWeekly = view.FindViewById<ChartView>(Resource.Id.chartWeekly);
            chartCategory = view.FindViewById<ChartView>(Resource.Id.chartCategory);

            rvTopCategoriesBreakdown.SetLayoutManager(new LinearLayoutManager(Context));
            _categoryAdapter = new CategoryBreakdownAdapter(new List<CategorySummary>());
            rvTopCategoriesBreakdown.SetAdapter(_categoryAdapter);

            return view;
        }

        public override async void OnResume()
        {
            base.OnResume();
            await LoadStatsDataAsync();
        }

        private async Task LoadStatsDataAsync()
        {
            var allExpenses = await _dbService.GetExpensesAsync();
            if (allExpenses == null || allExpenses.Count == 0) return;

            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthExpenses = allExpenses.Where(e => e.Date >= monthStart).ToList();

            decimal monthSpent = monthExpenses.Sum(e => e.Amount);
            int monthCount = monthExpenses.Count;
            decimal dailyAvg = DateTime.Now.Day > 0 ? monthSpent / DateTime.Now.Day : monthSpent;

            var groupedExpenses = allExpenses.GroupBy(e => e.Category);
            List<CategorySummary> categorySummaries = groupedExpenses.Select(group => new CategorySummary
            {
                CategoryName = group.Key,
                TotalAmount = group.Sum(e => e.Amount),
                PercentageOfTotal = 0m
            }).OrderByDescending(s => s.TotalAmount).ToList();

            decimal grandTotal = allExpenses.Sum(e => e.Amount);
            if (grandTotal > 0)
            {
                categorySummaries.ForEach(s => s.PercentageOfTotal = (s.TotalAmount / grandTotal) * 100);
            }
            var topCategory = categorySummaries.FirstOrDefault();

            var weeklyEntries = GenerateWeeklyChartData(allExpenses);
            var categoryEntries = GenerateCategoryChartData(categorySummaries);

            RequireActivity().RunOnUiThread(() =>
            {
                tvMonthTotal.Text = $"₱{monthSpent:N2}";
                tvMonthCount.Text = $"{monthCount} expenses";
                tvDailyAverage.Text = $"₱{dailyAvg:N2}";
                tvTotalCount.Text = allExpenses.Count.ToString();

                if (topCategory != null)
                {
                    tvTopCategoryName.Text = topCategory.CategoryName;
                    tvTopCategoryAmount.Text = $"₱{topCategory.TotalAmount:N2}";
                    SetCategoryIconStyle(ivTopCategoryIcon, topCategory.CategoryName);
                }

                var topBreakdown = categorySummaries.Take(3).ToList();
                _categoryAdapter.UpdateData(topBreakdown);

                chartWeekly.Chart = new BarChart
                {
                    Entries = weeklyEntries,
                    BackgroundColor = SKColors.Transparent,
                    LabelTextSize = 30f,
                    Margin = 20f
                };

                chartCategory.Chart = new DonutChart
                {
                    Entries = categoryEntries,
                    BackgroundColor = SKColors.Transparent,
                    LabelTextSize = 30f,
                    HoleRadius = 0.5f 
                };
            });
        }

        private List<ChartEntry> GenerateWeeklyChartData(List<Expense> allExpenses)
        {
            var entries = new List<ChartEntry>();
            var mintGreen = SKColor.Parse("#10B981"); 

            for (int i = 6; i >= 0; i--)
            {
                var targetDate = DateTime.Today.AddDays(-i);
                var spentThatDay = allExpenses.Where(e => e.Date.Date == targetDate).Sum(e => e.Amount);

                entries.Add(new ChartEntry((float)spentThatDay)
                {
                    Label = targetDate.ToString("ddd"),
                    ValueLabel = spentThatDay > 0 ? $"₱{spentThatDay:0}" : "", 
                    Color = mintGreen
                });
            }
            return entries;
        }

        private List<ChartEntry> GenerateCategoryChartData(List<CategorySummary> summaries)
        {
            var entries = new List<ChartEntry>();

            foreach (var summary in summaries)
            {
                var categoryColor = GetSkiaColorForCategory(summary.CategoryName ?? "");

                entries.Add(new ChartEntry((float)summary.TotalAmount)
                {
                    Label = summary.CategoryName,
                    ValueLabel = $"₱{summary.TotalAmount:0}",

                    Color = categoryColor,
                    TextColor = categoryColor,
                    ValueLabelColor = categoryColor
                });
            }
            return entries;
        }

        private SKColor GetSkiaColorForCategory(string category)
        {
            switch (category)
            {
                case "Food & Dining": return SKColor.Parse("#D97706");
                case "Transportation": return SKColor.Parse("#2563EB");
                case "Shopping": return SKColor.Parse("#9333EA");
                case "Bills & Utilities": return SKColor.Parse("#DC2626");
                default: return SKColor.Parse("#4B5563");
            }
        }

        private void SetCategoryIconStyle(ImageView iv, string category)
        {
            string iconTintHex;
            int iconResId;

            switch (category)
            {
                case "Food & Dining": iconTintHex = "#D97706"; iconResId = Android.Resource.Drawable.IcMenuMyPlaces; break;
                case "Transportation": iconTintHex = "#2563EB"; iconResId = Android.Resource.Drawable.IcMenuDirections; break;
                case "Shopping": iconTintHex = "#9333EA"; iconResId = Android.Resource.Drawable.IcMenuSortBySize; break;
                case "Bills & Utilities": iconTintHex = "#DC2626"; iconResId = Android.Resource.Drawable.IcMenuRecentHistory; break;
                default: iconTintHex = "#4B5563"; iconResId = Android.Resource.Drawable.IcMenuAgenda; break;
            }
            iv.SetImageResource(iconResId);
            iv.SetColorFilter(Android.Graphics.Color.ParseColor(iconTintHex));
        }

        public class CategorySummary
        {
            public string? CategoryName { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal PercentageOfTotal { get; set; }
        }
    }
}