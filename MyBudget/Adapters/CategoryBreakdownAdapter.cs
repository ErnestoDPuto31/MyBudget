using Android.Views;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Card;
using MyBudget.Fragments;
using Android.Graphics;

namespace MyBudget.Adapters
{
    public class CategoryBreakdownAdapter : RecyclerView.Adapter
    {
        public List<StatisticsFragment.CategorySummary> Summaries { get; private set; }

        public CategoryBreakdownAdapter(List<StatisticsFragment.CategorySummary> summaries)
        {
            Summaries = summaries;
        }

        public void UpdateData(List<StatisticsFragment.CategorySummary> newSummaries)
        {
            Summaries = newSummaries;
            NotifyDataSetChanged();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.item_expense, parent, false);
            return new SummaryViewHolder(itemView);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is SummaryViewHolder vh)
            {
                var summary = Summaries[position];

                vh.ExpenseName.Text = summary.CategoryName;
                vh.ExpenseDateCategory.Text = $"{summary.PercentageOfTotal:F1}% of total spending";
                vh.ExpenseAmount.Text = $"₱{summary.TotalAmount:N2}";
                SetCategoryStyle(vh, summary.CategoryName);
            }
        }

        public override int ItemCount => Summaries?.Count ?? 0;

        private void SetCategoryStyle(SummaryViewHolder vh, string category)
        {
            string bgColorHex;
            string iconTintHex;
            int iconResId;

            switch (category)
            {
                case "Food & Dining":
                    bgColorHex = "#FEF3C7"; iconTintHex = "#D97706"; iconResId = Android.Resource.Drawable.IcMenuMyPlaces; break;
                case "Transportation":
                    bgColorHex = "#DBEAFE"; iconTintHex = "#2563EB"; iconResId = Android.Resource.Drawable.IcMenuDirections; break;
                case "Shopping":
                    bgColorHex = "#F3E8FF"; iconTintHex = "#9333EA"; iconResId = Android.Resource.Drawable.IcMenuSortBySize; break;
                case "Bills & Utilities":
                    bgColorHex = "#FEE2E2"; iconTintHex = "#DC2626"; iconResId = Android.Resource.Drawable.IcMenuRecentHistory; break;
                default:
                    bgColorHex = "#F3F4F6"; iconTintHex = "#4B5563"; iconResId = Android.Resource.Drawable.IcMenuAgenda; break;
            }

            vh.CardCategoryIcon.SetCardBackgroundColor(Color.ParseColor(bgColorHex));
            vh.IconCategory.SetImageResource(iconResId);
            vh.IconCategory.SetColorFilter(Color.ParseColor(iconTintHex));
        }

        public class SummaryViewHolder : RecyclerView.ViewHolder
        {
            public MaterialCardView CardCategoryIcon { get; }
            public ImageView IconCategory { get; }
            public TextView ExpenseName { get; }
            public TextView ExpenseDateCategory { get; }
            public TextView ExpenseAmount { get; }

            public SummaryViewHolder(View itemView) : base(itemView)
            {
                CardCategoryIcon = itemView.FindViewById<MaterialCardView>(Resource.Id.cardCategoryIcon);
                IconCategory = itemView.FindViewById<ImageView>(Resource.Id.ivCategoryIcon);
                ExpenseName = itemView.FindViewById<TextView>(Resource.Id.tvExpenseName);
                ExpenseDateCategory = itemView.FindViewById<TextView>(Resource.Id.tvExpenseDateCategory);
                ExpenseAmount = itemView.FindViewById<TextView>(Resource.Id.tvExpenseAmount);
            }
        }
    }
}