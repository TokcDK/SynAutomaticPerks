using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynAutomaticPerks.Extensions;
using SynAutomaticPerks.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SynAutomaticPerks.Extensions;

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

        public class ASISSetData
        {
            public string SectionName = "";
            public HashSet<StringCompareSettingGroup> List = new();
            public CompareType StringCompareType;
        }
        private static void SetAsisAutoSpellsIniValuesToSettings()
        {
            Console.WriteLine("Set Asis autospells ini values into settings..");

            List<ASISSetData> AutomaticSpellsIniParamsData = new()
            {
                new ASISSetData(){ SectionName = "NPCInclusions", List = Settings.Value.ASIS.NPCInclusions, StringCompareType = CompareType.StartsWith },
                new ASISSetData(){ SectionName = "NPCExclusions", List = Settings.Value.ASIS.NPCExclusions, StringCompareType = CompareType.Contains },
                new ASISSetData(){ SectionName = "NPCKeywordExclusions", List = Settings.Value.ASIS.NPCKeywordExclusions, StringCompareType = CompareType.StartsWith },
                new ASISSetData(){ SectionName = "NPCModExclusions", List = Settings.Value.ASIS.NPCModExclusions, StringCompareType = CompareType.Equals },
                new ASISSetData(){ SectionName = "PerkModInclusions", List = Settings.Value.ASIS.PerkModInclusions, StringCompareType = CompareType.Equals },
                new ASISSetData(){ SectionName = "PerkInclusions", List = Settings.Value.ASIS.PerkInclusions, StringCompareType = CompareType.Equals },
                new ASISSetData(){ SectionName = "PERKEXCLUSIONSCONTAINS", List = Settings.Value.ASIS.PerkExclusons, StringCompareType = CompareType.Contains },
                new ASISSetData(){ SectionName = "PERKEXCLUSIONSSTARTSWITH", List = Settings.Value.ASIS.PerkExclusons, StringCompareType = CompareType.StartsWith },
                new ASISSetData(){ SectionName = "ForcedFollowers", List = Settings.Value.ASIS.ForcedFollowers, StringCompareType = CompareType.Equals },
                new ASISSetData(){ SectionName = "FollowersFactions", List = Settings.Value.ASIS.FollowersFactions, StringCompareType = CompareType.Equals },
            };

            foreach(var paramData in AutomaticSpellsIniParamsData)
            {
                var list = paramData.List;
                var groupToAdd = new StringCompareSettingGroup() { Comment = nameof(list) };
                HashSet<string> addStrings = new();
                foreach (var v in AutomaticSpellsIniParams![paramData.SectionName])
                {
                    if (addStrings.Contains(v)) continue;

                    addStrings.Add(v);
                    var stringInfo = new StringCompareSetting
                    {
                        Name = v,
                        IgnoreCase = true,
                        Compare = paramData.StringCompareType
                    };

                    groupToAdd.StringsList.Add(stringInfo);
                }
                list.Add(groupToAdd);
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
            if (perkInfoList.Count == 0)
            {
                Console.WriteLine($"No perks to add...Exit.");
                return;
            }

            bool useNpcModExclude = Settings.Value.NativeSettings.NpcModExclude.HasAnyValidValue();
            bool useNpcModExcludeByName = Settings.Value.ASIS.NPCModExclusions.HasAnyValidValue();
            bool useNpcExclude = Settings.Value.ASIS.NPCExclusions.HasAnyValidValue();
            bool useNpcInclude = Settings.Value.ASIS.NPCInclusions.HasAnyValidValue();
            bool useNpcKeywordExclude = Settings.Value.ASIS.NPCKeywordExclusions.HasAnyValidValue();
            bool useFollowersFactions = Settings.Value.NativeSettings.FollowersFactions.HasAnyValidValue() || Settings.Value.ASIS.FollowersFactions.HasAnyValidValue();
            bool useForceFollowers = Settings.Value.NativeSettings.ForcedFollowersNpc.HasAnyValidValue() || Settings.Value.ASIS.ForcedFollowers.HasAnyValidValue();

            int patchedNpcCount = 0; // for tests
            int showProgressInfoCounter = 1000; // for tests
            Console.WriteLine($"Patching npcs...");
            foreach (var npcGetter in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                // skip invalid
                if (npcGetter == null) continue;
                if (npcGetter.IsDeleted) continue;
                if (string.IsNullOrWhiteSpace(npcGetter.EditorID)) continue;
                if (npcGetter.Template != null && !npcGetter.Template.IsNull && npcGetter.Configuration.TemplateFlags.HasFlag( NpcConfiguration.TemplateFlag.SpellList)) continue; // use spells & perks from template
                
                var sourceModKey = npcGetter.FormKey.ModKey;
                //if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc source mod is in excluded list");
                if (useNpcModExclude && Settings.Value.NativeSettings.NpcModExclude.Contains(sourceModKey)) continue;
                //if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc source mod is in included list");
                if (useNpcModExcludeByName && sourceModKey.FileName.String.HasAnyFromList(Settings.Value.ASIS.NPCModExclusions)) continue;

                //if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc has spells");
                //if (npcGetter.ActorEffect == null) continue;
                //if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc edid is not empty");

                // followers specific checks
                bool IsFolower = useForceFollowers && (Settings.Value.NativeSettings.ForcedFollowersNpc.Contains(npcGetter) || npcGetter.EditorID.HasAnyFromList(Settings.Value.ASIS.ForcedFollowers));
                if (!IsFolower) IsFolower = useFollowersFactions && npcGetter.Factions.Any(f => (Settings.Value.NativeSettings.FollowersFactions.Contains(f.Faction) || f.Faction.FormKey.ToString().Replace(':', '=').HasAnyFromList(Settings.Value.ASIS.FollowersFactions)));

                // npc specific checks, not followers
                //if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc in ignore list");
                if (useNpcExclude && !IsFolower && npcGetter.EditorID.HasAnyFromList(Settings.Value.ASIS.NPCExclusions)) continue;
                //if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc in included list");
                if (useNpcInclude && !IsFolower && !npcGetter.EditorID.HasAnyFromList(Settings.Value.ASIS.NPCInclusions)) continue;
                //if (IsDebugNPC) Console.WriteLine($"{npcDebugID} check if npc has keywords from ignore list");
                if (useNpcKeywordExclude && !IsFolower && npcGetter.Keywords != null)
                {
                    bool skip = false;
                    foreach (var keywordGetterFormLink in npcGetter.Keywords)
                    {
                        if (!keywordGetterFormLink.TryResolve(state.LinkCache, out var keywordGeter)) continue;
                        if (string.IsNullOrWhiteSpace(keywordGeter.EditorID)) continue;

                        if (!keywordGeter.EditorID.HasAnyFromList(Settings.Value.ASIS.NPCKeywordExclusions)) continue;

                        skip = true;
                        break;
                    }

                    //if (IsDebugNPC) Console.WriteLine($"{npcDebugID} skip npc if has excluded keyword:{skip}");
                    if (skip) continue;
                }

                if (!npcGetter.TryUnTemplate(state.LinkCache, NpcConfiguration.TemplateFlag.Stats, out var untemplatedNpc)) continue;

                bool isNullPerks = npcGetter.Perks == null;
                bool isHavePerks = !isNullPerks && npcGetter.Perks!.Count > 0;
                HashSet<FormKey> npcPerks = isHavePerks ? new(npcGetter.Perks!.Select(p => p.Perk.FormKey)) : new();

                HashSet<IPerkGetter> perksToAdd = new();
                foreach (var perkInfo in perkInfoList)
                {
                    if (isHavePerks && npcPerks.Contains(perkInfo.Key.FormKey)) continue;

                    bool isAnyConditionFailed = false;
                    foreach (var perkCondtion in perkInfo.Value.Conditions)
                    {
                        var npcSkillValue = untemplatedNpc.PlayerSkills!.SkillValues[perkCondtion.ActorSkill] + untemplatedNpc.PlayerSkills.SkillOffsets[perkCondtion.ActorSkill];
                        if (npcSkillValue < perkCondtion.Value) { isAnyConditionFailed = true; break; }
                    }

                    if (isAnyConditionFailed) continue;

                    perksToAdd.Add(perkInfo.Key);
                }

                if (perksToAdd.Count == 0) continue;

                patchedNpcCount++;
                var npc = state.PatchMod.Npcs.GetOrAddAsOverride(npcGetter);
                if (isNullPerks) npc.Perks = new Noggog.ExtendedList<PerkPlacement>();
                foreach (var perkToAdd in perksToAdd)
                {
                    var perkItem = new PerkPlacement
                    {
                        Perk = perkToAdd.ToLink(),
                        Rank = 1
                    };
                    npc.Perks!.Add(perkItem);
                }

                if (--showProgressInfoCounter <= 0)
                {
                    showProgressInfoCounter = 1000;
                    Console.WriteLine($"Still patching npcs.. Patched {patchedNpcCount} npcs..");
                }
                //Console.WriteLine($"Added {perksToAdd.Count} perks to {npc.EditorID}");
            }

            Console.WriteLine($"\n\nOverall patched {patchedNpcCount} npcs");
        }

        private static Dictionary<IPerkGetter, PerkInfo> GetPerkInfo(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Dictionary<IPerkGetter, PerkInfo> perkInfoList = new();
            HashSet<ActorValue> validActorValues = new(18)
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

            // check if lists can be used
            bool useModInclude = Settings.Value.NativeSettings.PerkModInclude.HasAnyValidValue() || Settings.Value.ASIS.PerkModInclusions.HasAnyValidValue();
            bool usePerkInclude = Settings.Value.ASIS.PerkInclusions.HasAnyValidValue();
            bool usePerkExclude = Settings.Value.ASIS.PerkExclusons.HasAnyValidValue();

            Console.WriteLine($"Get valid perks infos..");
            foreach (var perkGetterContext in state.LoadOrder.PriorityOrder.Perk().WinningContextOverrides())
            {
                if (perkGetterContext == null) continue;
                var perkGetter = perkGetterContext.Record;

                if (perkGetter == null) continue;
                if (perkGetter.IsDeleted) continue;
                //if (perkGetter.EditorID != "ORD_Alt20_AlterationDualCasting_Perk_20_WasAlterationDualCasting") continue;

                //if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if spel is from included mods");
                var sourceModKey = state.LinkCache.ResolveAllContexts<IPerk, IPerkGetter>(perkGetter.FormKey).Last().ModKey;
                if (useModInclude && !Settings.Value.NativeSettings.PerkModInclude.Contains(sourceModKey)
                    && !sourceModKey.FileName.String.HasAnyFromList(Settings.Value.ASIS.PerkModInclusions)) continue;

                //if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if spell cast type is valid");
                //if (!IsValidSpellType(spellGetter)) continue;
                //if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if already added");
                //if (spellInfoList.ContainsKey(spellGetter)) continue;
                //if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if has empty edid");
                var edid = perkGetter.EditorID ?? "";
                bool edidEmpty = edid == "";
                if (usePerkInclude && !edidEmpty && !edid.HasAnyFromList(Settings.Value.ASIS.PerkInclusions)) continue;
                //if (IsDebugSpell) Console.WriteLine($"{spellDebugID} check if the spell is in excluded list");
                if (usePerkExclude && !edidEmpty && edid.HasAnyFromList(Settings.Value.ASIS.PerkExclusons)) continue;

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

                    //Console.WriteLine($"actorValue:{actorValue}");
                    if (!Enum.TryParse(typeof(Skill), actorValue.ToString(), out var avSkill))
                    {
                        //Console.WriteLine("failed to get skill");
                        failed = true;
                        continue;
                    }
                    var value = floatCOnditionGetter.ComparisonValue;
                    //Console.WriteLine($"actorValue:{actorValue},skill:{avSkill},value:{value}");

                    //Console.WriteLine("add perk info");
                    passed = true;
                    perkInfo.Conditions.Add(new ConditionData() { ActorSkill = (Skill)avSkill!, Value = value });
                    //Console.WriteLine($"{nameof(floatCOnditionDataGetter.ParameterOneNumber)}:{floatCOnditionDataGetter.ParameterOneNumber}");
                    //Console.WriteLine($"{(ActorValue)floatCOnditionDataGetter.ParameterOneNumber}");
                }

                if (passed && !failed) perkInfoList.Add(perkGetter, perkInfo);
            }

            Console.WriteLine($"Added {perkInfoList.Count} perk infos!");
            return perkInfoList;
        }
    }
}
