using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using MyBudget.Adapters;
using MyBudget.Models;
using MyBudget.Services;

namespace MyBudget.Fragments
{
    public class ExpensesFragment : AndroidX.Fragment.App.Fragment
    {
        private DatabaseService? _dbService;
        private ExpenseAdapter? _expenseAdapter;

        private EditText? etSearchExpenses;
        private Spinner? spinnerFilterCategory;
        private TextView? tvTotalCount, tvTotalAmount;
        private LinearLayout? layoutEmptyState;
        private RecyclerView? rvAllExpenses;

        private List<Expense> _allExpenses = new List<Expense>();

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.fragment_expenses, container, false);

            _dbService = new DatabaseService();

            etSearchExpenses = view.FindViewById<EditText>(Resource.Id.etSearchExpenses);
            spinnerFilterCategory = view.FindViewById<Spinner>(Resource.Id.spinnerFilterCategory);
            tvTotalCount = view.FindViewById<TextView>(Resource.Id.tvTotalCount);
            tvTotalAmount = view.FindViewById<TextView>(Resource.Id.tvTotalAmount);
            layoutEmptyState = view.FindViewById<LinearLayout>(Resource.Id.layoutEmptyState);
            rvAllExpenses = view.FindViewById<RecyclerView>(Resource.Id.rvAllExpenses);

            rvAllExpenses.SetLayoutManager(new LinearLayoutManager(Context));
            _expenseAdapter = new ExpenseAdapter(new List<Expense>());
            rvAllExpenses.SetAdapter(_expenseAdapter);

            _expenseAdapter.ItemClick += OnExpenseClicked;

            var categories = new string[] { "All Categories", "Food & Dining", "Transportation", "Shopping", "Entertainment", "Bills & Utilities", "Healthcare", "Education", "Other" };
            var spinnerAdapter = new ArrayAdapter<string>(RequireContext(), Android.Resource.Layout.SimpleSpinnerItem, categories);
            spinnerAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinnerFilterCategory.Adapter = spinnerAdapter;

            etSearchExpenses.TextChanged += (s, e) => ApplyFilters();
            spinnerFilterCategory.ItemSelected += (s, e) => ApplyFilters();

            return view;
        }

        public override async void OnResume()
        {
            base.OnResume();
            await LoadAllExpensesAsync();
        }

        private async Task LoadAllExpensesAsync()
        {
            _allExpenses = await _dbService.GetExpensesAsync();
            ApplyFilters(); 
        }

        private void ApplyFilters()
        {
            string searchQuery = etSearchExpenses.Text?.ToLower() ?? "";
            string selectedCategory = spinnerFilterCategory.SelectedItem?.ToString() ?? "All Categories";

            var filteredList = _allExpenses.Where(e =>
                (string.IsNullOrWhiteSpace(searchQuery) || e.Name.ToLower().Contains(searchQuery)) &&
                (selectedCategory == "All Categories" || e.Category == selectedCategory)
            ).OrderByDescending(e => e.Date).ToList();

            RequireActivity().RunOnUiThread(() =>
            {
                tvTotalCount.Text = filteredList.Count.ToString();
                tvTotalAmount.Text = $"₱{filteredList.Sum(e => e.Amount):N2}";

                if (filteredList.Count == 0)
                {
                    layoutEmptyState.Visibility = ViewStates.Visible;
                    rvAllExpenses.Visibility = ViewStates.Gone;
                }
                else
                {
                    layoutEmptyState.Visibility = ViewStates.Gone;
                    rvAllExpenses.Visibility = ViewStates.Visible;
                    _expenseAdapter.UpdateData(filteredList);
                }
            });
        }

        private void OnExpenseClicked(object sender, (Expense expense, View view) data)
        {
            var ctx = Context ?? RequireActivity();
            var menu = new PopupMenu(ctx, data.view);

            menu.MenuItemClick += async (send, args) =>
            {
                var title = args.Item.TitleFormatted.ToString();
                if (title == "Delete")
                {
                    await _dbService.DeleteExpenseAsync(data.expense);
                    await LoadAllExpensesAsync(); 
                }
                else if (title == "Edit")
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