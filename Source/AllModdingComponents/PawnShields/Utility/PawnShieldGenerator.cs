﻿using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;

namespace PawnShields
{
    /// <summary>
    /// Assists in generating shields for pawns. Based off PawnWeaponGenerator.
    /// </summary>
    public static class PawnShieldGenerator
    {
        public static List<ThingStuffPair> allShieldPairs;
        public static List<ThingStuffPair> workingShields = new List<ThingStuffPair>();

        /*static PawnShieldGenerator()
        {
            //Initialise all shields.
            Reset();
        }*/

        /// <summary>
        /// Tries to generate a shield for the pawn.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="request"></param>
        public static void TryGenerateShieldFor(Pawn pawn, PawnGenerationRequest request)
        {
            workingShields.Clear();

            // Same conditions as weapon generation, except using PawnKindDef ShieldPawnGeneratorProperties.shieldTags instead of pawn.kindDef.weaponTags
            var generatorProps = request.KindDef.GetModExtension<ShieldPawnGeneratorProperties>();
            if (generatorProps == null || generatorProps.shieldTags.NullOrEmpty())
                return;
            if (!pawn.RaceProps.ToolUser ||
                !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) ||
                pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                return;
            }

            var generatorPropsShieldMoney = generatorProps.shieldMoney;
            float randomInRange = generatorPropsShieldMoney.RandomInRange;
            foreach (var w in allShieldPairs)
            {
                if (w.Price <= randomInRange)
                    if (!w.thing.weaponTags.NullOrEmpty())
                    {
                        if (generatorProps.shieldTags.Any(tag => w.thing.weaponTags.Contains(tag)))
                        {
                            if (w.thing.generateAllowChance >= 1f ||
                                Rand.ChanceSeeded(w.thing.generateAllowChance, pawn.thingIDNumber ^ w.thing.shortHash ^ 0x1B3B648))
                            {
                                workingShields.Add(w);
                            }
                        }
                    }
            }
            if (workingShields.Count == 0)
            {
                //Log.Warning("No working shields found for " + pawn.Label + "::" + pawn.KindLabel);
                return;
            }

            if (workingShields.TryRandomElementByWeight(w => w.Commonality * w.Price, out var thingStuffPair))
            {
                var thingWithComps = (ThingWithComps)ThingMaker.MakeThing(thingStuffPair.thing, thingStuffPair.stuff);
                PawnGenerator.PostProcessGeneratedGear(thingWithComps, pawn);
                float biocodeWeaponChance = (request.BiocodeWeaponChance > 0f) ? request.BiocodeWeaponChance : pawn.kindDef.biocodeWeaponChance;
                if (Rand.Value < biocodeWeaponChance)
                    thingWithComps.TryGetComp<CompBiocodableWeapon>()?.CodeFor(pawn);
                pawn.equipment.AddEquipment(thingWithComps);
            }

            workingShields.Clear();
        }

        /// <summary>
        /// Resets the shield generator.
        /// </summary>
        public static void Reset()
        {
            static bool IsShield(ThingDef td) => td.equipmentType != EquipmentType.Primary && td.HasComp(typeof(CompShield));

            allShieldPairs = ThingStuffPair.AllWith(IsShield);

            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs.Where(IsShield))
            {
                float num = (from pa in allShieldPairs
                    where pa.thing == thingDef
                    select pa).Sum(pa => pa.Commonality);
                float num2 = thingDef.generateCommonality / num;
                if (num2 == 1f) continue;
                for (int i = 0; i < allShieldPairs.Count; i++)
                {
                    var thingStuffPair = allShieldPairs[i];
                    if (thingStuffPair.thing == thingDef)
                    {
                        allShieldPairs[i] = new ThingStuffPair(thingStuffPair.thing, thingStuffPair.stuff,
                            thingStuffPair.commonalityMultiplier * num2);
                    }
                }
            }
        }
    }
}
