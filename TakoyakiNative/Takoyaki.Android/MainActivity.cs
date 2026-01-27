using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.App;

namespace Takoyaki.Android
{
    [Activity(Label = "@string/app_name", 
              MainLauncher = true, 
              Theme = "@style/Theme.AppCompat.NoActionBar",
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize,
              ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : AppCompatActivity
    {
        private TakoyakiSurfaceView _surfaceView;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Hide System UI for Immersive Mode
            Window!.DecorView!.SystemUiVisibility = (StatusBarVisibility)(
                SystemUiFlags.ImmersiveSticky | 
                SystemUiFlags.HideNavigation | 
                SystemUiFlags.Fullscreen);

            // Init OpenGL View
            _surfaceView = new TakoyakiSurfaceView(this);
            SetContentView(_surfaceView);
        }

        protected override void OnPause()
        {
            base.OnPause();
            _surfaceView.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            _surfaceView.OnResume();
        }
    }
}
