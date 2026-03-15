using SQLite;
using MyBudget.Models;

namespace MyBudget.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _db;

        async Task Init()
        {
            if (_db != null) return;
            var databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyBudget.db3");
            _db = new SQLiteAsyncConnection(databasePath);

            await _db.CreateTableAsync<Expense>();
            await _db.CreateTableAsync<BudgetSettings>();

            var settings = await GetBudgetSettingsAsync();
            if (settings == null)
            {
                await _db.InsertAsync(new BudgetSettings { Period = BudgetPeriod.Monthly, Amount = 1000m });
            }
        }

        public async Task<List<Expense>> GetExpensesAsync()
        {
            await Init();
            return await _db.Table<Expense>().OrderByDescending(e => e.Date).ToListAsync();
        }

        public async Task<int> AddExpenseAsync(Expense expense)
        {
            await Init();
            return await _db.InsertAsync(expense);
        }

        public async Task<int> DeleteExpenseAsync(string id)
        {
            await Init();
            return await _db.DeleteAsync<Expense>(id);
        }

        public async Task<BudgetSettings> GetBudgetSettingsAsync()
        {
            await Init();
            return await _db.Table<BudgetSettings>().FirstOrDefaultAsync();
        }

        public async Task<int> UpdateBudgetSettingsAsync(BudgetSettings settings)
        {
            await Init();
            return await _db.UpdateAsync(settings);
        }
        public Task<int> DeleteExpenseAsync(Expense expense)
        {
            return _db.DeleteAsync(expense);
        }
        public Task<int> UpdateExpenseAsync(Expense expense)
        {
            return _db.UpdateAsync(expense);
        }
    }
}