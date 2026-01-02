using System.Reflection;

using MelonLoader;

using RexmeckItemReplacer;

#region MelonLoader
[assembly: MelonInfo(typeof(Core), ModInfo.Name, ModInfo.Version, ModInfo.Company, ModInfo.DownloadLink)]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]
#endregion

#region Assembly Info
[assembly: AssemblyTitle(ModInfo.Name)]
[assembly: AssemblyDescription(ModInfo.Description)]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(ModInfo.Company)]
[assembly: AssemblyProduct(ModInfo.Name)]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
#endregion

#region Assembly Version

[assembly: AssemblyVersion(ModInfo.Version)]
[assembly: AssemblyFileVersion(ModInfo.Version)]
[assembly: AssemblyInformationalVersion(ModInfo.Version)]


#endregion
