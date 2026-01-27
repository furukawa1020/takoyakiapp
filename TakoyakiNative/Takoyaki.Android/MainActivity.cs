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
        private Android.Widget.RelativeLayout _uiOverlay;
        private Android.Widget.TextView _scoreText;
        private Android.Widget.TextView _commentText;
        private Android.Widget.Button _resetButton;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Hide System UI
            Window!.DecorView!.SystemUiVisibility = (StatusBarVisibility)(
                SystemUiFlags.ImmersiveSticky | 
                SystemUiFlags.HideNavigation | 
                SystemUiFlags.Fullscreen);

            // Root Layout
            var rootLayout = new Android.Widget.FrameLayout(this);
            
            // 1. Game View
            _surfaceView = new TakoyakiSurfaceView(this);
            _surfaceView.GameFinished += OnGameFinished;
            rootLayout.AddView(_surfaceView);

            // 2. UI Overlay
            _uiOverlay = new Android.Widget.RelativeLayout(this);
            _uiOverlay.Visibility = ViewStates.Gone; // Hidden initially
            _uiOverlay.SetBackgroundColor(Android.Graphics.Color.Argb(200, 0, 0, 0)); // Semi-transparent black

            // Container for center content
            var centerLayout = new Android.Widget.LinearLayout(this);
            centerLayout.Orientation = Android.Widget.Orientation.Vertical;
            centerLayout.Gravity = GravityFlags.Center;
            
            var centerParams = new Android.Widget.RelativeLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            centerParams.AddRule(LayoutRules.CenterInParent);
            
            // Score Text
            _scoreText = new Android.Widget.TextView(this);
            _scoreText.TextSize = 48;
            _scoreText.SetTextColor(Android.Graphics.Color.White);
            _scoreText.Gravity = GravityFlags.Center;
            centerLayout.AddView(_scoreText);

            // Comment Text
            _commentText = new Android.Widget.TextView(this);
            _commentText.TextSize = 24;
            _commentText.SetTextColor(Android.Graphics.Color.Yellow);
            _commentText.Gravity = GravityFlags.Center;
            centerLayout.AddView(_commentText);

            // Reset Button
            _resetButton = new Android.Widget.Button(this);
            _resetButton.Text = "焼く"; // "Grill Another"
            _resetButton.Click += (s, e) => RestartGame();
            centerLayout.AddView(_resetButton);

            _uiOverlay.AddView(centerLayout, centerParams);
            rootLayout.AddView(_uiOverlay);

            SetContentView(rootLayout);
        }

        private void OnGameFinished(int score)
        {
            RunOnUiThread(() =>
            {
                _scoreText.Text = $"{score}点";
                
                string comment = "";
                if (score == 100) comment = "神の領域 (Godlike)";
                else if (score >= 80) comment = "匠の技 (Master)";
                else if (score >= 50) comment = "修行あるのみ (Apprentice)";
                else comment = "焼き直し (Burnt)";
                
                _commentText.Text = comment;
                
                _uiOverlay.Visibility = ViewStates.Visible;
            });
        }

        private void RestartGame()
        {
            _uiOverlay.Visibility = ViewStates.Gone;
            _surfaceView.ResetGame();
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
