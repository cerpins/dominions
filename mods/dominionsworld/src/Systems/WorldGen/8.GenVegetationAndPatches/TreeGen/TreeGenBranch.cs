﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.MathTools;

namespace dominions.Systems.TreeGen
{
    // Subset of tree gen config
    public class Inheritance
    {
        public int from;
        public string[] skip;
    }

    public class TreeGenBranch
    {
        public Inheritance inherit;

        /// <summary>
        /// Thicknesss multiplier applied on the first sequence
        /// </summary>
        public float widthMultiplier = 1;

        /// <summary>
        /// Branch radius
        /// </summary>
        public float girth = 0;
        
        /// <summary>
        /// Girth evolution relative to total itterations
        /// </summary>
        public EvolvingNatFloat girthEvolve = EvolvingNatFloat.create(EnumTransformFunction.IDENTICAL, 1);

        /// <summary>
        /// Offset that penetrates into the earth
        /// </summary>
        public int yHardOffset = 0;

        /// <summary>
        /// Thickness loss per sequence
        /// </summary>
        public float widthloss = 0.05f;

        /// <summary>
        /// Stop growing once size has gone below this value
        /// </summary>
        public NatFloat dieAt = NatFloat.createUniform(0.0002f, 0);

        /// <summary>
        /// Amount up vertical angle loss due to gravity
        /// </summary>
        public float gravityDrag = 0f;

        /// <summary>
        /// Vertical angle
        /// </summary>
        public NatFloat angleVert = null;

        /// <summary>
        /// Horizontal angle
        /// </summary>
        public NatFloat angleHori = NatFloat.createUniform(0, GameMath.PI);

        /// <summary>
        /// Own Thickness loss multiplier per sub branch
        /// </summary>
        public float branchWidthLossMul = 1f;


        /// <summary>
        /// Modification of vertical angle over distance
        /// </summary>
        public EvolvingNatFloat angleVertEvolve = EvolvingNatFloat.createIdentical(GameMath.PI / 2);

        /// <summary>
        /// Modification of horizontal angle over distance
        /// </summary>
        public EvolvingNatFloat angleHoriEvolve = EvolvingNatFloat.createIdentical(0f);

        /// <summary>
        /// When to start branches relative to total itterations 
        /// </summary>
        public NatFloat branchStart = NatFloat.createUniform(0.7f, 0f);

        /// <summary>
        /// Whether to stop growing after first set of branches
        /// </summary>
        public bool capBranch = false;

        /// <summary>
        /// Branch spacing relative to total itterations
        /// </summary>
        public NatFloat branchSpacing = NatFloat.createUniform(0.3f, 0f);

        public NatFloat branchVerticalAngle = NatFloat.createUniform(0, GameMath.PI);

        public NatFloat branchHorizontalAngle = NatFloat.createUniform(0, GameMath.PI);



        /// <summary>
        /// Thickness of sub branches
        /// </summary>
        public NatFloat branchWidthMultiplier = NatFloat.createUniform(0, 0f);

        /// <summary>
        /// Thickness of sub branches. If null then for each branch event a new multiplier will be read from branchWidthMultiplier. Otherwise multiplier wil be read once and evolved using branchWidthMultiplierEvolve's algo.
        /// </summary>
        public EvolvingNatFloat branchWidthMultiplierEvolve = null;

        /// <summary>
        /// Amount of sub branches over distance (beginning of branch = sequence 0, end of branch = sequence 1000)
        /// </summary>            
        public NatFloat branchQuantity = NatFloat.createUniform(1, 0);

        /// <summary>
        /// Amount of sub branches over distance. If null then for each branch event a new quantity will be read from branchQuantity. Otherwise branchQuantity wil be read once and evolved using branchQuantityEvolve's algo.
        /// </summary>            
        public EvolvingNatFloat branchQuantityEvolve = null;


        public TreeGenBranch()
        {

        }

        public void InheritFrom(TreeGenBranch treeGenTrunk, string[] skip)
        {
            FieldInfo[] fields = GetType().GetFields();
            foreach (FieldInfo field in fields)
            {
                if (!skip.Contains(field.Name))
                {
                    field.SetValue(this, treeGenTrunk.GetType().GetField(field.Name).GetValue(treeGenTrunk));
                }
            }
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (angleVert == null) angleVert = NatFloat.createUniform(0, 0);
        }
    }
}
