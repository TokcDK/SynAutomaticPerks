using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Synthesis.Settings;
using StringCompareSettings;
using System.Collections.Generic;

namespace SynAutomaticPerks
{
    public class PatcherSettings
    {
        [SynthesisOrder]
        [SynthesisTooltip("ASIS like options, can be read from ASIS AutomaticSpell.ini if exist or entered manually here")]
        public ASISOptions ASIS = new();
    }

    public class ASISOptions
    {
        [SynthesisOrder]
        [SynthesisDiskName("NPCInclusions")]
        //[SynthesisSettingName("NPC Include")]
        [SynthesisTooltip("Strings determine included npcs by editor id")]
        public HashSet<StringCompareSetting> NPCInclusions = new();
        [SynthesisOrder]
        [SynthesisDiskName("NPCExclusions")]
        //[SynthesisSettingName("NPC Exclude")]
        [SynthesisTooltip("Strings determine excluded npcs by editor id")]
        public HashSet<StringCompareSetting> NPCExclusions = new();
        [SynthesisOrder]
        [SynthesisDiskName("NPCModExclusions")]
        //[SynthesisSettingName("Npc Keyword Exclude")]
        [SynthesisTooltip("Strings determine excluded mods for npcs")]
        public HashSet<StringCompareSetting> NPCModExclusions = new();
        [SynthesisOrder]
        [SynthesisDiskName("NPCKeywordExclusions")]
        //[SynthesisSettingName("Npc Keyword Exclude")]
        [SynthesisTooltip("Strings determine excluded npcs by editor id")]
        public HashSet<StringCompareSetting> NPCKeywordExclusions = new();
        [SynthesisOrder]
        [SynthesisDiskName("PerkInclusions")]
        //[SynthesisSettingName("Npc Keyword Exclude")]
        [SynthesisTooltip("Strings determine list of icluded perks to add only. All if empty")]
        public HashSet<StringCompareSetting> PerkInclusions = new();
        [SynthesisOrder]
        [SynthesisDiskName("PerkExclusons")]
        //[SynthesisSettingName("Spell Exclude")]
        [SynthesisTooltip("Strings determine excluded perks by editor id")]
        public HashSet<StringCompareSetting> PerkExclusons = new();
        [SynthesisOrder]
        [SynthesisDiskName("PerkModInclusions")]
        //[SynthesisSettingName("SpellModExclude")]
        [SynthesisTooltip("Strings determine included mods for perks")]
        public HashSet<StringCompareSetting> PerkModInclusions = new();
    }
}
