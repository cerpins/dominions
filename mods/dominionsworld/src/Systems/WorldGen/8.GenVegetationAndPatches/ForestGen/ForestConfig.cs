using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace dominions.Systems.TreeGen
{
    // World property variant defines the "code"
    // Small changes to trees used in forest variants
    [JsonObject(MemberSerialization.OptIn)]
    public class ForestTreeProperties : WorldPropertyVariant
    { 
        [JsonProperty]
        public NatFloat Size = NatFloat.createGauss(1, 0.2f);
        // Chance to use this tree - needs to be renamed.
        [JsonProperty]
        public int Weight = 1; 
        // Radius where no other tree wil generate
        [JsonProperty]
        public int Distance = 0;
        // Distance from chunk side
        [JsonProperty]
        public int SideDistance = 0;
        // Forego distancing
        [JsonProperty]
        public bool IsShrub = false;

        [OnDeserialized]
        public void AfterDeserialization(StreamingContext context)
        {
            // Throw msg here if path does not exist
            Debug.WriteLine("Deserialized successfuly");
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ForestVariant
    {
        [JsonProperty]
        public string Code;
        [JsonProperty]
        public float Density = 1;
        [JsonProperty]
        public ForestTreeProperties[] TreeProps;
        [JsonProperty]
        public ForestTreeProperties[] ShrubProps;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ForestConfig
    {
        [JsonProperty]
        public string Code;
        [JsonProperty]
        public ForestVariant[] Variants;
    }
}