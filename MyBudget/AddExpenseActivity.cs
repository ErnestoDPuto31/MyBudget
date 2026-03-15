using AndroidX.AppCompat.App; 
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using MyBudget.Models;
using MyBudget.Services;

namespace MyBudget
{
    [Activity(Label = "Add Expense", Theme = "@style/Theme.MyBudget")]
    public class AddExpenseActivity : AppCompatActivity
    {
        private TextInputEditText? etDescription, etAmount, etNotes;
        private Spinner? spinnerCategory;
        private MaterialButton? btnSaveExpense;
        private TextView? tvPageTitle;
        private DatabaseService? _dbService;

        private string? _editingExpenseId = null;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_add_expense);

            _dbService = new DatabaseService();

            tvPageTitle = FindViewById<TextView>(Resource.Id.tvPageTitle);
            etDescription = FindViewById<TextInputEditText>(Resource.Id.etDescription);
            etAmount = FindViewById<TextInputEditText>(Resource.Id.etAmount);
            etNotes = FindViewById<TextInputEditText>(Resource.Id.etNotes);
            spinnerCategory = FindViewById<Spinner>(Resource.Id.spinnerCategory);
            btnSaveExpense = FindViewById<MaterialButton>(Resource.Id.btnSaveExpense);

            SetupCategorySpinner();

            _editingExpenseId = Intent.GetStringExtra("ExpenseId");
            if (!string.IsNullOrEmpty(_editingExpenseId))
            {
                await LoadExpenseDataForEditing();
            }

            btnSaveExpense.Click += async (sender, e) => await SaveExpenseAsync();
        }

        private void SetupCategorySpinner()
        {
            var categories = new string[] { "Food & Dining", "Transportation", "Shopping", "Entertainment", "Bills & Utilities", "Healthcare", "Education", "Other" };
            var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, categories);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinnerCategory.Adapter = adapter;
        }

        private async Task LoadExpenseDataForEditing()
        {
            var allExpenses = await _dbService.GetExpensesAsync();
            var expense = allExpenses.FirstOrDefault(e => e.Id == _editingExpenseId);

            if (expense != null)
            {
                tvPageTitle.Text = "Edit Expense";
                btnSaveExpense.Text = "Update Expense";
                etDescription.Text = expense.Name; 
                etAmount.Text = expense.Amount.ToString();
                etNotes.Text = expense.Notes;

      
                var adapter = (ArrayAdapter<string>)spinnerCategory.Adapter;
                int position = adapter.GetPosition(expense.Category);
                spinnerCategory.SetSelection(position);
            }
        }

        private async Task SaveExpenseAsync()
        {
            if (string.IsNullOrWhiteSpace(etDescription.Text) || string.IsNullOrWhiteSpace(etAmount.Text))
            {
                Toast.MakeText(this, "Please fill in the details", ToastLength.Short).Show();
                return;
            }

            if (!decimal.TryParse(etAmount.Text, out decimal amount))
            {
                Toast.MakeText(this, "Invalid amount", ToastLength.Short).Show();
                return;
            }

            var expense = new Expense
            {
                Id = string.IsNullOrEmpty(_editingExpenseId) ? Guid.NewGuid().ToString() : _editingExpenseId,
                Name = etDescription.Text.Trim(),
                Amount = amount,
                Category = spinnerCategory.SelectedItem.ToString(),
                Notes = etNotes.Text?.Trim(),
                Date = DateTime.Now
            };

            if (string.IsNullOrEmpty(_editingExpenseId))
                await _dbService.AddExpenseAsync(expense);
            else
                await _dbService.UpdateExpenseAsync(expense);

            Toast.MakeText(this, "Success!", ToastLength.Short).Show();
            Finish();
        }
    }
}