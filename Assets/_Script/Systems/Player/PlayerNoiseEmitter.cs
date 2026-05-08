// ============================================================
// PlayerNoiseEmitter.cs — Bailiff & Co  V2
// Toute émission sonore du joueur passe par ici → EventBus.
// Throttle par niveau pour éviter le spam d'events.
//
// CHANGEMENTS V2 :
//   - OnBruitEmis → OnNoiseEmitted (nouveau nom event)
//   - Inchangé structurellement — script déjà propre
// ============================================================
using UnityEngine;

public class PlayerNoiseEmitter : MonoBehaviour
{
    private float _dernierBruitLegerTime  = 0f;
    private float _dernierBruitFortTime   = 0f;

    private const float THROTTLE_LEGER = 0.3f;  // pas normaux — 1 event / 300ms
    private const float THROTTLE_FORT  = 0.1f;  // sprint / impact — 1 event / 100ms

    public void EmettreBruit(NiveauBruit niveau, float portee)
    {
        if (niveau == NiveauBruit.Silencieux) return;

        float now = Time.time;

        if (niveau == NiveauBruit.Leger)
        {
            if (now - _dernierBruitLegerTime < THROTTLE_LEGER) return;
            _dernierBruitLegerTime = now;
        }
        else // Fort ou Tresfort
        {
            if (now - _dernierBruitFortTime < THROTTLE_FORT) return;
            _dernierBruitFortTime = now;
        }

        EventBus<OnNoiseEmitted>.Raise(new OnNoiseEmitted
        {
            Position = transform.position,
            Range    = portee,
            Level    = niveau,
            Source   = gameObject
        });
    }

    /// <summary>
    /// Bruit d'atterrissage après un saut — portée proportionnelle à la vélocité.
    /// </summary>
    public void EmettreBruitAtterrissage(float vitesseChuteAbsolue)
    {
        // Chute légère < 4 m/s = bruit léger. Au-delà = fort.
        if (vitesseChuteAbsolue < 2f) return;

        NiveauBruit niveau = vitesseChuteAbsolue < 5f
            ? NiveauBruit.Leger
            : NiveauBruit.Fort;

        float portee = Mathf.Clamp(vitesseChuteAbsolue * 0.8f, 3f, 15f);
        EmettreBruit(niveau, portee);
    }
}