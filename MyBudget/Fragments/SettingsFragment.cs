
using Android.Views;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using MyBudget.Models;
using MyBudget.Services;

namespace MyBudget.Fragments
{
    public class SettingsFragment : AndroidX.Fragment.App.Fragment
    {
        private RadioGroup rgBudgetPeriod;
        private RadioButton rbWeekly, rbMonthly;
        private TextInputEditText etBudgetAmount;
        private MaterialButton btnSaveSettings;

        private DatabaseService _dbService;
        private BudgetSettings _currentSettings;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(MyBudget.Resource.Layout.fragment_settings, container, false);

            _dbService = new DatabaseService();

            rgBudgetPeriod = view.FindViewById<RadioGroup>(MyBudget.Resource.Id.rgBudgetPeriod);
            rbWeekly = view.FindViewById<RadioButton>(MyBudget.Resource.Id.rbWeekly);
            rbMonthly = view.FindViewById<RadioButton>(MyBudget.Resource.Id.rbMonthly);
            etBudgetAmount = view.FindViewById<TextInputEditText>(MyBudget.Resource.Id.etBudgetAmount);
            btnSaveSettings = view.FindViewById<MaterialButton>(MyBudget.Resource.Id.btnSaveSettings);

            btnSaveSettings.Click += async (sender, e) => await SaveSettingsAsync();

            return view;
        }

        public override async void OnResume()
        {
            base.OnResume();
            await LoadCurrentSettingsAsync();
        }

        private async Task LoadCurrentSettingsAsync()
        {
            _currentSettings = await _dbService.GetBudgetSettingsAsync();

            RequireActivity().RunOnUiThread(() =>
            {
                if (_currentSettings.Period == BudgetPeriod.Weekly)
                {
                    rbWeekly.Checked = true;
                }
                else
                {
                    rbMonthly.Checked = true;
                }

                etBudgetAmount.Text = _currentSettings.Amount.ToString("0.00");
            });
        }

        private async Task SaveSettingsAsync()
        {
            if (!decimal.TryParse(etBudgetAmount.Text, out decimal newAmount) || newAmount <= 0)
            {
                Toast.MakeText(RequireActivity(), "Please enter a valid budget amount", ToastLength.Short).Show();
                return;
            }

            BudgetPeriod newPeriod = rbWeekly.Checked ? BudgetPeriod.Weekly : BudgetPeriod.Monthly;
            _currentSettings.Amount = newAmount;
            _currentSettings.Period = newPeriod;
            await _dbService.UpdateBudgetSettingsAsync(_currentSettings);

            Toast.MakeText(RequireActivity(), "Budget settings saved successfully!", ToastLength.Short).Show();
        }
    }
}