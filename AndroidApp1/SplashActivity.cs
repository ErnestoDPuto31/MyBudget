using Android.App;
using Android.Content;
using Android.OS;

namespace AndroidApp1
{
    [Activity(Label = "MyBudget", Theme = "@style/Theme.MyBudget.Splash", MainLauncher = true, NoHistory = true)]
    public class SplashActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.splash_layout);
            var content = FindViewById<LinearLayout>(Resource.Id.main_container); 
            content.Alpha = 0f;
            content.Animate().Alpha(1f).SetDuration(800).Start();

            // Delay for 2 seconds, then start MainActivity
            new Handler().PostDelayed(() =>
            {
                StartActivity(new Intent(this, typeof(MainActivity)));
                Finish();
            }, 2000);
        }
    }
}
