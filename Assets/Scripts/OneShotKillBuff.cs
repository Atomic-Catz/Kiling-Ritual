using UnityEngine;
using System.Collections;

namespace InfimaGames.LowPolyShooterPack
{
    public class OneShotKillBuff : MonoBehaviour
    {
        [Header("Buff Settings")]
        [Tooltip("Duration in seconds the Insta-Kill buff lasts.")]
        public float duration = 10f;

        public bool IsActive { get; private set; }

        public void Activate()
        {
            if (IsActive)
                StopAllCoroutines();

            StartCoroutine(BuffRoutine());
        }

        private IEnumerator BuffRoutine()
        {
            IsActive = true;
            yield return new WaitForSeconds(duration);
            IsActive = false;
        }
    }
}