// ============================================================
// HabitationData.cs — Bailiff & Co  V2
// ScriptableObject décrivant le logement du propriétaire.
// Affiché dans la fiche mission du Hub.
// Créer via : clic droit → Create → BailiffCo/HabitationData
// ============================================================
using UnityEngine;

[CreateAssetMenu(menuName = "BailiffCo/HabitationData")]
public class HabitationData : ScriptableObject
{
    [Header("Logement")]
    [Tooltip("Ex : Individuelle, Appartement, Loft…")]
    public string Type    = "Individuelle";

    [Tooltip("Surface en m²")]
    public int    Surface = 100;

    [Tooltip("Ex : Plain-pied, 1er étage, 2 + sous-sol…")]
    public string Etage   = "Plain-pied";

    [Tooltip("Ex : Porte, Porte + Garage, Digicode…")]
    public string Acces   = "Porte";
}
