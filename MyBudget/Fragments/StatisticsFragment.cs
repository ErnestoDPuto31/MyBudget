
using Android.Views;
using MyBudget.Services;

namespace MyBudget.Fragments
{
    public class StatisticsFragment : AndroidX.Fragment.App.Fragment
    {
        private TextView? statsTotalAmount;
        private LinearLayout? categoryContainer;
        private DatabaseService? dbService;

        public override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            dbService = new DatabaseService();
        }

        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.fragment_statistics, container, false);

            statsTotalAmount = view.FindViewById<TextView>(Resource.Id.statsTotalAmount);
            categoryContainer = view.FindViewById<LinearLayout>(Resource.Id.categoryContainer);

            return view;
        }

        public override async void OnResume()
        {
            base.OnResume();
            try
            {
                await LoadStatisticsAsync();
            }
            catch (Exception ex)
            {
                RequireActivity().RunOnUiThread(() =>
                {
                    Toast.MakeText(RequireActivity(), $"Error loading stats: {ex.Message}", ToastLength.Long).Show();
                });
            }
        }

        private async Task LoadStatisticsAsync()
        {
            var allExpenses = await dbService.GetExpensesAsync();
            decimal grandTotal = allExpenses.Sum(e => e.Amount);

            var categoryStats = allExpenses
                .GroupBy(e => e.Category)
                .Select(group => new
                {
                    CategoryName = string.IsNullOrEmpty(group.Key) ? "Uncategorized" : group.Key,
                    Total = group.Sum(e => e.Amount)
                })
                .OrderByDescending(stat => stat.Total)
                .ToList();

            RequireActivity().RunOnUiThread(() =>
            {
                statsTotalAmount.Text = $"${grandTotal:F2}";
                categoryContainer.RemoveAllViews();

                foreach (var stat in categoryStats)
                {
                    LinearLayout row = new LinearLayout(RequireContext())
                    {
                        Orientation = Android.Widget.Orientation.Horizontal,
                        LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
                        {
                            BottomMargin = 20
                        }
                    };

                    TextView nameText = new TextView(RequireContext())
                    {
                        Text = stat.CategoryName,
                        TextSize = 16f,
                        LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f)
                    };
                    nameText.SetTextColor(Android.Graphics.Color.ParseColor("#212121"));

                    TextView amountText = new TextView(RequireContext())
                    {
                        Text = $"${stat.Total:F2}",
                        TextSize = 16f,
                        Typeface = Android.Graphics.Typeface.DefaultBold
                    };
                    amountText.SetTextColor(Android.Graphics.Color.ParseColor("#388E3C"));

                    row.AddView(nameText);
                    row.AddView(amountText);
                    categoryContainer.AddView(row);
                }
            });
        }
    }
}