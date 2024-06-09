using System;
using UnityEngine;

namespace UnityEcho.Utils
{
    public class ScreenshotScript : MonoBehaviour
    {
        [ContextMenu("Capture")]
        private void TakeScreenshot()
        {
            ScreenCapture.CaptureScreenshot("Screenshots/" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".png");
        }
    }
}