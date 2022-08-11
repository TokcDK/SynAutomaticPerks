using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SkyrimNPCHelpers;
using StringCompareSettings;
using IniReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SynAutomaticPerks
{
    public class Program
    {
        static Lazy<PatcherSettings> Settings = null!;
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetAutogeneratedSettings("PatcherSettings", "settings.json", out Settings)
                .SetTypicalOpen(GameRelease.SkyrimSE, "SynAutomaticPerks.esp")
                .Run(args);
        }

        public class ConditionData
        {
            public float Value;
            public Skill ActorSkill;
        }
        public class PerkInfo
        {
            public List<ConditionData> Conditions = new();
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            SearchAndTryReadASISIni(state);

            SetAsisAutoSpellsIniValuesToSettings();

            Dictionary<IPerkGetter, PerkInfo> PerkInfoList = GetPerkInfo(state);
            AddPerksToNpc(state, PerkInfoList);
        }

        private static void SetAsisAutoSpellsIniValuesToSettings()
        {
            Console.WriteLine("Set Asis autospells ini values into settings..");

            foreach (var v in AutomaticSpellsIniParams!["NPCInclusions"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.StartsWith
                };

                var list = Settings.Value.ASIS.NPCInclusions;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }
            foreach (var v in AutomaticSpellsIniParams["NPCExclusions"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.Contains
                };

                var list = Settings.Value.ASIS.NPCExclusions;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }
            foreach (var v in AutomaticSpellsIniParams["PERKEXCLUSIONSCONTAINS"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.Contains
                };

                var list = Settings.Value.ASIS.PerkExclusons;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }
            foreach (var v in AutomaticSpellsIniParams["PERKEXCLUSIONSSTARTSWITH"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.StartsWith
                };

                var list = Settings.Value.ASIS.PerkExclusons;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }
            foreach (var v in AutomaticSpellsIniParams["NPCKeywordExclusions"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.StartsWith
                };

                var list = Settings.Value.ASIS.NPCKeywordExclusions;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }
            foreach (var v in AutomaticSpellsIniParams["NPCModExclusions"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.Equals
                };

                var list = Settings.Value.ASIS.NPCModExclusions;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }
            foreach (var v in AutomaticSpellsIniParams["PerkModInclusions"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.Equals
                };

                var list = Settings.Value.ASIS.PerkModInclusions;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }
            foreach (var v in AutomaticSpellsIniParams["PerkInclusions"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.Equals
                };

                var list = Settings.Value.ASIS.PerkModInclusions;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }
            foreach (var v in AutomaticSpellsIniParams["ForcedFollowers"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.Equals
                };

                var list = Settings.Value.ASIS.PerkModInclusions;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }
            foreach (var v in AutomaticSpellsIniParams["FollowersFactions"])
            {
                var stringInfo = new StringCompareSetting
                {
                    Name = v,
                    IgnoreCase = true,
                    Compare = CompareType.Equals
                };

                var list = Settings.Value.ASIS.PerkModInclusions;
                if (list.Contains(stringInfo)) continue;

                list.Add(stringInfo);
            }

            AutomaticSpellsIniParams = null;
        }

        public static Dictionary<string, HashSet<string>>? AutomaticSpellsIniParams = new()
        {
            { "NPCInclusions", new HashSet<string>() },
            { "NPCExclusions", new HashSet<string>() },
            { "NPCKeywordExclusions", new HashSet<string>() },
            { "NPCModExclusions", new HashSet<string>() },
            { "PerkModInclusions", new HashSet<string>() },
            { "PerkInclusions", new HashSet<string>() },
            { "PERKEXCLUSIONSCONTAINS", new HashSet<string>() },
            { "PERKEXCLUSIONSSTARTSWITH", new HashSet<string>() },
            { "ForcedFollowers", new HashSet<string>() },
            { "FollowersFactions", new HashSet<string>() },
        };

        private static void SearchAndTryReadASISIni(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var iniPath = Path.Combine(state!.DataFolderPath, "SkyProc Patchers", "ASIS", "AutomaticPerks.ini");
            if (!File.Exists(iniPath)) return;

            // read AutomaticSpells ini parameters into settings
            Console.WriteLine("Found ASIS 'AutomaticPerks.ini'. Trying to read..");

            Dictionary<string, HashSet<string>> iniSections = new();
            iniSections.ReadIniSectionValuesFrom(iniPath);

            var keys = new HashSet<string>(AutomaticSpellsIniParams!.Keys);
            int iniValuesCount = 0;
            int iniSectionsCount = 0;
            foreach (var key in keys)
            {
                if (!iniSections.ContainsKey(key)) continue;

                var v = iniSections[key];
                AutomaticSpellsIniParams[key] = v;
                iniValuesCount += v.Count;
                iniSectionsCount++;
            }

            Console.WriteLine($"Added {iniSectionsCount} sections and {iniValuesCount} values from 'AutomaticPerks.ini'");
        }

        private static void AddPerksToNpc(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, Dictionary<IPerkGetter, PerkInfo> perkInfoList)
        {
            int limit = 0; // for tests
            foreach (var npcGetterContext in state.LoadOrder.PriorityOrder.Npc().WinningContextOverrides())
            {
                if (npcGetterContext == null) continue;
                if (limit >= 100) continue;

                var npcGetter = npcGetterContext.Record;
                if (npcGetter == null) continue;
                
                if (!npcGetter.TryUnTemplate(state.LinkCache, NpcConfiguration.TemplateFlag.Stats, out var untemplatedNpc)) continue;

                bool isNullPerks = npcGetter.Perks == null;
                bool isHavePerks = !isNullPerks && npcGetter.Perks!.Count > 0;
                HashSet<FormKey> npcPerks = isHavePerks? new(npcGetter.Perks!.Select(p => p.Perk.FormKey)):new();

                HashSet<IPerkGetter> perksToAdd = new();
                foreach (var perkInfo in perkInfoList)
                {
                    if (!isNullPerks && isHavePerks && npcPerks.Contains(perkInfo.Key.FormKey)) continue;

                    bool isAnyConditionFailed = false;
                    foreach(var perkCondtion in perkInfo.Value.Conditions)
                    {
                        var npcSkillValue = untemplatedNpc.PlayerSkills!.SkillValues[perkCondtion.ActorSkill] + untemplatedNpc.PlayerSkills.SkillOffsets[perkCondtion.ActorSkill];
                        if (npcSkillValue < perkCondtion.Value) { isAnyConditionFailed = true; break; }
                    }

                    if (isAnyConditionFailed) continue;

                    perksToAdd.Add(perkInfo.Key);
                }

                if (perksToAdd.Count == 0) continue;

                limit++;
                var npc = state.PatchMod.Npcs.GetOrAddAsOverride(npcGetter);
                if (npc.Perks == null) npc.Perks = new Noggog.ExtendedList<PerkPlacement>();
                foreach(var perkToAdd in perksToAdd)
                {
                    var perkItem = new PerkPlacement
                    {
                        Perk = perkToAdd.AsLink(),
                        Rank = 1
                    };
                    npc.Perks.Add(perkItem);
                }

                Console.WriteLine($"Added {perksToAdd.Count} perks to {npc.EditorID}");
            }
        }

        private static Dictionary<IPerkGetter, PerkInfo> GetPerkInfo(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Dictionary<IPerkGetter, PerkInfo> perkInfoList = new();
            HashSet<ActorValue> validActorValues = new()
                {
                    ActorValue.OneHanded,
                    ActorValue.TwoHanded,
                    ActorValue.Archery,
                    ActorValue.Block,
                    ActorValue.Smithing,
                    ActorValue.HeavyArmor,
                    ActorValue.LightArmor,
                    ActorValue.Pickpocket,
                    ActorValue.Lockpicking,
                    ActorValue.Sneak,
                    ActorValue.Alchemy,
                    ActorValue.Speech,
                    ActorValue.Alteration,
                    ActorValue.Conjuration,
                    ActorValue.Destruction,
                    ActorValue.Illusion,
                    ActorValue.Restoration,
                    ActorValue.Enchanting,
                };
            Console.WriteLine($"Get perks");
            foreach (var perkGetterContext in state.LoadOrder.PriorityOrder.Perk().WinningContextOverrides())
            {
                if (perkGetterContext == null) continue;
                var perkGetter = perkGetterContext.Record;

                if (perkGetter == null) continue;
                //if (perkGetter.EditorID != "ORD_Alt20_AlterationDualCasting_Perk_20_WasAlterationDualCasting") continue;

                bool failed = false;
                bool passed = false;
                var perkInfo = new PerkInfo();
                foreach (var perkCondition in perkGetter.Conditions)
                {
                    if (perkCondition == null) continue;
                    //Console.WriteLine($"1");
                    if (perkCondition.Data.RunOnType != Condition.RunOnType.Subject) continue;
                    //Console.WriteLine($"2");
                    if (perkCondition.CompareOperator != CompareOperator.GreaterThanOrEqualTo) continue;

                    //Console.WriteLine($"3");
                    if (perkCondition is not IConditionFloatGetter floatCOnditionGetter) continue;
                    //Console.WriteLine($"4");
                    if (floatCOnditionGetter.Data is not IFunctionConditionDataGetter floatCOnditionDataGetter) continue;

                    //Console.WriteLine($"5");
                    if (floatCOnditionDataGetter.Function != Condition.Function.GetBaseActorValue) continue;

                    //Console.WriteLine($"6");

                    var actorValue = (ActorValue)floatCOnditionDataGetter.ParameterOneNumber;
                    if (!validActorValues.Contains(actorValue)) continue;

                    Console.WriteLine($"actorValue:{actorValue}");
                    if (!Enum.TryParse(typeof(Skill), actorValue.ToString(), out var avSkill))
                    {
                        //Console.WriteLine("failed to get skill");
                        failed = true;
                        continue;
                    }
                    var value = floatCOnditionGetter.ComparisonValue;
                    Console.WriteLine($"actorValue:{actorValue},skill:{avSkill},value:{value}");

                    Console.WriteLine("add perk info");
                    passed = true;
                    perkInfo.Conditions.Add(new ConditionData() { ActorSkill = (Skill)avSkill!, Value = value });
                    //Console.WriteLine($"{nameof(floatCOnditionDataGetter.ParameterOneNumber)}:{floatCOnditionDataGetter.ParameterOneNumber}");
                    //Console.WriteLine($"{(ActorValue)floatCOnditionDataGetter.ParameterOneNumber}");
                }

                if (passed && !failed) perkInfoList.Add(perkGetter, perkInfo);
            }

            return perkInfoList;
        }
    }
}
