using System;

using Il2CppCysharp.Threading.Tasks;

using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;

namespace ItemReplacer.Utilities
{
    internal static class Fusion
    {
        public static bool HasFusion => Core.FindMelon("LabFusion", "Lakatrazz") != null;
        public static bool IsConnected
        {
            get
            {
                if (HasFusion) return Internal_IsConnected();
                else return false;
            }
        }

        public static bool IsHost
        {
            get
            {
                if (IsConnected) return Internal_IsHost();
                else return false;
            }
        }

        internal static bool Internal_IsConnected()
        {
            return LabFusion.Network.NetworkInfo.HasServer;
        }

        internal static bool Internal_IsHost()
        {
            return LabFusion.Network.NetworkInfo.IsHost;
        }

        public static void NetworkSpawnSpawnable(string barcode, CrateSpawner spawner, UniTaskCompletionSource<Poolee> source)
        {
            if (IsHost) Internal_NetworkSpawnSpawnable(barcode, spawner, source);
        }

        private static void Internal_NetworkSpawnSpawnable(string barcode, CrateSpawner spawner, UniTaskCompletionSource<Poolee> source)
        {
            var spawnable = new Spawnable()
            {
                crateRef = new SpawnableCrateReference(barcode),
                policyData = null
            };

            if (spawnable?.crateRef.IsValid() != true)
                return;


            var transform = spawner.transform;



            LabFusion.RPC.NetworkAssetSpawner.Spawn(new LabFusion.RPC.NetworkAssetSpawner.SpawnRequestInfo()
            {
                Spawnable = spawnable,
                Position = transform.position,
                Rotation = transform.rotation,
                SpawnCallback = (info) => OnNetworkSpawn(spawner, info, source),
            });
        }

        private static void OnNetworkSpawn(CrateSpawner spawner, object _info, UniTaskCompletionSource<Poolee> source)
        {
            if (_info is not LabFusion.RPC.NetworkAssetSpawner.SpawnCallbackInfo info)
                return;


            // In the event that the CrateSpawner was part of a now destroyed GameObject, null check
            if (spawner == null)
                return;


            var spawned = info.Spawned;

            var poolee = Poolee.Cache.Get(spawned);

            source.TrySetResult(poolee);

            // Make sure we actually have a network entity
            if (info.Entity == null)
                LabFusion.Marrow.Patching.CrateSpawnerPatches.OnFinishNetworkSpawn(spawner, info.Spawned);

            // Send spawn message
            var spawnedID = info.Entity.ID;

            LabFusion.Marrow.Messages.CrateSpawnerMessage.SendCrateSpawnerMessage(spawner, spawnedID);

        }
        internal static bool HandleFusionCrateSpawner(string barcode, CrateSpawner spawner, out UniTask<Poolee> res)
        {
            res = null;
            // If this scene is unsynced, the spawner can function as normal.
            if (!LabFusion.Scene.NetworkSceneManager.IsLevelNetworked)
                return true;


            if (IsSingleplayerOnly(spawner))
                return true;


            // If we don't own the CrateSpawner, don't allow a spawn from it
            if (!HasOwnership(spawner))
            {
                res = new UniTask<Poolee>(null);
                return false;
            }

            var source = new UniTaskCompletionSource<Poolee>();
            res = new UniTask<Poolee>(source.TryCast<IUniTaskSource<Poolee>>(), default);

            try
            {
                NetworkSpawnSpawnable(barcode, spawner, source);
            }
            catch (Exception e)
            {
                Core.Logger.Error($"networking CrateSpawner {spawner.name}", e);
            }

            return false;
        }

        private static bool HasOwnership(CrateSpawner spawner)
        {
            if (LabFusion.Marrow.Extenders.CrateSpawnerExtender.Cache.TryGet(spawner, out var networkEntity))
            {
                return networkEntity.IsOwner;
            }

            return LabFusion.Scene.NetworkSceneManager.IsLevelHost;
        }

        private static bool IsSingleplayerOnly(CrateSpawner crateSpawner)
        {
            // Check if this CrateSpawner has a Desyncer
            if (LabFusion.Marrow.Integration.Desyncer.Cache.ContainsSource(crateSpawner.gameObject))
                return true;


            var spawnable = crateSpawner._spawnable;

            if (spawnable == null)
                return false;


            if (!spawnable.crateRef.IsValid() || spawnable.crateRef.Crate == null)
                return false;


            // Check for the Singleplayer Only tag
            return LabFusion.Marrow.CrateFilterer.HasTags(spawnable.crateRef.Crate, LabFusion.Marrow.FusionTags.SingleplayerOnly);
        }
    }
}
