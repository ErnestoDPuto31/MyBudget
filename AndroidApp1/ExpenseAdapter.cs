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
        var view = convertView ?? context.LayoutInflater.Inflate(Android.Resource.Layout.SimpleListItem2, null);

        view.FindViewById<TextView>(Android.Resource.Id.Text1).Text = $"{item.Description} - ₱{item.Amount}";
        view.FindViewById<TextView>(Android.Resource.Id.Text2).Text = $"Category: {item.Category}";

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
