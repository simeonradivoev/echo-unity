using UnityEngine.EventSystems;

namespace UnityEcho.UI.XR
{
    public class XREventSystem : EventSystem
    {
        #region Overrides of EventSystem

        protected override void Update()
        {
        }

        #endregion

        private void LateUpdate()
        {
            base.Update();
        }
    }
}