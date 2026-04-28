// ============================================================
// EventBus.cs — Bailiff & Co  V2
// Bus d'événements central. RÈGLE ABSOLUE : aucun système
// n'appelle directement un autre. Toute communication passe ici.
//
// NOUVEAUTÉ V2 : s'auto-enregistre dans EventBusHelper au
// premier Subscribe, pour que ClearAll() puisse tout nettoyer
// entre les transitions de scènes → zéro fuite mémoire.
//
// Usage :
//   EventBus<OnObjetCharge>.Raise(new OnObjetCharge { ... });
//   EventBus<OnObjetCharge>.Subscribe(MonHandler);
//   EventBus<OnObjetCharge>.Unsubscribe(MonHandler);
// ============================================================
using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventBus<T> where T : struct
{
    private static readonly List<Action<T>> _handlers = new();
    private static bool _registered = false;

    public static void Subscribe(Action<T> handler)
    {
        if (!_handlers.Contains(handler))
            _handlers.Add(handler);

        // S'enregistre une seule fois dans EventBusHelper
        if (!_registered)
        {
            EventBusHelper.RegisterClear(Clear);
            _registered = true;
        }
    }

    public static void Unsubscribe(Action<T> handler)
    {
        _handlers.Remove(handler);
    }

    public static void Raise(T evt)
    {
        // Itère en sens inverse pour gérer les Unsubscribe pendant l'itération
        for (int i = _handlers.Count - 1; i >= 0; i--)
            _handlers[i]?.Invoke(evt);
    }

    /// <summary>
    /// Vide la liste des handlers de CE type d'event.
    /// Appelé automatiquement par EventBusHelper.ClearAll()
    /// à chaque transition de scène.
    /// </summary>
    public static void Clear()
    {
        _handlers.Clear();
        _registered = false;
    }
}
