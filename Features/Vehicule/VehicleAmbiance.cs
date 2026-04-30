// ============================================================
// VehicleAmbiance.cs — Bailiff & Co  V2
// À mettre sur le prefab de chaque véhicule en mission.
// Joue aléatoirement un son spécial (braiment, musique...)
// dans la fourchette de temps définie dans VehiculeData.
//
// CHANGEMENTS V2 :
//   - VehiculeAmbiance → VehicleAmbiance (anglais cohérent)
//   - VehiculeDef → VehiculeData (nouveau nom SO)
//   - OnBruitEmis → OnNoiseEmitted
//   - Commentaires en anglais pour cohérence
//
// SETUP UNITY :
//   Prefab véhicule :
//   ├── VehicleRuntime.cs (root)
//   ├── VehicleAmbiance.cs  ← ce script
//   └── AudioSource          ← assigner dans _audioSource
//       (Spatial Blend = 1 pour audio 3D, Play On Awake = false)
// ============================================================
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class VehicleAmbiance : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("VehiculeData de ce véhicule — contient les clips et intervalles.")]
    [SerializeField] private VehiculeData _data;

    [Header("Références")]
    [SerializeField] private AudioSource _audioSource;

    // ================================================================
    // LIFECYCLE
    // ================================================================

    private void Awake()
    {
        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        // Ne démarre la coroutine que si des sons spéciaux sont définis
        if (_data == null) return;
        if (_data.SpecialSounds == null || _data.SpecialSounds.Length == 0) return;

        StartCoroutine(AmbianceLoop());
    }

    // ================================================================
    // COROUTINE PRINCIPALE
    // ================================================================

    private IEnumerator AmbianceLoop()
    {
        // Attente initiale aléatoire pour désynchroniser les véhicules
        // si plusieurs sont présents dans la mission
        float initialWait = Random.Range(
            _data.SpecialSoundIntervalMin * 0.5f,
            _data.SpecialSoundIntervalMax * 0.5f);
        yield return new WaitForSeconds(initialWait);

        while (true)
        {
            // Choisit un clip aléatoire parmi les sons spéciaux
            AudioClip clip = ChooseRandomClip();
            if (clip != null)
            {
                _audioSource.clip = clip;
                _audioSource.Play();

                // Émet un bruit pour que ProprietaireAI puisse réagir
                // (portée modérée — le son vient de la rue)
                EventBus<OnNoiseEmitted>.Raise(new OnNoiseEmitted
                {
                    Position = transform.position,
                    Range    = _data.SpecialSoundNoiseRange,
                    Level    = NiveauBruit.Fort,
                    Source   = gameObject
                });
            }

            // Attend un intervalle aléatoire avant le prochain son
            float wait = Random.Range(
                _data.SpecialSoundIntervalMin,
                _data.SpecialSoundIntervalMax);
            yield return new WaitForSeconds(wait);
        }
    }

    // ================================================================
    // UTILITAIRES
    // ================================================================

    private AudioClip ChooseRandomClip()
    {
        if (_data.SpecialSounds == null || _data.SpecialSounds.Length == 0)
            return null;

        int index = Random.Range(0, _data.SpecialSounds.Length);
        return _data.SpecialSounds[index];
    }

    // ================================================================
    // API PUBLIQUE
    // ================================================================

    /// <summary>
    /// Stoppe les sons spéciaux (ex : fin de mission, popup de départ).
    /// </summary>
    public void StopAmbiance()
    {
        StopAllCoroutines();
        if (_audioSource.isPlaying)
            _audioSource.Stop();
    }
}