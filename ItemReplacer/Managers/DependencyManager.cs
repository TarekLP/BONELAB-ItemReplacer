using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Mono.Cecil;

namespace ItemReplacer.Managers
{
    internal static class DependencyManager
    {
        internal static void LoadDependencies(bool log = true)
        {
            var assembly = Assembly.GetExecutingAssembly();

            if (assembly == null)
            {
                Log("Executing assembly was somehow not found, cannot not load dependencies", Core.Logger.Error, log);
                return;
            }

            var assemblyInfo = assembly.GetName();

            if (assemblyInfo == null)
            {
                Log("Assembly Info was not found, cannot not load dependencies", Core.Logger.Error, log);
                return;
            }

            var names = assembly.GetManifestResourceNames();

            var dependencies = names?.Where(x => x.StartsWith($"{assemblyInfo.Name}.Dependencies.") && x.EndsWith(".dll")).ToList();
            dependencies.ForEach(x => TryLoadDependency(x.Replace($"{assemblyInfo.Name}.Dependencies.", string.Empty).Replace(".dll", ""), log));
        }

        internal static bool TryLoadDependency(string name, bool log = true)
        {
            try
            {
                Log($"Attempting to load dependency: {name}", Core.Logger.Msg, log);

                var assembly = Assembly.GetExecutingAssembly();

                if (assembly == null)
                {
                    Log($"Executing assembly was somehow not found, cannot not load {name}", Core.Logger.Error, log);
                    return false;
                }

                var assemblyInfo = assembly.GetName();

                if (assemblyInfo == null)
                {
                    Log($"Assembly Info was not found, cannot not load {name}", Core.Logger.Error, log);
                    return false;
                }

                var _path = $"{assemblyInfo.Name}.Dependencies.{name}.dll";
                var names = assembly.GetManifestResourceNames();
                if (names?.Contains(_path) != true)
                {
                    if (log) Core.Logger.Error($"There were no embedded resources or dependency was not found in the list of embedded resources, cannot not load {name}");
                    return false;
                }

                var stream = assembly.GetManifestResourceStream(_path);
                if (stream == null || stream.Length == 0)
                {
                    Log($"Could not get stream of {name}, cannot not load it", Core.Logger.Error, log);
                    return false;
                }

                stream.Position = 0;

                var bytes = stream.ToByteArray();

                using (var memStream = new MemoryStream(bytes))
                using (var assemblyMono = AssemblyDefinition.ReadAssembly(memStream))
                {
                    if (assemblyMono == null && AppDomain.CurrentDomain.GetAssemblies().Any(x => x.GetName().Name == assemblyMono.Name.Name))
                    {
                        Log($"{name} is already loaded!", Core.Logger.Msg, log);
                        return true;
                    }
                }
                Assembly.Load(bytes);

                Log($"Loaded {name}", Core.Logger.Msg, log);
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"An unexpected error occurred while loading {name}", ex);
                return false;
            }
            return true;
        }

        internal static void Log(string message, Action<string> action, bool print = true)
        {
            if (print)
                action(message);
        }

        internal static byte[] ToByteArray(this Stream stream)
        {
            using MemoryStream ms = new();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}