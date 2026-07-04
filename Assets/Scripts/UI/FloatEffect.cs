using UnityEngine;

public class FloatEffect : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.05f;
    [SerializeField] private float frequency = 1.2f;

    private Vector3 startPos;

    private void Awake()
    {
        startPos = transform.localPosition;
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * frequency) * amplitude;
        transform.localPosition = startPos + new Vector3(0f, offset, 0f);
    }
}
