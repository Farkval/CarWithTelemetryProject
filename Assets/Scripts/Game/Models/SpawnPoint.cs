using UnityEngine;

namespace Assets.Scripts.Game.Models
{
    public class SpawnPoint
    {
        public readonly Vector3 SpawnPosition;
        public readonly Quaternion SpawnRotation;

        public bool PlayerSpawned { get; private set; }
        public string PlayerName { get; private set; }

        public SpawnPoint(Vector3 pos, Quaternion rot)
        {
            SpawnPosition = pos;
            SpawnRotation = rot;
        }

        public void OnPlayerSpawned(string playerName)
        {
            PlayerSpawned = true;
            PlayerName = playerName;
        }

        public void ClearSpawn()
        {
            PlayerSpawned = false;
            PlayerName = null;
        }
    }
}
