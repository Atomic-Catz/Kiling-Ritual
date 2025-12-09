using System.Collections;
using UnityEngine;

namespace InfimaGames.LowPolyShooterPack
{
    public class CamoBuff : MonoBehaviour
    {
        bool active = false;
        Coroutine expireCoroutine;

        public void Grant(float duration = 5f)
        {
            if (expireCoroutine != null)
                StopCoroutine(expireCoroutine);

            active = true;
            expireCoroutine = StartCoroutine(ExpireAfter(duration));
        }

        public bool IsActive() => active;

        public void Clear()
        {
            if (expireCoroutine != null)
            {
                StopCoroutine(expireCoroutine);
                expireCoroutine = null;
            }
            active = false;
        }

        IEnumerator ExpireAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            active = false;
            expireCoroutine = null;
        }
    }
}
