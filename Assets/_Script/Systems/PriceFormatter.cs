// ============================================================
// PriceFormatter.cs — Bailiff & Co
// Classe utilitaire pour formater les prix de manière cohérente
// dans tout le projet.
//
// UTILISATION :
//   PriceFormatter.Format(1234.5f)  → "1 234.50 €"
//   PriceFormatter.Format(50f)      → "50.00 €"
//   PriceFormatter.Format(0f)       → "0.00 €"
//   PriceFormatter.Format(100)      → "100.00 €"
//   PriceFormatter.Format(5295.84f) → "5 295.84 €"
// ============================================================
using System.Globalization;
using UnityEngine;

public static class PriceFormatter
{
    private static readonly NumberFormatInfo PRICE_FORMAT = CreatePriceFormat();

    private static NumberFormatInfo CreatePriceFormat()
    {
        var nfi = (NumberFormatInfo)NumberFormatInfo.InvariantInfo.Clone();
        nfi.NumberGroupSeparator = " ";
        nfi.NumberGroupSizes = new[] { 3 };
        return nfi;
    }

    /// <summary>Formate un prix avec séparateurs de milliers et 2 décimales. Ex: 5295.84f → "5 295.84 €"</summary>
    public static string Format(float price) => price.ToString("N2", PRICE_FORMAT) + " €";

    /// <summary>Surcharge pour accepter les int</summary>
    public static string Format(int price) => Format((float)price);
}
