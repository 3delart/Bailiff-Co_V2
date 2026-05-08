// ============================================================
// EventBusHelper.cs — Bailiff & Co  V2
// NOUVEAU — Registre global de tous les EventBus<T> actifs.
// SceneLoader appelle ClearAll() avant chaque transition
// de scène → plus aucun handler de scène détruite ne reste
// abonné → zéro NullReferenceException.
//
// Fonctionnement :
//   - EventBus<T>.Subscribe() s'enregistre ici au 1er appel.
//   - SceneLoader.TransitionAvecFondu() appelle ClearAll()
//     juste avant le chargement de la nouvelle scène.
//   - Résultat : tous les bus sont vidés proprement.
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventBusHelper
{
    private static readonly List<Action> _clearCallbacks = new();

    /// <summary>
    /// Enregistre la méthode Clear() d'un EventBus<T>.
    /// Appelé automatiquement par EventBus<T> au premier Subscribe.
    /// </summary>
    public static void RegisterClear(Action callback)
    {
        if (!_clearCallbacks.Contains(callback))
            _clearCallbacks.Add(callback);
    }

    /// <summary>
    /// Vide TOUS les EventBus enregistrés.
    /// À appeler dans SceneLoader avant chaque chargement de scène.
    /// </summary>
    public static void ClearAll()
    {
        foreach (var cb in _clearCallbacks)
            cb?.Invoke();

        _clearCallbacks.Clear();
    }
}
