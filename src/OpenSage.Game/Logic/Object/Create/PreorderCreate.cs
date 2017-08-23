﻿using OpenSage.Data.Ini.Parser;

namespace OpenSage.Logic.Object
{
    /// <summary>
    /// Allows the use of the PREORDER ConditionState with this object which in turn is only 
    /// triggered by the presence of registry key 'Preorder' set to '1' in 
    /// HKLM\Software\ElectronicArts\EAGames\Generals.
    /// </summary>
    public sealed class PreorderCreate : ObjectBehavior
    {
        internal static PreorderCreate Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<PreorderCreate> FieldParseTable = new IniParseTable<PreorderCreate>();
    }
}