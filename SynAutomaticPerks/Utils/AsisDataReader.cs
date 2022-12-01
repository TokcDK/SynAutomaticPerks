using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using SynAutomaticPerks.Extensions;
using SynAutomaticPerks.Settings;

namespace SynAutomaticPerks.Utils
{
    internal static class AsisDataReader
    {
        internal class ASISSetData
        {
            public string SectionName = "";
            public HashSet<StringCompareSettingGroup> List = new();
            public CompareType StringCompareType;
        }
        internal static void SetAsisAutoSpellsIniValuesToSettings()
        {
            Console.WriteLine("Set Asis autospells ini values into settings..");

            List<ASISSetData> AutomaticSpellsIniParamsData = new()
            {
                new ASISSetData(){ SectionName = "NPCInclusions", List = Program.Settings.Value.ASIS.NPCInclusions, StringCompareType = CompareType.StartsWith },
                new ASISSetData(){ SectionName = "NPCExclusions", List = Program.Settings.Value.ASIS.NPCExclusions, StringCompareType = CompareType.Contains },
                new ASISSetData(){ SectionName = "NPCKeywordExclusions", List = Program.Settings.Value.ASIS.NPCKeywordExclusions, StringCompareType = CompareType.StartsWith },
                new ASISSetData(){ SectionName = "NPCModExclusions", List = Program.Settings.Value.ASIS.NPCModExclusions, StringCompareType = CompareType.Equals },
                new ASISSetData(){ SectionName = "PerkModInclusions", List = Program.Settings.Value.ASIS.PerkModInclusions, StringCompareType = CompareType.Equals },
                new ASISSetData(){ SectionName = "PerkInclusions", List = Program.Settings.Value.ASIS.PerkInclusions, StringCompareType = CompareType.Equals },
                new ASISSetData(){ SectionName = "PERKEXCLUSIONSCONTAINS", List = Program.Settings.Value.ASIS.PerkExclusons, StringCompareType = CompareType.Contains },
                new ASISSetData(){ SectionName = "PERKEXCLUSIONSSTARTSWITH", List = Program.Settings.Value.ASIS.PerkExclusons, StringCompareType = CompareType.StartsWith },
                new ASISSetData(){ SectionName = "ForcedFollowers", List = Program.Settings.Value.ASIS.ForcedFollowers, StringCompareType = CompareType.Equals },
                new ASISSetData(){ SectionName = "FollowersFactions", List = Program.Settings.Value.ASIS.FollowersFactions, StringCompareType = CompareType.Equals },
            };

            foreach (var paramData in AutomaticSpellsIniParamsData)
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

        internal static Dictionary<string, HashSet<string>>? AutomaticSpellsIniParams = new()
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

        internal static void SearchAndTryReadASISIni(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
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
    }
}
