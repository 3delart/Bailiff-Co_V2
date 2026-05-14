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
using UnityEngine;

public static class PriceFormatter
{
    /// <summary>
    /// Formate un prix avec séparateurs de milliers (espaces) et point décimal.
    /// Exemple : 5295.84f → "5 295.84 €"
    /// </summary>
    public static string Format(float price)
    {
        // Formate avec 2 décimales et point comme séparateur décimal
        string priceStr = price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        
        // Ajoute les espaces comme séparateurs de milliers
        priceStr = AddThousandsSeparator(priceStr);
        
        // Ajoute le symbole €
        return priceStr + " €";
    }

    /// <summary>
    /// Surcharge pour accepter les int
    /// </summary>
    public static string Format(int price)
    {
        return Format((float)price);
    }

    /// <summary>
    /// Ajoute des espaces comme séparateurs de milliers.
    /// Exemple : "5295.84" → "5 295.84"
    /// </summary>
    private static string AddThousandsSeparator(string priceStr)
    {
        // Sépare la partie entière et décimale
        string[] parts = priceStr.Split('.');
        string integerPart = parts[0];
        string decimalPart = parts.Length > 1 ? parts[1] : "00";

        // Ajoute des espaces tous les 3 chiffres dans la partie entière
        string result = "";
        int count = 0;
        
        for (int i = integerPart.Length - 1; i >= 0; i--)
        {
            if (count > 0 && count % 3 == 0)
                result = " " + result;
            
            result = integerPart[i] + result;
            count++;
        }

        // Réassemble avec la partie décimale (avec point)
        return result + "." + decimalPart;
    }
}