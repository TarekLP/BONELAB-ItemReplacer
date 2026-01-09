using MelonLoader;
using ItemReplacer;
using System.Reflection;

#region MelonLoader
[assembly: MelonInfo(typeof(Core), ModInfo.Name, ModInfo.Version, ModInfo.Author, ModInfo.DownloadLink)]
[assembly: MelonColor(0, 153, 0, 204)]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]
#endregion

#region Assembly Info
[assembly: AssemblyTitle(ModInfo.Name)]
[assembly: AssemblyDescription(ModInfo.Description)]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(ModInfo.Author)]
[assembly: AssemblyProduct(ModInfo.Name)]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
#endregion

#region Assembly Version
[assembly: AssemblyVersion(ModInfo.Version)]
[assembly: AssemblyFileVersion(ModInfo.Version)]
[assembly: AssemblyInformationalVersion(ModInfo.Version)]
#endregion
