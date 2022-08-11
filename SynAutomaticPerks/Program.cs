using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SynAutomaticPerks
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "YourPatcher.esp")
                .Run(args);
        }

        public class ConditionData
        {
            public float Value;
            public Skill? ActorSkill;
        }
        public class PerkInfo
        {
            public List<ConditionData> Conditions = new();
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Dictionary<IPerkGetter, PerkInfo> PerkInfoList = GetPerkInfo(state);
            AddPerks(state, PerkInfoList);

        }

        private static void AddPerks(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, Dictionary<IPerkGetter, PerkInfo> perkInfoList)
        {
            foreach (var npcGetterContext in state.LoadOrder.PriorityOrder.Npc().WinningContextOverrides())
            {
                if (npcGetterContext == null) continue;

                var npcGetter = npcGetterContext.Record;
                if (npcGetter == null) continue;

                bool isNullPerks = npcGetter.Perks == null;
                bool isHavePerks = !isNullPerks && npcGetter.Perks!.Count>0;

                HashSet<FormKey> npcPerks = new(npcGetter.Perks!.Select(p=>p.Perk.FormKey));

                foreach (var perkGetter in perkInfoList)
                {
                    if (!isNullPerks && isHavePerks && npcPerks.Contains(perkGetter.Key.FormKey)) continue;


                }
            }
        }

        private static Dictionary<IPerkGetter, PerkInfo> GetPerkInfo(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Dictionary<IPerkGetter, PerkInfo> perkInfoList = new();
            //foreach (var npcGetter in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                //if (npcGetter == null) continue;

                Console.WriteLine($"Get perks");
                foreach (var perkGetterContext in state.LoadOrder.PriorityOrder.Perk().WinningContextOverrides())
                {
                    if (perkGetterContext == null) continue;
                    var perkGetter = perkGetterContext.Record;

                    if (perkGetter == null) continue;
                    if (perkGetter.EditorID != "ORD_Alt20_AlterationDualCasting_Perk_20_WasAlterationDualCasting") continue;

                    bool failed = false;
                    bool passed = false;
                    var perkInfo = new PerkInfo();
                    foreach (var perkCondition in perkGetter.Conditions)
                    {
                        if (perkCondition == null) continue;
                        if (perkCondition.Data.RunOnType != Condition.RunOnType.Subject) continue;
                        if (perkCondition.CompareOperator != CompareOperator.GreaterThanOrEqualTo) continue;

                        //if (!perkCondition.Flags.HasFlag(Condition.Function.GetBaseActorValue)) continue;
                        //if (!perkCondition.Flags.HasFlag(Condition.ParameterType.ActorValue)) continue;
                        //if (!perkCondition.Flags.HasFlag(ActorValue.Alteration)) continue;
                        //if (perkCondition.Flags.HasFlag(Condition.Flag.UseGlobal)) continue;
                        //if (perkCondition is not IConditionFloat cond) continue;
                        if (perkCondition is not IConditionFloatGetter floatCOnditionGetter) continue;
                        //var floatcond = (IConditionFloat)perkCondition;
                        //if (floatcond == null) continue;
                        //Console.WriteLine($"11111111111111111");
                        if (floatCOnditionGetter.Data is not IFunctionConditionDataGetter floatCOnditionDataGetter) continue;

                        if (floatCOnditionDataGetter.Function != Condition.Function.GetBaseActorValue) continue;

                        //Console.WriteLine($"We are here! perk:{perkGetter.Record.EditorID}");

                        try
                        {
                            var value = floatCOnditionGetter.ComparisonValue;
                            var actorValue = (ActorValue)floatCOnditionDataGetter.ParameterOneNumber;
                            var skill = GetSkillByActorValue(actorValue);
                            if (skill == null)
                            {
                                failed = true;
                                continue;
                            }

                            passed = true;
                            perkInfo.Conditions.Add(new ConditionData() { ActorSkill = skill, Value = value });
                            //Console.WriteLine($"{nameof(floatCOnditionDataGetter.ParameterOneNumber)}:{floatCOnditionDataGetter.ParameterOneNumber}");
                            //Console.WriteLine($"{(ActorValue)floatCOnditionDataGetter.ParameterOneNumber}");
                        }
                        catch
                        {
                            failed = true;
                        }
                    }

                    if (passed && !failed) perkInfoList.Add(perkGetter, perkInfo);
                }
            }

            return perkInfoList;
        }
        private static Skill? GetSkillByActorValue(ActorValue effectMagicSkillActorValue)
        {
            return (Skill?)Enum.Parse(typeof(Skill?), effectMagicSkillActorValue.ToString());
        }
    }
}
