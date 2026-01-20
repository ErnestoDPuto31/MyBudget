using Android.App;
using Android.Views;
using Android.Widget;
using AndroidApp1;
using System.Collections.Generic;

public class ExpenseAdapter : BaseAdapter<Expense>
{
    List<Expense> items;
    Activity context;

    public ExpenseAdapter(Activity context, List<Expense> items)
    {
        this.context = context;
        this.items = items;
    }

    public override Expense this[int position] => items[position];
    public override int Count => items.Count;
    public override long GetItemId(int position) => position;

    public override View GetView(int position, View convertView, ViewGroup parent)
    {
        var item = items[position];
        var view = convertView ?? context.LayoutInflater.Inflate(Resource.Layout.expense_item, null);

        // Get references to your UI
        var titleTxt = view.FindViewById<TextView>(Resource.Id.textTitle);
        var categoryTxt = view.FindViewById<TextView>(Resource.Id.textCategory);
        var dateTxt = view.FindViewById<TextView>(Resource.Id.textDate);
        var amountTxt = view.FindViewById<TextView>(Resource.Id.textAmount);
        var rowContainer = (LinearLayout)view;

        rowContainer.SetBackgroundColor(Android.Graphics.Color.Transparent);
        titleTxt.SetTextColor(Android.Graphics.Color.ParseColor("#212121"));
        amountTxt.SetTextColor(Android.Graphics.Color.ParseColor("#212121"));

        if (item.Category == "ESSENTIAL")
        {
            if (item.Description.Contains("[MONTHLY]"))
            {
                rowContainer.SetBackgroundColor(Android.Graphics.Color.ParseColor("#E1F5FE"));
                titleTxt.SetTextColor(Android.Graphics.Color.ParseColor("#0277BD"));
                amountTxt.SetTextColor(Android.Graphics.Color.ParseColor("#0277BD"));
            }
            else if (item.Description.Contains("[WEEKLY]"))
            {
                rowContainer.SetBackgroundColor(Android.Graphics.Color.ParseColor("#F3E5F5"));
                titleTxt.SetTextColor(Android.Graphics.Color.ParseColor("#7B1FA2"));
                amountTxt.SetTextColor(Android.Graphics.Color.ParseColor("#7B1FA2"));
            }
            titleTxt.Text = " " + item.Description;
        }
        else
        {
            titleTxt.Text = !string.IsNullOrWhiteSpace(item.Description) ? item.Description : item.Category;
        }

        categoryTxt.Text = item.Category.ToUpper();
        dateTxt.Text = !string.IsNullOrEmpty(item.Date) ? item.Date : "No date recorded";
        amountTxt.Text = $"₱{item.Amount:F2}";

        return view;
    }

    public void RemoveItem(int position)
    {
        items.RemoveAt(position);
        NotifyDataSetChanged();
    }

    public void UpdateItem(int position, Expense updated)
    {
        items[position] = updated;
        NotifyDataSetChanged();
    }
}