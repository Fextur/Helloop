using UnityEngine;
using Helloop.Player;

namespace Helloop.Audio
{
    public class AudioListenerFollower : MonoBehaviour
    {
        public Transform fpsCamera;
        public Transform tpsCamera;
        public PlayerMovement playerMovement;

        void Update()
        {
            if (playerMovement.isStealth)
                transform.position = tpsCamera.position;
            else
                transform.position = fpsCamera.position;

            transform.rotation = playerMovement.isStealth ? tpsCamera.rotation : fpsCamera.rotation;
        }
    }
}