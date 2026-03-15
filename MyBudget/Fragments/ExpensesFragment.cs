using Android.Views;
using AndroidX.RecyclerView.Widget;
using MyBudget.Models;
using MyBudget.Services;
using MyBudget.Adapters;

namespace MyBudget.Fragments
{
    public class ExpensesFragment : AndroidX.Fragment.App.Fragment
    {
        private RecyclerView rvExpenses;
        private ExpenseAdapter _adapter;
        private DatabaseService _dbService;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(MyBudget.Resource.Layout.fragment_expenses, container, false);

            _dbService = new DatabaseService();
            rvExpenses = view.FindViewById<RecyclerView>(MyBudget.Resource.Id.rvExpenses);

            rvExpenses.SetLayoutManager(new LinearLayoutManager(RequireContext()));

            _adapter = new ExpenseAdapter(new List<Expense>());
            rvExpenses.SetAdapter(_adapter);

            return view;
        }

        public override async void OnResume()
        {
            base.OnResume();
            try
            {
                await LoadExpensesAsync();
            }
            catch (Exception ex)
            {
                Activity?.RunOnUiThread(() =>
                {
                    Toast.MakeText(RequireActivity(), $"Error loading expenses: {ex.Message}", ToastLength.Long).Show();
                });
            }
        }

        private async Task LoadExpensesAsync()
        {
            var allExpenses = await _dbService.GetExpensesAsync();
            RequireActivity().RunOnUiThread(() =>
            {
                _adapter.UpdateData(allExpenses);
            });
        }
    }
}