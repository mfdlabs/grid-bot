using System.Security.Principal;

namespace MFDLabs.Diagnostics.Extensions
{
    public static class WindowsIdentityExtensions
    {
        public static bool IsAdministrator(this WindowsIdentity id) => new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
