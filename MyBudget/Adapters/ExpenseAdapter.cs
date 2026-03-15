using Android.Views;
using AndroidX.RecyclerView.Widget;
using MyBudget.Models;

namespace MyBudget.Adapters
{
    public class ExpenseAdapter : RecyclerView.Adapter
    {
        public List<Expense> Expenses { get; private set; }
        public event EventHandler<(Expense expense, View view)> ItemClick;

        public ExpenseAdapter(List<Expense> expenses)
        {
            Expenses = expenses;
        }

        public override int ItemCount => Expenses.Count;

        public void UpdateData(List<Expense> newExpenses)
        {
            Expenses = newExpenses;
            NotifyDataSetChanged(); 
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(MyBudget.Resource.Layout.item_expense, parent, false);
            return new ExpenseViewHolder(itemView);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is ExpenseViewHolder vh)
            {
                var expense = Expenses[position];

                vh.TvName.Text = expense.Name;
                vh.TvCategory.Text = expense.Category;
                vh.TvDate.Text = expense.Date.ToString("MMM d, yyyy - h:mm tt");

                vh.TvAmount.Text = $"₱{expense.Amount:N2}";
                vh.ItemView.Click += (s, e) => ItemClick?.Invoke(this, (expense, vh.ItemView));
            }
        }
    }

    public class ExpenseViewHolder : RecyclerView.ViewHolder
    {
        public TextView TvName { get; private set; }
        public TextView TvCategory { get; private set; }
        public TextView TvDate { get; private set; }
        public TextView TvAmount { get; private set; }

        public ExpenseViewHolder(View itemView) : base(itemView)
        {
            TvName = itemView.FindViewById<TextView>(MyBudget.Resource.Id.tvExpenseName);
            TvCategory = itemView.FindViewById<TextView>(MyBudget.Resource.Id.tvExpenseCategory);
            TvDate = itemView.FindViewById<TextView>(MyBudget.Resource.Id.tvExpenseDate);
            TvAmount = itemView.FindViewById<TextView>(MyBudget.Resource.Id.tvExpenseAmount);
        }
    }
}