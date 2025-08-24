using UnityEngine;

namespace Demo.UI
{
    public class DebugUIController : MonoBehaviour
    {
        public void SpawnTestObject(GameObject prefab)
        {
            var camera = Camera.main;
            var ray = new Ray(camera.transform.position, camera.transform.forward);
            var hadHit = Physics.Raycast(ray, out var hit);
            var distance = 1f;
            if (hadHit)
            {
                distance = Mathf.Min(distance, hit.distance);
            }
            Instantiate(prefab, ray.GetPoint(distance), Quaternion.identity);
        }
    }
}