using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    public class CharacterInstaKill : MonoBehaviour
    {
        public bool InstaKillActive { get; private set; }
        private float timer;

        private void Update()
        {
            if (InstaKillActive)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                    InstaKillActive = false;
            }
        }

        public void Activate(float duration)
        {
            InstaKillActive = true;
            timer = duration;

            Debug.Log("INSTA-KILL ACTIVATED!");
        }
    }
}