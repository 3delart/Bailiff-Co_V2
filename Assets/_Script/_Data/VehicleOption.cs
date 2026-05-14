using UnityEngine;

namespace BailiffCo
{
    [System.Serializable]
    public class VehicleOption
    {
        /// <summary>Type of vehicle option (determines which fields are used).</summary>
        public VehicleOptionType Type;

        /// <summary>Display name of the option.</summary>
        public string OptionName;

        /// <summary>Price of the option.</summary>
        public float Price;

        /// <summary>Trailer prefab — only used when Type is Remorque.</summary>
        public GameObject TrailerPrefab;
    }
}
