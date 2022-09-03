﻿/*
==========================================================================
This file is part of Briefing Room for DCS World, a mission
generator for DCS World, by @akaAgar
(https://github.com/akaAgar/briefing-room-for-dcs)

Briefing Room for DCS World is free software: you can redistribute it
and/or modify it under the terms of the GNU General Public License
as published by the Free Software Foundation, either version 3 of
the License, or (at your option) any later version.

Briefing Room for DCS World is distributed in the hope that it will
be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Briefing Room for DCS World.
If not, see https://www.gnu.org/licenses/
==========================================================================
*/

using BriefingRoom4DCS.Mission;
using BriefingRoom4DCS.Template;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BriefingRoom4DCS
{
    internal class MizMaker
    {

        internal static byte[] ExportToMizBytes(DCSMission mission, MissionTemplate template)
        {
            Dictionary<string, byte[]> MizFileEntries = new Dictionary<string, byte[]>();

            AddStringValueToEntries(MizFileEntries, "briefing.html", mission.Briefing.GetBriefingAsHTML(true));
            AddStringValueToEntries(MizFileEntries, "credits.txt", $"Generated with BriefingRoom for DCS World (https://akaagar.itch.io/briefing-room-for-dcs) {BriefingRoom.VERSION} ({BriefingRoom.BUILD_VERSION})");
            if (template != null){
                AddStringValueToEntries(MizFileEntries, "l10n/DEFAULT/template.brt", Encoding.ASCII.GetString(template.GetIniBytes()));
                mission.AppendValue("MapResourcesFiles", $"[\"ResKey_Snd_template\"] = \"template.brt\",\n");
            }
            AddLuaFileToEntries(MizFileEntries, "mission", "Mission.lua", mission);
            AddLuaFileToEntries(MizFileEntries, "options", "Options.lua", null);
            AddStringValueToEntries(MizFileEntries, "theatre", mission.GetValue("TheaterID"));
            AddLuaFileToEntries(MizFileEntries, "warehouses", "Warehouses.lua", mission);

            AddLuaFileToEntries(MizFileEntries, "l10n/DEFAULT/dictionary", "Dictionary.lua", mission);
            AddLuaFileToEntries(MizFileEntries, "l10n/DEFAULT/mapResource", "MapResource.lua", mission);
            AddLuaFileToEntries(MizFileEntries, "l10n/DEFAULT/script.lua", "Script.lua", mission);

            foreach (string mediaFile in mission.GetMediaFileNames())
            {
                byte[] fileBytes = mission.GetMediaFile(mediaFile);
                if (fileBytes == null) continue;
                MizFileEntries.Add(mediaFile, fileBytes);
            }

            return Toolbox.ZipData(MizFileEntries);
        }

        private static bool AddLuaFileToEntries(Dictionary<string, byte[]> mizFileEntries, string mizEntryKey, string sourceFile, DCSMission mission = null)
        {
            if (string.IsNullOrEmpty(mizEntryKey) || mizFileEntries.ContainsKey(mizEntryKey) || string.IsNullOrEmpty(sourceFile)) return false;
            sourceFile = $"{BRPaths.INCLUDE_LUA}{sourceFile}";
            if (!File.Exists(sourceFile)) return false;

            string luaContent = File.ReadAllText(sourceFile);
            if (mission != null) // A mission was provided, do the required replacements in the file.
                luaContent = mission.ReplaceValues(luaContent);
            luaContent = BriefingRoom.LanguageDB.ReplaceValues(luaContent);

            foreach (Match match in Regex.Matches(luaContent, "\\$.*?\\$"))
                BriefingRoom.PrintToLog($"Found a non-assigned value ({match.Value}) in Lua file \"{mizEntryKey}\".", LogMessageErrorLevel.Info);
            luaContent = Regex.Replace(luaContent, "\\$.*?\\$", "0");

            mizFileEntries.Add(mizEntryKey, Encoding.UTF8.GetBytes(luaContent));
            return true;
        }

        private static bool AddStringValueToEntries(Dictionary<string, byte[]> mizFileEntries, string mizEntryKey, string stringValue)
        {
            if (string.IsNullOrEmpty(mizEntryKey) || mizFileEntries.ContainsKey(mizEntryKey)) return false;
            mizFileEntries.Add(mizEntryKey, Encoding.UTF8.GetBytes(stringValue));
            return true;
        }


    }
}