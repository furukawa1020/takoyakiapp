using UnityEngine;
using System.Collections;
using System.IO;

namespace TakoyakiPhysics.Meta
{
    public class ShareManager : MonoBehaviour
    {
        public void ShareScreenshot()
        {
            StartCoroutine(CaptureAndShare());
        }

        private IEnumerator CaptureAndShare()
        {
            yield return new WaitForEndOfFrame();

            string timestamp = System.DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileName = $"OneBallSoul_{timestamp}.png";
            string filePath = Path.Combine(Application.persistentDataPath, fileName);

            ScreenCapture.CaptureScreenshot(fileName);
            
            // Wait for file to be written
            yield return new WaitForSeconds(1.0f); 

            Debug.Log($"Screenshot saved to: {filePath}");

            // In a real build, we would use a Native Share plugin here
            // e.g. NativeShare.Share(filePath, "Check out my Takoyaki!");
            
            // For now, we simulate success
            Debug.Log("Share Dialog Opened (Simulated)");
        }
    }
}
