using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
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

        public class PerkInfo
        {
            List<IConditionGetter> Conditions = new();
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach (var npcGetter in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                foreach (var perkGetter in state.LoadOrder.PriorityOrder.Perk().WinningOverrides())
                {
                    foreach (var perkCondition in perkGetter.Conditions)
                    {
                        bool useGlobal = perkCondition.Flags.HasFlag(Condition.Flag.UseGlobal);

                        //var func = perkCondition.
                    }
                }
            }
        }
    }
}
