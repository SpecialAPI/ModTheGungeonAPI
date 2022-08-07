using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using AttachPoint = tk2dSpriteDefinition.AttachPoint;
using UnityEngine;
using System.IO;
using BepInEx;

public static partial class ETGMod
{
    /// <summary>
    /// Returns the path to a plugin's folder.
    /// </summary>
    /// <param name="plug">The plugin to get the folder path of.</param>
    /// <returns>The path to a plugin's folder.</returns>
    public static string FolderPath(this BaseUnityPlugin plug)
    {
        return Path.Combine(plug.Info.Location, "..");
    }

    /// <summary>
    /// ETGMod asset management.
    /// </summary>
    public static partial class Assets
    {
        private static List<tk2dSpriteCollectionData> _spriteCollections;
        private static readonly List<string> processedFolders = new();
        internal static readonly Dictionary<string, Texture2D> unprocessedSingleTextureReplacements = new();
        internal static readonly Dictionary<string, Dictionary<string, Texture2D>> unprocessedReplacements = new();
        internal static readonly Dictionary<string, Dictionary<string, AssetSpriteData>> unprocessedJsons = new();

        /// <summary>
        /// All loaded sprite collections.
        /// </summary>
        public static List<tk2dSpriteCollectionData> Collections => _spriteCollections ??= Resources.FindObjectsOfTypeAll<tk2dSpriteCollectionData>().Where(x => x?.gameObject != null).ToList();//x.gameObject.scene.name == null

        /// <summary>
        /// A dictionary where the keys are the paths to textures loaded through SetupSprites methods and the values are the textures themselves.
        /// </summary>
        public static Dictionary<string, Texture2D> TextureMap = new();

        /// <summary>
        /// The instance RuntimeAtlasPacker that is used for creating new spritesheets.
        /// </summary>
        public static RuntimeAtlasPacker Packer = new();

        /// <summary>
        /// Creates UVs for a sprite definition.
        /// </summary>
        /// <param name="texture">The spritesheet texture.</param>
        /// <param name="x">The x coordinate of your target texture's bottom left corner.</param>
        /// <param name="y">The y coordinate of your target texture's bottom left corner.</param>
        /// <param name="width">The width of your target texture.</param>
        /// <param name="height">The height of your target texture.</param>
        /// <returns>The created UVs.</returns>
        public static Vector2[] GenerateUVs(Texture2D texture, int x, int y, int width, int height)
        {
            return new Vector2[] {
                new Vector2((x        ) / (float) texture.width, (y         ) / (float) texture.height),
                new Vector2((x + width) / (float) texture.width, (y         ) / (float) texture.height),
                new Vector2((x        ) / (float) texture.width, (y + height) / (float) texture.height),
                new Vector2((x + width) / (float) texture.width, (y + height) / (float) texture.height),
            };
        }

