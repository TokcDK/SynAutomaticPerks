using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using SynAutomaticPerks.Settings;
using System.Collections.Generic;
using System.Linq;

namespace SynAutomaticPerks.Extensions
{
    public static class ListExtensions
    {
        public static bool HasAnyValidValue(this HashSet<StringCompareSettingGroup> list)
        {
            return list.Count > 0 && list.Any(g => g.StringsList.Any(s => !string.IsNullOrWhiteSpace(s.Name)));
        }

        public static bool HasAnyValidValue(this HashSet<ModKey> list)
        {
            return list.Count > 0 && list.Any(m => m != null && !m.IsNull);
        }

        public static bool HasAnyValidValue(this HashSet<FormLink<IFactionGetter>> list)
        {
            return list.Count > 0 && list.Any(r => r != null && !r.IsNull);
        }

        public static bool HasAnyValidValue(this HashSet<FormLink<INpcGetter>> list)
        {
            return list.Count > 0 && list.Any(r => r != null && !r.IsNull);
        }
    }
}
