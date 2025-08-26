using UnityEngine;
using Random = UnityEngine.Random;

namespace UnityEcho.Demo
{
    public class SpawnPointsManager : MonoBehaviour
    {
        private void Start()
        {
            var spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
            var spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            var player = GameObject.FindWithTag("Player").GetComponent<Rigidbody>();
            player.position = spawnPoint.transform.position;
            player.rotation = spawnPoint.transform.rotation;
        }
    }
}