// ============================================================
// ShopConsoCard.cs — binder d'une carte consommable du shop
// À mettre sur le prefab carte conso (_carteConsoPrefab).
// ============================================================
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopConsoCard : MonoBehaviour
{
    [SerializeField] private TMP_Text _nom, _meta, _possedes, _qty, _total, _boutonLabel;
    [SerializeField] private Button _moins, _plus, _acheter;
    [SerializeField] private GameObject _lockOverlay;

    public void Bind(OutilData o, int qty, bool locked, InventaireSystem inv, Action<int> onStep, Action onBuy)
    {
        if (_nom) _nom.text = o.ToolName;
        if (_lockOverlay) _lockOverlay.SetActive(locked);

        if (locked)
        {
            if (_meta) _meta.text = "🔒 Mission " + o.UnlocksAfterMission;
            if (_moins) _moins.interactable = false;
            if (_plus) _plus.interactable = false;
            if (_acheter) _acheter.interactable = false;
            return;
        }

        int total = o.PurchasePrice * qty;
        bool ok = (GameManager.Instance?.Argent ?? 0) >= total;

        if (_meta)     _meta.text     = o.PurchasePrice + " € / unité · max " + o.MaxCarryPerMission + "/mission";
        if (_possedes) _possedes.text = "possédés : " + (inv != null ? inv.QuantiteConsommable(o.ToolName) : 0);
        if (_qty)      _qty.text      = qty.ToString();
        if (_total)    _total.text    = "total : " + total + " €";
        if (_boutonLabel) _boutonLabel.text = ok ? "Acheter ×" + qty : "Fonds insuffisants";

        if (_moins)
        {
            _moins.onClick.RemoveAllListeners();
            _moins.interactable = qty > 1;
            _moins.onClick.AddListener(() => onStep(-1));
        }
        if (_plus)
        {
            _plus.onClick.RemoveAllListeners();
            _plus.interactable = qty < 10;
            _plus.onClick.AddListener(() => onStep(1));
        }
        if (_acheter)
        {
            _acheter.onClick.RemoveAllListeners();
            _acheter.interactable = ok;
            _acheter.onClick.AddListener(() => onBuy());
        }
    }
}
