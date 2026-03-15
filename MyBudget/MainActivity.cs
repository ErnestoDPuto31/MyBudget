using AndroidX.AppCompat.App;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.Navigation;
using MyBudget.Fragments;

namespace MyBudget
{
    [Activity(Label = "@string/app_name", Theme = "@style/Theme.MyBudget", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private BottomNavigationView bottomNavigation;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            try
            {
                base.OnCreate(savedInstanceState);
                SQLitePCL.Batteries_V2.Init();

                SetContentView(MyBudget.Resource.Layout.activity_main);

                bottomNavigation = FindViewById<BottomNavigationView>(MyBudget.Resource.Id.bottomNavigation);
                bottomNavigation.ItemSelected += BottomNavigation_ItemSelected;

                if (savedInstanceState == null)
                {
                    LoadFragment(new DashboardFragment());
                }
            }
            catch (System.Exception ex)
            {
                Android.Widget.Toast.MakeText(this, $"STARTUP CRASH: {ex.Message}", Android.Widget.ToastLength.Long).Show();

                System.Diagnostics.Debug.WriteLine("=====================================");
                System.Diagnostics.Debug.WriteLine("CRASH REASON: " + ex.ToString());
                System.Diagnostics.Debug.WriteLine("=====================================");
            }
        }
        private void BottomNavigation_ItemSelected(object sender, NavigationBarView.ItemSelectedEventArgs e)
        {
            AndroidX.Fragment.App.Fragment selectedFragment = null;

            if (e.Item.ItemId == MyBudget.Resource.Id.navigation_home)
            {
                selectedFragment = new DashboardFragment();
            }
            else if (e.Item.ItemId == MyBudget.Resource.Id.navigation_expenses)
            {
                selectedFragment = new ExpensesFragment();
            }
            else if (e.Item.ItemId == MyBudget.Resource.Id.navigation_statistics)
            {
                selectedFragment = new StatisticsFragment();
            }
            else if (e.Item.ItemId == MyBudget.Resource.Id.navigation_settings)
            {
                selectedFragment = new SettingsFragment();
            }

            if (selectedFragment != null)
            {
                LoadFragment(selectedFragment);
                e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
        }

        private void LoadFragment(AndroidX.Fragment.App.Fragment fragment)
        {
            SupportFragmentManager.BeginTransaction()
                .Replace(MyBudget.Resource.Id.fragmentContainer, fragment)
                .Commit();
        }
    }
}