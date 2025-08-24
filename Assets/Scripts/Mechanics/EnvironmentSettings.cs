using UnityEngine.Rendering;

namespace UnityEcho.Mechanics
{
    public class EnvironmentSettings : VolumeComponent
    {
        public ClampedFloatParameter atmosphere = new(0, 0, 1);
    }
}