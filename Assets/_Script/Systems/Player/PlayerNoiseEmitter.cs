// ============================================================
// PlayerNoiseEmitter.cs — Bailiff & Co  V2
// All player noise emission routes through here → EventBus.
// Throttled by level to prevent event spam.
// ============================================================
using UnityEngine;

public class PlayerNoiseEmitter : MonoBehaviour
{
    private float _lastLightNoiseTime = 0f;
    private float _lastLoudNoiseTime = 0f;

    private const float THROTTLE_LIGHT = 0.3f;  // Light footsteps — 1 event / 300ms
    private const float THROTTLE_LOUD = 0.1f;   // Sprint/impact — 1 event / 100ms

    public void EmitNoise(NoiseLevel level, float range)
    {
        if (level == NoiseLevel.Silent) return;

        float now = Time.time;

        if (level == NoiseLevel.Light)
        {
            if (now - _lastLightNoiseTime < THROTTLE_LIGHT) return;
            _lastLightNoiseTime = now;
        }
        else // Loud or VeryLoud
        {
            if (now - _lastLoudNoiseTime < THROTTLE_LOUD) return;
            _lastLoudNoiseTime = now;
        }

        EventBus<OnNoiseEmitted>.Raise(new OnNoiseEmitted
        {
            Position = transform.position,
            Range = range,
            Level = level,
            Source = gameObject
        });
    }

    /// <summary>Landing noise after fall — range proportional to fall velocity.</summary>
    public void EmitLandingNoise(float fallSpeedAbsolute)
    {
        if (fallSpeedAbsolute < 2f) return;

        NoiseLevel level = fallSpeedAbsolute < 5f ? NoiseLevel.Light : NoiseLevel.Loud;
        float range = Mathf.Clamp(fallSpeedAbsolute * 0.8f, 3f, 15f);
        EmitNoise(level, range);
    }
}
