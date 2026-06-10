// ============================================================
// HoldIK.cs — Bailiff & Co
// Hand IK partagé (Animation Rigging) pour TENIR un objet : outil en main
// (PlayerToolUser) OU objet de valeur porté (PlayerCarry).
//
// Colle les mains aux points de grip de l'objet :
//   - GripRight (+ GripLeft si 2 mains) sur le prefab de l'objet.
//   - Les cibles IK (Target des contraintes) suivent ces grips chaque frame
//     → restent sous HoldRig, donc PAS détruites quand l'objet l'est.
// À mettre sur le PrefabPlayer ; assigner HoldRig + les 2 contraintes.
// ============================================================
using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class HoldIK : MonoBehaviour
{
    [SerializeField] private Rig                 _holdRig;   // HoldRig (poids 0/1)
    [SerializeField] private TwoBoneIKConstraint _ikRight;   // IK_HandRight
    [SerializeField] private TwoBoneIKConstraint _ikLeft;    // IK_HandLeft

    private Transform        _gripR;
    private Transform        _gripL;
    private Func<GripPose>   _getR;     // offsets lus EN LIVE (réglables en playmode)
    private Func<GripPose>   _getL;

    /// <summary>Tient l'objet : mains collées à ses grips (1 ou 2 selon HandCount).
    /// `getR`/`getL` renvoient l'offset par main, relus chaque frame (réglage live).</summary>
    public void Hold(Transform model, HandCount mains, Func<GripPose> getR, Func<GripPose> getL)
    {
        if (_holdRig == null || model == null) { Release(); return; }

        _gripR = Trouver(model, "GripRight");
        _gripL = mains == HandCount.TwoHand ? Trouver(model, "GripLeft") : null;
        _getR  = getR;
        _getL  = getL;

        bool okR = _gripR != null && _ikRight != null && _ikRight.data.target != null;
        bool okL = _gripL != null && _ikLeft  != null && _ikLeft.data.target  != null;

        if (_ikRight != null) _ikRight.weight = okR ? 1f : 0f;
        if (_ikLeft  != null) _ikLeft.weight  = okL ? 1f : 0f;
        _holdRig.weight = (okR || okL) ? 1f : 0f;
    }

    /// <summary>Relâche : IK désactivé.</summary>
    public void Release()
    {
        _gripR = _gripL = null;
        _getR  = _getL  = null;
        if (_ikRight != null) _ikRight.weight = 0f;
        if (_ikLeft  != null) _ikLeft.weight  = 0f;
        if (_holdRig != null) _holdRig.weight = 0f;
    }

    private void LateUpdate()
    {
        if (_gripR != null && _ikRight != null && _ikRight.data.target != null)
        {
            GripPose p = _getR != null ? _getR() : default;
            _ikRight.data.target.SetPositionAndRotation(
                _gripR.TransformPoint(p.PositionOffset),
                _gripR.rotation * Quaternion.Euler(p.EulerOffset));
        }
        if (_gripL != null && _ikLeft != null && _ikLeft.data.target != null)
        {
            GripPose p = _getL != null ? _getL() : default;
            _ikLeft.data.target.SetPositionAndRotation(
                _gripL.TransformPoint(p.PositionOffset),
                _gripL.rotation * Quaternion.Euler(p.EulerOffset));
        }
    }

    private static Transform Trouver(Transform root, string nom)
    {
        if (root.name == nom) return root;
        foreach (Transform c in root)
        {
            var r = Trouver(c, nom);
            if (r != null) return r;
        }
        return null;
    }
}
