using UnityEngine;

namespace BailiffCo
{
    [System.Serializable]
    public class VehicleOption
    {
        public VehicleOptionType Type;
        public string OptionName;
        public float Price;
        public GameObject TrailerPrefab; // Only for Remorque type
    }
}
