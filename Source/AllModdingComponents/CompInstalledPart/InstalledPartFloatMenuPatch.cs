﻿using System;
using System.Collections.Generic;
using JecsTools;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CompInstalledPart
{
    public class InstalledPartFloatMenuPatch : FloatMenuPatch
    {
        public override IEnumerable<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>>
            GetFloatMenus()
        {
            var curCondition = new _Condition(_ConditionType.IsType, typeof(ThingWithComps));

            static List<FloatMenuOption> curFunc(Vector3 clickPos, Pawn pawn, Thing curThing)
            {
                var opts = new List<FloatMenuOption>();
                if (curThing.TryGetCompInstalledPart() is CompInstalledPart groundPart)
                    if (pawn.equipment != null)
                    {
                        //Remove "Equip" option from right click.
                        if (groundPart.GetEquippable != null)
                        {
                            var optToRemoveIndex = opts.FindIndex(x => x.Label.Contains(curThing.Label));
                            if (optToRemoveIndex >= 0) opts.RemoveAt(optToRemoveIndex);
                        }

                        var text = "CompInstalledPart_Install".Translate();
                        opts.Add(new FloatMenuOption(text, delegate
                        {
                            var props = groundPart.Props;
                            if (props != null)
                                if (!props.allowedToInstallOn.NullOrEmpty())
                                {
                                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                                    Find.Targeter.BeginTargeting(new TargetingParameters
                                    {
                                        canTargetPawns = true,
                                        canTargetBuildings = true,
                                        mapObjectTargetsMustBeAutoAttackable = false,
                                        validator = delegate(TargetInfo targ)
                                        {
                                            if (!targ.HasThing)
                                                return false;
                                            return props.allowedToInstallOn.Contains(targ.Thing.def);
                                        }
                                    }, delegate(LocalTargetInfo target)
                                    {
                                        curThing.SetForbidden(false);
                                        groundPart.GiveInstallJob(pawn, target.Thing);
                                    }, null, null, null);
                                }
                                else
                                {
                                    Log.ErrorOnce(
                                        "CompInstalledPart :: allowedToInstallOn list needs to be defined in XML.",
                                        3242);
                                }
                        }, MenuOptionPriority.Default, null, null, 29f, null, null));
                    }
                return opts;
            };

            yield return new KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>(curCondition, curFunc);
        }
    }
}
