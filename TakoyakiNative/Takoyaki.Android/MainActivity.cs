using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;

namespace Takoyaki.Android
{
    [Activity(Label = "@string/app_name", 
              Name = "com.hatake.takoyaki.soul.MainActivity",
              MainLauncher = true, 
              Theme = "@style/Theme.AppCompat.NoActionBar",
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize,
              ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : AppCompatActivity
    {
        private TakoyakiSurfaceView _surfaceView;
        private RelativeLayout _uiOverlay;
        private TextView _scoreText;
        private TextView _commentText;
        private Button _resetButton;

        public override bool DispatchKeyEvent(KeyEvent? e)
        {
            if (e != null && _surfaceView.HandleKeyEvent(e)) return true;
            return base.DispatchKeyEvent(e);
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            try
            {
                global::Android.Util.Log.Error("TakoyakiCrash", "STARTING ONCREATE");
                base.OnCreate(savedInstanceState);
                // Hide System UI
                Window!.DecorView!.SystemUiVisibility = (StatusBarVisibility)(
                    SystemUiFlags.ImmersiveSticky | 
                    SystemUiFlags.HideNavigation | 
                    SystemUiFlags.Fullscreen);

                // Root Layout
                var rootLayout = new global::Android.Widget.FrameLayout(this);
                
                // 1. Game View
                _surfaceView = new TakoyakiSurfaceView(this);
                _surfaceView.GameFinished += OnGameFinished;
                rootLayout.AddView(_surfaceView);

                // 2. UI Overlay
                _uiOverlay = new global::Android.Widget.RelativeLayout(this);
                _uiOverlay.Visibility = ViewStates.Gone; // Hidden initially
                _uiOverlay.SetBackgroundColor(global::Android.Graphics.Color.Argb(200, 0, 0, 0)); // Semi-transparent black

                // Container for center content
                var centerLayout = new global::Android.Widget.LinearLayout(this);
                centerLayout.Orientation = global::Android.Widget.Orientation.Vertical;
                centerLayout.SetGravity(GravityFlags.Center);
                
                var centerParams = new global::Android.Widget.RelativeLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
                centerParams.AddRule(LayoutRules.CenterInParent);
                
                // Score Text
                _scoreText = new global::Android.Widget.TextView(this);
                _scoreText.TextSize = 48;
                _scoreText.SetTextColor(global::Android.Graphics.Color.White);
                _scoreText.Gravity = GravityFlags.Center;
                centerLayout.AddView(_scoreText);

                // Comment Text
                _commentText = new global::Android.Widget.TextView(this);
                _commentText.TextSize = 24;
                _commentText.SetTextColor(global::Android.Graphics.Color.Yellow);
                _commentText.Gravity = GravityFlags.Center;
                centerLayout.AddView(_commentText);

                // Reset Button
                _resetButton = new global::Android.Widget.Button(this);
                _resetButton.Text = "焼く"; // "Grill Another"
                _resetButton.Click += (s, e) => RestartGame();
                centerLayout.AddView(_resetButton);
                
                // Share Button
                var shareButton = new global::Android.Widget.Button(this);
                shareButton.Text = "シェア";
                shareButton.Click += (s, e) => ShareScreenshot();
                centerLayout.AddView(shareButton);

                _uiOverlay.AddView(centerLayout, centerParams);
                rootLayout.AddView(_uiOverlay);

                // 3. Debug Control Buttons (Top of screen)
                var debugButtonLayout = new global::Android.Widget.LinearLayout(this);
                debugButtonLayout.Orientation = global::Android.Widget.Orientation.Horizontal;
                debugButtonLayout.SetGravity(GravityFlags.Center);
                debugButtonLayout.SetBackgroundColor(global::Android.Graphics.Color.Argb(150, 0, 0, 0));
                debugButtonLayout.SetPadding(10, 10, 10, 10);

                var debugParams = new global::Android.Widget.FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                debugParams.Gravity = GravityFlags.Top;

                // Pour Button
                var pourButton = new global::Android.Widget.Button(this);
                pourButton.Text = "流し込む";
                pourButton.SetTextColor(global::Android.Graphics.Color.White);
                pourButton.Click += (s, e) => _surfaceView.EmulatePour(2.0f);
                debugButtonLayout.AddView(pourButton);

                // Flip Button
                var flipButton = new global::Android.Widget.Button(this);
                flipButton.Text = "ひっくり返す";
                flipButton.SetTextColor(global::Android.Graphics.Color.White);
                flipButton.Click += (s, e) => _surfaceView.EmulateFlip();
                debugButtonLayout.AddView(flipButton);

                // Serve Button
                var serveButton = new global::Android.Widget.Button(this);
                serveButton.Text = "サーブ";
                serveButton.SetTextColor(global::Android.Graphics.Color.White);
                serveButton.Click += (s, e) => _surfaceView.EmulateServe(1.0f);
                debugButtonLayout.AddView(serveButton);

                rootLayout.AddView(debugButtonLayout, debugParams);

                SetContentView(rootLayout);
            }
            catch (System.Exception ex)
            {
                global::Android.Util.Log.Error("TakoyakiCrash", $"CRASH IN ONCREATE: {ex}");
                throw;
            }
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

        private void ShareScreenshot()
        {
            _surfaceView.CaptureScreenshot((bitmap) => {
                try
                {
                    // Save Bitmap to Cache Dir
                    var cachePath = new Java.IO.File(CacheDir, "images");
                    cachePath.Mkdirs(); 
                    var filePath = new Java.IO.File(cachePath, "takoyaki_share.png");
                    var stream = new System.IO.FileStream(filePath.AbsolutePath, System.IO.FileMode.Create);
                    bitmap.Compress(global::Android.Graphics.Bitmap.CompressFormat.Png, 100, stream);
                    stream.Close();

                    // Create URI
                    var contentUri = AndroidX.Core.Content.FileProvider.GetUriForFile(
                        this, "com.hatake.takoyaki.soul.fileprovider", filePath);

                    if (contentUri != null)
                    {
                        var shareIntent = new global::Android.Content.Intent();
                        shareIntent.SetAction(global::Android.Content.Intent.ActionSend);
                        shareIntent.AddFlags(global::Android.Content.ActivityFlags.GrantReadUriPermission); 
                        shareIntent.SetDataAndType(contentUri, ContentResolver.GetType(contentUri));
                        shareIntent.PutExtra(global::Android.Content.Intent.ExtraStream, contentUri);
                        shareIntent.SetType("image/png");
                        StartActivity(global::Android.Content.Intent.CreateChooser(shareIntent, "たこ焼きを共有"));
                    }
                }
                catch (System.Exception ex)
                {
                    global::Android.Util.Log.Error("TakoyakiShare", $"Share failed: {ex}");
                }
            });
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

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _surfaceView?.Dispose();
        }
    }
}
