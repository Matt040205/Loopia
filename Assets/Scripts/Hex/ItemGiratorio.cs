using UnityEngine;

namespace Loopia.Hex
{
    public class ItemGiratorio : MonoBehaviour
    {
        public float grausPorSegundo = 120f;
        public float balanco = 0.08f;
        Vector3 _origem;
        void Start() => _origem = transform.localPosition;
        void Update()
        {
            transform.Rotate(Vector3.up, grausPorSegundo * Time.deltaTime, Space.Self);
            transform.localPosition = _origem + Vector3.up * (Mathf.Sin(Time.time * 2.5f) * balanco);
        }
    }
}
