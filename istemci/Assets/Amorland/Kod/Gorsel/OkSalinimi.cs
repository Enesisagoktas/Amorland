using UnityEngine;

namespace Amorland.Gorsel
{
    /// <summary>Hedef okunun zipirtisi: taban konum cevresinde asagi-yukari.</summary>
    public class OkSalinimi : MonoBehaviour
    {
        public Vector3 Taban;
        private float _faz;

        private void Update()
        {
            _faz += Time.deltaTime * 4f;
            transform.position = Taban + new Vector3(0f, Mathf.Abs(Mathf.Sin(_faz)) * 0.18f, 0f);
        }
    }
}
