// ====================================================
// 카메라 컨트롤러
// 블록 쌓일 때마다 목표 높이(targetHeight) 받아서 Lerp로 따라 올라감
// 퍼펙트 칠 때 Shake() 호출되면 잠깐 좌우로 흔들림
// ====================================================
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance;

    [Header("Follow")]
    public float followEase = 0.08f;
    private float targetHeight = 4f;

    [Header("Shake")]
    private float shakeTime = 0f;
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0f;
    private Vector3 basePos;

    void Awake()
    {
        Instance = this;
        basePos = transform.localPosition;
    }

    void LateUpdate()
    {
        // 높이 추적 (Lerp)
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, targetHeight, followEase);

        // 흔들림 적용
        Vector3 shakeOffset = Vector3.zero;
        if (shakeTime > 0f)
        {
            shakeTime -= Time.unscaledDeltaTime;
            float strength = (shakeTime / shakeDuration) * shakeMagnitude;
            shakeOffset = new Vector3(
                Mathf.Sin(shakeTime * 50f) * strength,
                0f,
                0f
            );
        }

        transform.position = pos + shakeOffset;
    }

    public void SetTargetHeight(float height)
    {
        targetHeight = height;
    }

    // duration: 초, magnitude: 흔들림 강도
    public void Shake(float magnitude, float framesEquivalent)
    {
        shakeMagnitude = magnitude;
        shakeDuration = framesEquivalent / 60f; // 프레임 기준을 초로 환산
        shakeTime = shakeDuration;
    }
}