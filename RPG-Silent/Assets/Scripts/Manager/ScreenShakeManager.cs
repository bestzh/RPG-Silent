using UnityEngine;

public class ScreenShakeManager : MonoBehaviour
{
    public static ScreenShakeManager Instance;

    private static float _intensityScale = 1f;

    private Transform cameraTransform;
    private Vector3 originalPos;
    private float shakeDuration = 0f;
    private float shakeAmount = 0.2f;
    private float decreaseFactor = 1.5f;

    public static void SetIntensityScale(float scale) =>
        _intensityScale = Mathf.Clamp01(scale);

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        cameraTransform = Camera.main.transform;
        originalPos = cameraTransform.localPosition;
    }

    private void Update()
    {
        if (shakeDuration > 0)
        {
            cameraTransform.localPosition = originalPos + Random.insideUnitSphere * shakeAmount;
            shakeDuration -= Time.deltaTime * decreaseFactor;
        }
        else
        {
            shakeDuration = 0f;
            cameraTransform.localPosition = originalPos;
        }
    }

    public void Shake(float amount, float duration)
    {
        if (_intensityScale <= 0f) return;

        shakeAmount = amount * _intensityScale;
        shakeDuration = duration;
    }
}
