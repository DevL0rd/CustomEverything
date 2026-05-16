using System;
using System.IO;
using System.Threading.Tasks;
using CustomEverything.App;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.Store;
using HarmonyLib;

namespace CustomEverything.Inspectors;

internal static class CustomInspectorLoader
{
    private static readonly AccessTools.FieldRef<SceneInspector, SyncRef<Sync<string>>> RootTextField =
        AccessTools.FieldRefAccess<SceneInspector, SyncRef<Sync<string>>>("_rootText");

    private static readonly AccessTools.FieldRef<SceneInspector, SyncRef<Sync<string>>> ComponentTextField =
        AccessTools.FieldRefAccess<SceneInspector, SyncRef<Sync<string>>>("_componentText");

    private static readonly AccessTools.FieldRef<SceneInspector, SyncRef<Slot>> HierarchyContentRootField =
        AccessTools.FieldRefAccess<SceneInspector, SyncRef<Slot>>("_hierarchyContentRoot");

    private static readonly AccessTools.FieldRef<SceneInspector, SyncRef<Slot>> ComponentsContentRootField =
        AccessTools.FieldRefAccess<SceneInspector, SyncRef<Slot>>("_componentsContentRoot");

    internal static async Task<bool> TryLoad(SceneInspector inspector, Uri sourceUri)
    {
        float3? restorePosition = null;
        floatQ? restoreRotation = null;
        float3? restoreScale = null;

        try
        {
            await default(ToBackground);

            var assetUri = await ResolveAssetUri(sourceUri);
            if (assetUri == null)
                return false;

            var assetFile = await Engine.Current.AssetManager.GatherAssetFile(assetUri, 20, null);
            if (assetFile == null || !File.Exists(assetFile))
                return false;

            var node = DataTreeConverter.Load(assetFile, assetUri);
            if (!PrepareInspectorData(inspector, node, out var translator))
                return false;

            await default(ToWorld);
            if (inspector.Slot.IsDestroyed)
                return false;

            restorePosition = inspector.Slot.GlobalPosition;
            restoreRotation = inspector.Slot.GlobalRotation;
            restoreScale = inspector.Slot.GlobalScale;

            inspector.Slot.LoadObject(node, null, refTranslator: translator);
            var loadedInspector = inspector.Slot.GetComponent<SceneInspector>(candidate => candidate != inspector);
            if (loadedInspector == null)
                return false;

            TransferInspectorReferences(from: loadedInspector, to: inspector);
            loadedInspector.Destroy(false);

            inspector.Enabled = true;
            inspector.Slot.GlobalPosition = restorePosition.Value;
            inspector.Slot.GlobalRotation = restoreRotation.Value;
            inspector.Slot.GlobalScale = restoreScale.Value;
            return true;
        }
        catch (Exception ex)
        {
            CustomEverythingLog.Error($"Failed to load custom inspector: {ex}");
            RestoreTransform(inspector, restorePosition, restoreRotation, restoreScale);
            return false;
        }
    }

    private static async Task<Uri> ResolveAssetUri(Uri uri)
    {
        if (uri.Scheme != Engine.Current.Cloud.Platform.RecordScheme)
            return uri;

        var record = await Engine.Current.Cloud.Records.GetRecordCached<Record>(uri, null);
        return record.IsError ? null : new Uri(record.Entity.AssetURI);
    }

    private static bool PrepareInspectorData(SceneInspector inspector, DataTreeDictionary node, out ReferenceTranslator translator)
    {
        translator = new ReferenceTranslator();
        if (node == null)
            return false;

        var root = GetRootObject(node);
        var components = root?.TryGetDictionary("Components")?.TryGetList("Data");
        if (components == null)
            return false;

        var usesTypeTable = node.TryGetDictionary("FeatureFlags")?.TryGetNode("TypeManagement") != null;
        foreach (var componentNode in components.Children)
        {
            if (componentNode is not DataTreeDictionary component ||
                !IsSceneInspectorComponent(inspector.World, node, component, usesTypeTable))
                continue;

            var data = component.TryGetDictionary("Data");
            if (data == null)
                return false;

            MergeInspectorReferences(inspector, data, translator);
            return true;
        }

        return false;
    }

    private static DataTreeDictionary GetRootObject(DataTreeDictionary node)
    {
        var root = node.TryGetDictionary("Object");
        if (root?.TryGetDictionary("Name")?.TryGetNode("Data").LoadString() != "Holder")
            return root;

        if (root.TryGetList("Children")?.Children[0] is not DataTreeDictionary child)
            return root;

        node.Children["Object"] = child;
        return child;
    }

    private static bool IsSceneInspectorComponent(World world, DataTreeDictionary node, DataTreeDictionary component, bool usesTypeTable)
    {
        var typeNode = component.TryGetNode("Type");
        if (typeNode == null)
            return false;

        if (!usesTypeTable)
            return typeNode.LoadString() == typeof(SceneInspector).ToString();

        var typeIndex = typeNode.LoadInt();
        var types = node.TryGetList("Types");
        return types != null &&
               types.Count > typeIndex &&
               world.Types.DecodeType(types[typeIndex].LoadString()) == typeof(SceneInspector);
    }

    private static void MergeInspectorReferences(SceneInspector inspector, DataTreeDictionary data, ReferenceTranslator translator)
    {
        translator.Associate(inspector.ReferenceID, new Guid(data.TryGetNode("ID").LoadString()));
        data.Children["ID"] = new DataTreeValue(Guid.NewGuid().ToString());

        inspector.ForeachSyncMember<IWorldElement>(member =>
        {
            var memberData = data.TryGetDictionary(member.Name);
            var memberId = memberData?.TryGetNode("ID").LoadString();
            if (memberId == null)
                return;

            translator.Associate(member.ReferenceID, new Guid(memberId));
            memberData.Children["ID"] = new DataTreeValue(Guid.NewGuid().ToString());
        });
    }

    private static void TransferInspectorReferences(SceneInspector from, SceneInspector to)
    {
        RootTextField(to).Target = RootTextField(from).Target;
        ComponentTextField(to).Target = ComponentTextField(from).Target;
        HierarchyContentRootField(to).Target = HierarchyContentRootField(from).Target;
        ComponentsContentRootField(to).Target = ComponentsContentRootField(from).Target;
    }

    private static void RestoreTransform(SceneInspector inspector, float3? position, floatQ? rotation, float3? scale)
    {
        if (inspector.IsRemoved || inspector.Slot.IsRemoved)
            return;

        if (position.HasValue)
            inspector.Slot.GlobalPosition = position.Value;
        if (rotation.HasValue)
            inspector.Slot.GlobalRotation = rotation.Value;
        if (scale.HasValue)
            inspector.Slot.GlobalScale = scale.Value;
    }
}
