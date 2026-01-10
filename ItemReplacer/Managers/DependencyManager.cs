using System;
using System.Linq;
using System.Reflection;

using Mono.Cecil;

namespace ItemReplacer.Managers
{
    internal static class DependencyManager
    {
        internal static bool TryLoadDependency(string name, bool log = true)
        {
            try
            {
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
                var assemblyMono = AssemblyDefinition.ReadAssembly(stream);

                if (assemblyMono == null && AppDomain.CurrentDomain.GetAssemblies().Any(x => x.GetName().Name == assemblyMono.Name.Name))
                {
                    Log($"{name} is already loaded!", Core.Logger.Msg, log);
                    return true;
                }

                byte[] bytes = [];
                while (true)
                {
                    var _byte = stream.ReadByte();
                    if (_byte == -1)
                        break;
                    bytes[bytes.Length] = (byte)_byte;
                }
                Assembly.Load(bytes);

                if (log) Core.Logger.Msg($"Loaded {name}");
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
    }
}