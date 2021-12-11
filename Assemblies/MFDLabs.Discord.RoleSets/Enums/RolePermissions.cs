using System;
using System.Collections.Generic;

namespace MFDLabs.Discord.RoleSets.Enums
{
    public static class RoleSetEnumExtensions
    {
        public static string[] PermissionsToArray(this RolePermissions permissions)
        {
            var flags = new List<string>();

            foreach (var item in Enum.GetNames(typeof(RolePermissions)))
            {
                Enum.TryParse<RolePermissions>(item, out var permissionItem);
                if ((permissions & permissionItem) == 0) flags.Add(item);
            }

            return flags.ToArray();
        }
    }

    [Flags]
    public enum RolePermissions : byte
    {
    }
}