        /// <summary>
        /// Sets up sprites from a folder, adding sprites to collections or replacing existing sprites.
        /// </summary>
        /// <param name="folder">The root folder for the sprite setup.</param>
        public static void SetupSpritesFromFolder(string folder)
        {
            if (processedFolders.Contains(folder))
            {
                return;
            }
            if (!Directory.Exists(folder))
            {
                ETGModConsole.Log($"Part of the path to {folder} is missing!");
                return;
            }
            foreach(var image in Directory.GetFiles(folder, "*.png"))
            {
                var trimmed = image.Substring(image.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                if (trimmed.LastIndexOf(".") >= 0)
                {
                    trimmed = trimmed.Substring(0, trimmed.LastIndexOf("."));
                }
                Texture2D tex = new(1, 1, TextureFormat.RGBA32, false);
                try
                {
                    tex.LoadImage(File.ReadAllBytes(image));
                }
                catch { }
                tex.filterMode = FilterMode.Point;
                var coll = Collections.Find(x => x?.spriteCollectionName != null && x.spriteCollectionName.ToLowerInvariant() == trimmed.ToLowerInvariant());
                if (coll == null || coll.materials == null || coll.materials.Length == 0)
                {
                    if (coll == null)
                    {
                        var cname = trimmed.Replace("_", " ");
                        if (!unprocessedSingleTextureReplacements.ContainsKey(cname) || unprocessedSingleTextureReplacements[cname] == null)
                        {
                            unprocessedSingleTextureReplacements[cname] = tex;
                        }
                    }
                    continue;
                }

                var material = coll.materials[0];
                if (!material)
                    continue;

                Texture mainTexture = material.mainTexture;
                if (!mainTexture)
                    continue;

                string atlasName = mainTexture.name;
                if (string.IsNullOrEmpty(atlasName))
                    continue;

                if (atlasName[0] == '~')
                    continue;
                Dictionary<int, Texture> textureCache = new();
                if (coll.spriteDefinitions != null)
                {
                    for (int i = 0; i < coll.spriteDefinitions.Length; i++)
                    {
                        var def = coll.spriteDefinitions[i];
                        var addToCache = def.materialInst != null;
                        foreach (var mat in coll.materials)
                        {
                            if (def.materialInst == null || def.materialInst.mainTexture == mat.mainTexture)
                            {
                                addToCache = false;
                            }
                        }
                        if (addToCache)
                        {
                            textureCache[i] = def.materialInst.mainTexture;
                        }
                    }
                }
                tex.name = '~' + atlasName;
                for (int i = 0; i < coll.materials.Length; i++)
                {
                    if (coll.materials[i]?.mainTexture == null)
                        continue;

                    coll.materials[i].mainTexture = tex;
                }
                coll.inst.materialInsts = null;
                coll.inst.Init();
                var instIsNew = coll.inst != coll;
                foreach (var kvp in textureCache)
                {
                    if (kvp.Key < coll.Count)
                    {
                        var def = coll.spriteDefinitions[kvp.Key];
                        if (def != null && def.materialInst != null)
                        {
                            def.materialInst = new(def.material);
                            def.materialInst.mainTexture = kvp.Value;
                        }
                    }
                }
                if (instIsNew)
                {
                    if (coll.inst?.materials != null)
                    {
                        for (int i = 0; i < coll.inst.materials.Length; i++)
                        {
                            if (coll.inst.materials[i]?.mainTexture == null)
                                continue;

                            coll.inst.materials[i].mainTexture = tex;
                        }
                    }
                    foreach (var kvp in textureCache)
                    {
                        if (kvp.Key < coll.inst.Count)
                        {
                            var def = coll.inst.spriteDefinitions[kvp.Key];
                            if (def != null && def.materialInst != null)
                            {
                                def.materialInst.mainTexture = kvp.Value;
                            }
                        }
                    }
                }
            }
            foreach (var collection in Directory.GetDirectories(folder))
            {
                var trimmed = "";
                if (collection.LastIndexOf(Path.DirectorySeparatorChar) + 1 == collection.Length)
                {
                    trimmed = collection.Substring(0, trimmed.LastIndexOf(Path.DirectorySeparatorChar));
                    if (trimmed.LastIndexOf(Path.DirectorySeparatorChar) != trimmed.Length)
                    {
                        trimmed = trimmed.Substring(trimmed.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                    }
                }
                else
                {
                    trimmed = collection.Substring(collection.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                }
                var coll = Collections.Find(x => x?.spriteCollectionName != null && x.spriteCollectionName.ToLowerInvariant() == trimmed.ToLowerInvariant());
                if (coll?.spriteDefinitions != null && coll.Count > 0) //no point in going though the sprites if they mean nothing
                {
                    List<tk2dSpriteDefinition> newSprites = new(coll.spriteDefinitions);
                    List<tk2dSpriteDefinition> newSpriteInst = new(coll.inst.spriteDefinitions);
                    var instIsNew = coll.inst != coll;
                    foreach(var j in Directory.GetFiles(collection, "*.json"))
                    {
                        var trimmedExtension = j.Substring(0, j.LastIndexOf("."));
                        var defName = trimmedExtension.Substring(trimmedExtension.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                        var id = coll.GetSpriteIdByName(defName, -1);
                        if(id < 0)
                        {
                            continue;
                        }
                        using var stream = File.OpenRead(j);
                        AssetSpriteData frameData = default;
                        try
                        {
                            frameData = JSONHelper.ReadJSON<AssetSpriteData>(stream);
                        }
                        catch { ETGModConsole.Log("Error: invalid json at path " + j); continue; }
                        coll.SetAttachPoints(id, frameData.attachPoints);
                        if (instIsNew)
                        {
                            coll.inst.SetAttachPoints(id, frameData.attachPoints);
                        }
                    }
                    foreach (var image in Directory.GetFiles(collection, "*.png"))
                    {
                        var trimmedExtension = image.Substring(0, image.LastIndexOf("."));
                        var defName = trimmedExtension.Substring(trimmedExtension.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                        Texture2D tex = new(1, 1, TextureFormat.RGBA32, false);
                        try
                        {
                            tex.LoadImage(File.ReadAllBytes(image));
                        }
                        catch { }
                        var id = -1;
                        var existingIdx = coll.GetSpriteIdByName(defName, -1);
                        if (existingIdx >= 0)
                        {
                            coll.spriteDefinitions[existingIdx].ReplaceTexture(tex);
                            if (instIsNew)
                            {
                                coll.inst.spriteDefinitions[existingIdx].ReplaceTexture(tex);
                            }
                            id = existingIdx;
                        }
                        else
                        {
                            var frame = new tk2dSpriteDefinition
                            {
                                name = defName,
                                material = coll.materials[0]
                            }; // if youre reading this, FROG
                            frame.ReplaceTexture(tex);

                            frame.normals = new Vector3[0];
                            frame.tangents = new Vector4[0];
                            frame.indices = new int[] { 0, 3, 1, 2, 3, 0 };
                            const float pixelScale = 0.0625f;
                            float w = tex.width * pixelScale;
                            float h = tex.height * pixelScale;
                            frame.position0 = new Vector3(0f, 0f, 0f);
                            frame.position1 = new Vector3(w, 0f, 0f);
                            frame.position2 = new Vector3(0f, h, 0f);
                            frame.position3 = new Vector3(w, h, 0f);
                            frame.boundsDataCenter = frame.untrimmedBoundsDataCenter = new Vector3(w / 2f, h / 2f, 0f);
                            frame.boundsDataExtents = frame.untrimmedBoundsDataExtents = new Vector3(w, h, 0f);
                            id = newSprites.Count;
                            if (File.Exists(trimmedExtension + ".json"))
                            {
                                using var stream = File.OpenRead(trimmedExtension + ".json");
                                AssetSpriteData frameData = default;
                                try
                                {
                                    frameData = JSONHelper.ReadJSON<AssetSpriteData>(stream);
                                }
                                catch { ETGModConsole.Log("Error: invalid json at path " + trimmedExtension + ".json"); continue; }
                                coll.SetAttachPoints(id, frameData.attachPoints);
                                if (instIsNew)
                                {
                                    coll.inst.SetAttachPoints(id, frameData.attachPoints);
                                }
                            }
                            newSprites.Add(frame);
                            newSpriteInst.Add(frame);
                        }
                        var mapName = $"sprite/{trimmed}/{defName}";
                        if (TextureMap.ContainsKey(mapName))
                        {
                            TextureMap[mapName] = tex;
                        }
                        else
                        {
                            TextureMap.Add(mapName, tex);
                        }
                    }
                    coll.spriteDefinitions = newSprites.ToArray();
                    coll.spriteNameLookupDict = null;
                    if (instIsNew)
                    {
                        coll.inst.spriteDefinitions = newSpriteInst.ToArray();
                        coll.inst.spriteNameLookupDict = null;
                    }
                }
                else if(coll == null)
                {
                    if(!unprocessedReplacements.TryGetValue(trimmed.Replace("_", " "), out var replacement))
                    {
                        replacement = new();
                        unprocessedReplacements.Add(trimmed.Replace("_", " "), replacement);
                    }
                    if (!unprocessedJsons.TryGetValue(trimmed.Replace("_", " "), out var json))
                    {
                        json = new();
                        unprocessedJsons.Add(trimmed.Replace("_", " "), json);
                    }
                    foreach (var image in Directory.GetFiles(collection, "*.png"))
                    {
                        var trimmedExtension = image.Substring(0, image.LastIndexOf("."));
                        var defName = trimmedExtension.Substring(trimmedExtension.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                        Texture2D tex = new(1, 1, TextureFormat.RGBA32, false);
                        try
                        {
                            tex.LoadImage(File.ReadAllBytes(image));
                        }
                        catch { }
                        replacement[defName] = tex;
                    }
                    foreach (var j in Directory.GetFiles(collection, "*.json"))
                    {
                        var trimmedExtension = j.Substring(0, j.LastIndexOf("."));
                        var defName = trimmedExtension.Substring(trimmedExtension.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                        using var stream = File.OpenRead(j);
                        AssetSpriteData frameData = default;
                        try
                        {
                            frameData = JSONHelper.ReadJSON<AssetSpriteData>(stream);
                        }
                        catch { ETGModConsole.Log("Error: invalid json at path " + j); continue; }
                        json[defName] = frameData;
                    }
                }
            }
            processedFolders.Add(folder);
        }

        /// <summary>
        /// Sets up sprites from an assembly's embedded resources, adding sprites to collections or replacing existing sprites.
        /// </summary>
        /// <param name="asmb">The assembly to search.</param>
        /// <param name="path">The root path to the folder in the assembly's embedded resources.</param>
        public static void SetupSpritesFromAssembly(Assembly asmb, string path)
        {
            if(asmb == null)
            {
                return;
            }
            path = path.Replace("/", ".").Replace("\\", ".");
            if (!path.EndsWith("."))
            {
                path += ".";
            }
            string cachedCollectionName = null;
            tk2dSpriteCollectionData coll = null;
            List<tk2dSpriteDefinition> newSprites = null;
            List<tk2dSpriteDefinition> newSpriteInst = null;
            List<string> singleTextureReplacements = new();
            List<string> replacements = new();
            List<string> jsons = new();
            var resources = asmb.GetManifestResourceNames();
            foreach (var resource in resources)
            {
                if (resource.StartsWith(path) && resource.Length > path.Length)
                {
                    var names = resource.Substring(path.LastIndexOf(".") + 1).Split('.');
                    if (names.Length == 3)
                    {
                        var extension = names[2];
                        if (extension.ToLowerInvariant() == "png")
                        {
                            replacements.Add(resource);
                        }
                        else if(extension.ToLowerInvariant() == "json")
                        {
                            jsons.Add(resource);
                        }
                    }
                    else if(names.Length == 2)
                    {
                        var extension = names[1];
                        if(extension.ToLowerInvariant() == "png")
                        {
                            singleTextureReplacements.Add(resource);
                        }
                    }
                }
            }
            foreach(var resource in singleTextureReplacements)
            {
                Texture2D tex = new(1, 1, TextureFormat.RGBA32, false);
                using (var s = asmb.GetManifestResourceStream(resource))
                {
                    try
                    {
                        var b = new byte[s.Length];
                        s.Read(b, 0, b.Length);
                        tex.LoadImage(b);
                    }
                    catch { }
                }
                tex.filterMode = FilterMode.Point;
                var names = resource.Substring(path.LastIndexOf(".") + 1).Split('.');
                var collection = names[0];
                var spriteCollection = Collections.Find(x => x?.spriteCollectionName != null && x.spriteCollectionName.Replace(" ", "_").ToLowerInvariant() == collection.ToLowerInvariant());
                if (spriteCollection == null || spriteCollection.materials == null || spriteCollection.materials.Length == 0)
                {
                    if (spriteCollection == null)
                    {
                        var cname = collection.Replace("_", " ");
                        if (!unprocessedSingleTextureReplacements.ContainsKey(cname) || unprocessedSingleTextureReplacements[cname] == null)
                        {
                            unprocessedSingleTextureReplacements[cname] = tex;
                        }
                    }
                    continue;
                }

                var material = spriteCollection.materials[0];
                if (!material)
                    continue;

                Texture mainTexture = material.mainTexture;
                if (!mainTexture)
                    continue;

                string atlasName = mainTexture.name;
                if (string.IsNullOrEmpty(atlasName))
                    continue;

                if (atlasName[0] == '~')
                    continue;
                Dictionary<int, Texture> textureCache = new();
                if(spriteCollection.spriteDefinitions != null)
                {
                    for (int i = 0; i < spriteCollection.spriteDefinitions.Length; i++)
                    {
                        var def = spriteCollection.spriteDefinitions[i];
                        var addToCache = def.materialInst != null;
                        foreach(var mat in spriteCollection.materials)
                        {
                            if (def.materialInst == null || def.materialInst.mainTexture == mat.mainTexture)
                            {
                                addToCache = false;
                            }
                        }
                        if (addToCache)
                        {
                            textureCache[i] = def.materialInst.mainTexture;
                        }
                    }
                }
                tex.name = '~' + atlasName;
                for (int i = 0; i < spriteCollection.materials.Length; i++)
                {
                    if (spriteCollection.materials[i]?.mainTexture == null)
                        continue;

                    spriteCollection.materials[i].mainTexture = tex;
                }
                spriteCollection.inst.materialInsts = null;
                spriteCollection.inst.Init();
                var instIsNew = spriteCollection.inst != spriteCollection;
                foreach(var kvp in textureCache)
                {
                    if(kvp.Key < spriteCollection.Count)
                    {
                        var def = spriteCollection.spriteDefinitions[kvp.Key];
                        if(def != null && def.materialInst != null)
                        {
                            def.materialInst.mainTexture = kvp.Value;
                        }
                    }
                }
                if (instIsNew)
                {
                    if (spriteCollection.inst?.materials != null)
                    {
                        for (int i = 0; i < spriteCollection.inst.materials.Length; i++)
                        {
                            if (spriteCollection.inst.materials[i]?.mainTexture == null)
                                continue;

                            spriteCollection.inst.materials[i].mainTexture = tex;
                        }
                    }
                    foreach (var kvp in textureCache)
                    {
                        if (kvp.Key < spriteCollection.inst.Count)
                        {
                            var def = spriteCollection.inst.spriteDefinitions[kvp.Key];
                            if (def != null && def.materialInst != null)
                            {
                                def.materialInst.mainTexture = kvp.Value;
                            }
                        }
                    }
                }
            }
            foreach (var resource in jsons)
            {
                var names = resource.Substring(path.LastIndexOf(".") + 1).Split('.');
                var collection = names[0];
                var name = names[1];
                if (collection != cachedCollectionName) // only get a new collection if the name changes
                {
                    if (coll != null && newSprites != null)
                    {
                        coll.spriteDefinitions = newSprites.ToArray();
                        newSprites = null;
                        if (coll.inst != coll && newSpriteInst != null)
                        {
                            coll.inst.spriteDefinitions = newSpriteInst.ToArray();
                            newSpriteInst = null;
                            coll.inst.spriteNameLookupDict = null;
                        }
                        coll.spriteNameLookupDict = null;
                    }
                    coll = Collections.Find(x => x?.spriteCollectionName != null && x.spriteCollectionName.Replace(" ", "_").ToLowerInvariant() == collection.ToLowerInvariant());
                    if (coll?.spriteDefinitions != null)
                    {
                        newSprites = new(coll.spriteDefinitions);
                        newSpriteInst = new(coll.inst.spriteDefinitions);
                    }
                    cachedCollectionName = collection;
                }
                if (coll?.spriteDefinitions != null && coll.Count > 0) //no point in going though the sprites if they mean nothing
                {
                    var id = coll.GetSpriteIdByName(name, -1);
                    if (id < 0)
                    {
                        continue;
                    }
                    using var stream = asmb.GetManifestResourceStream(resource);
                    AssetSpriteData frameData = default;
                    try
                    {
                        frameData = JSONHelper.ReadJSON<AssetSpriteData>(stream);
                    }
                    catch { ETGModConsole.Log("Error: invalid json at project path " + resource); continue; }
                    coll.SetAttachPoints(id, frameData.attachPoints);
                    if (coll.inst != coll)
                    {
                        coll.inst.SetAttachPoints(id, frameData.attachPoints);
                    }
                }
                else if(coll == null)
                {
                    using var stream = asmb.GetManifestResourceStream(resource);
                    AssetSpriteData frameData = default;
                    try
                    {
                        frameData = JSONHelper.ReadJSON<AssetSpriteData>(stream);
                    }
                    catch { ETGModConsole.Log("Error: invalid json at project path " + resource); continue; }
                    if (!unprocessedJsons.TryGetValue(collection.Replace("_", " "), out var json))
                    {
                        json = new();
                        unprocessedJsons.Add(collection.Replace("_", " "), json);
                    }
                    json.Add(name, frameData);
                }
            }
            foreach (var resource in replacements)
            {
                var names = resource.Substring(path.LastIndexOf(".") + 1).Split('.');
                var collection = names[0];
                var name = names[1];
                if (collection != cachedCollectionName) // only get a new collection if the name changes
                {
                    if (coll != null && newSprites != null)
                    {
                        coll.spriteDefinitions = newSprites.ToArray();
                        newSprites = null;
                        if (coll.inst != coll && newSpriteInst != null)
                        {
                            coll.inst.spriteDefinitions = newSpriteInst.ToArray();
                            newSpriteInst = null;
                            coll.inst.spriteNameLookupDict = null;
                        }
                        coll.spriteNameLookupDict = null;
                    }
                    coll = Collections.Find(x => x?.spriteCollectionName != null && x.spriteCollectionName.Replace(" ", "_").ToLowerInvariant() == collection.ToLowerInvariant());
                    if (coll?.spriteDefinitions != null)
                    {
                        newSprites = new(coll.spriteDefinitions);
                        newSpriteInst = new(coll.inst.spriteDefinitions);
                    }
                    cachedCollectionName = collection;
                }
                if (coll?.spriteDefinitions != null && coll.Count > 0) //no point in going though the sprites if they mean nothing
                {
                    var instIsNew = coll.inst != coll;
                    Texture2D tex = new(1, 1, TextureFormat.RGBA32, false);
                    using (var s = asmb.GetManifestResourceStream(resource))
                    {
                        try
                        {
                            var b = new byte[s.Length];
                            s.Read(b, 0, b.Length);
                            tex.LoadImage(b);
                        }
                        catch { }
                    }
                    var id = -1;
                    var existingIdx = coll.GetSpriteIdByName(name, -1);
                    if (existingIdx >= 0)
                    {
                        coll.spriteDefinitions[existingIdx].ReplaceTexture(tex);
                        if (instIsNew)
                        {
                            coll.inst.spriteDefinitions[existingIdx].ReplaceTexture(tex);
                        }
                        id = existingIdx;
                    }
                    else
                    {
                        var frame = new tk2dSpriteDefinition
                        {
                            name = name,
                            material = coll.materials[0]
                        }; // if youre reading this, FROG
                        frame.ReplaceTexture(tex);

                        frame.normals = new Vector3[0];
                        frame.tangents = new Vector4[0];
                        frame.indices = new int[] { 0, 3, 1, 2, 3, 0 };
                        const float pixelScale = 0.0625f;
                        float w = tex.width * pixelScale;
                        float h = tex.height * pixelScale;
                        frame.position0 = new Vector3(0f, 0f, 0f);
                        frame.position1 = new Vector3(w, 0f, 0f);
                        frame.position2 = new Vector3(0f, h, 0f);
                        frame.position3 = new Vector3(w, h, 0f);
                        frame.boundsDataCenter = frame.untrimmedBoundsDataCenter = new Vector3(w / 2f, h / 2f, 0f);
                        frame.boundsDataExtents = frame.untrimmedBoundsDataExtents = new Vector3(w, h, 0f);
                        id = newSprites.Count;
                        newSprites.Add(frame);
                        newSpriteInst.Add(frame);
                    }
                    var mapName = $"sprite/{collection}/{name}";
                    if (TextureMap.ContainsKey(mapName))
                    {
                        TextureMap[mapName] = tex;
                    }
                    else
                    {
                        TextureMap.Add(mapName, tex);
                    }
                    var jsonpath = $"{path}{collection}.{name}.json";
                    if (resources.Contains(jsonpath))
                    {
                        using var stream = asmb.GetManifestResourceStream(jsonpath);
                        AssetSpriteData frameData = default;
                        try
                        {
                            frameData = JSONHelper.ReadJSON<AssetSpriteData>(stream);
                        }
                        catch { ETGModConsole.Log("Error: invalid json at project path " + jsonpath); continue; }
                        coll.SetAttachPoints(id, frameData.attachPoints);
                        if (instIsNew)
                        {
                            coll.inst.SetAttachPoints(id, frameData.attachPoints);
                        }
                    }
                }
                else
                {
                    Texture2D tex = new(1, 1, TextureFormat.RGBA32, false);
                    using (var s = asmb.GetManifestResourceStream(resource))
                    {
                        try
                        {
                            var b = new byte[s.Length];
                            s.Read(b, 0, b.Length);
                            tex.LoadImage(b);
                        }
                        catch { }
                    }
                    if (!unprocessedReplacements.TryGetValue(collection.Replace("_", " "), out var replacement))
                    {
                        replacement = new();
                        unprocessedReplacements.Add(collection.Replace("_", " "), replacement);
                    }
                    replacement.Add(name, tex);
                }
            }
            if (coll != null && newSprites != null)
            {
                coll.spriteDefinitions = newSprites.ToArray();
                newSprites = null;
                if (coll.inst != coll && newSpriteInst != null)
                {
                    coll.inst.spriteDefinitions = newSpriteInst.ToArray();
                    newSpriteInst = null;
                }
                coll.spriteNameLookupDict = null;
            }
        }

        /// <summary>
        /// Replaces a sprite definition's texture with the given one.
        /// </summary>
        /// <param name="frame">The sprite definition to replace.</param>
        /// <param name="replacement">The replacement texture.</param>
        /// <param name="pack">Does nothing, only exists for backwards compatibility.</param>
        public static void ReplaceTexture(tk2dSpriteDefinition frame, Texture2D replacement, bool pack = true)
        {
            frame.flipped = tk2dSpriteDefinition.FlipMode.None;
            frame.materialInst = new Material(frame.material);
            frame.texelSize = replacement.texelSize;
            frame.extractRegion = true;
            RuntimeAtlasSegment segment = Packer.Pack(replacement);
            frame.materialInst.mainTexture = segment.texture;
            frame.uvs = segment.uvs;
        }

    }

    /// <summary>
    /// Replaces a sprite definition's texture with the given one.
    /// </summary>
    /// <param name="frame">The sprite definition to replace.</param>
    /// <param name="replacement">The replacement texture.</param>
    /// <param name="pack">Does nothing, only exists for backwards compatibility.</param>
    public static void ReplaceTexture(this tk2dSpriteDefinition frame, Texture2D replacement, bool pack = true)
    {
        Assets.ReplaceTexture(frame, replacement);
    }
}