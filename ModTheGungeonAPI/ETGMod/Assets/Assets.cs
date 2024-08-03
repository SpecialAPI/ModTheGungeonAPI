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
        if(plug == null || plug.Info == null || plug.Info.Location == null)
        {
            return "";
        }
        return Path.GetDirectoryName(plug.Info.Location);
    }

    /// <summary>
    /// ETGMod asset management.
    /// </summary>
    public static partial class Assets
    {
        /// <summary>
        /// The extension for tk2dSpriteDefinition metadata files, without the . at the start.
        /// </summary>
        public const string DEFINITION_METADATA_EXTENSION = "jtk2d";
        /// <summary>
        /// The extension for tk2dSpriteDefinition metadata files, with the . at the start.
        /// </summary>
        public const string FULL_DEFINITION_METADATA_EXTENSION = ".jtk2d";
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
                new((x        ) / (float) texture.width, (y         ) / (float) texture.height),
                new((x + width) / (float) texture.width, (y         ) / (float) texture.height),
                new((x        ) / (float) texture.width, (y + height) / (float) texture.height),
                new((x + width) / (float) texture.width, (y + height) / (float) texture.height),
            };
        }

        /// <summary>
        /// Sets up sprites using manual lists, adding sprites to collections or replacing existing sprites.
        /// </summary>
        /// <param name="sheetReplacements">A dictionary where the keys are collction names and the values are the spritesheet replacements for those collections.</param>
        /// <param name="definitionReplacements">A ditionary where the keys are collection namse and the values are dictionaries with definition names as keys and definition sprites as values.</param>
        /// <param name="spriteData">A ditionary where the keys are collection namse and the values are dictionaries with definition names as keys and definition attach point information as values.</param>
        public static void SetupSprites(Dictionary<string, Texture2D> sheetReplacements, Dictionary<string, Dictionary<string, Texture2D>> definitionReplacements, Dictionary<string, Dictionary<string, AssetSpriteData>> spriteData)
        {
            if(sheetReplacements != null)
            {
                foreach(var kvp in sheetReplacements)
                {
                    var coll = FindCollectionOfName(kvp.Key);

                    if (coll == null || coll.materials == null || coll.materials.Length == 0)
                    {
                        if (coll != null)
                            continue;

                        var cname = kvp.Key.Replace("_", " ");
                        unprocessedSingleTextureReplacements[cname] = kvp.Value;

                        continue;
                    }

                    HandleCollectionSheetReplacement(coll, kvp.Value);
                }
            }

            if(definitionReplacements != null)
            {
                foreach(var kvp in definitionReplacements)
                {
                    var coll = FindCollectionOfName(kvp.Key);

                    if (coll == null || coll.materials == null || coll.materials.Length == 0)
                    {
                        if (coll != null)
                            continue;

                        var cname = kvp.Key.Replace("_", " ");

                        if (!unprocessedReplacements.TryGetValue(cname, out var dfs) || dfs == null)
                            unprocessedReplacements[cname] = dfs = new();

                        dfs.AddRange(kvp.Value);

                        continue;
                    }

                    HandleCollectionDefinitionReplacements(coll, kvp.Value);
                }
            }

            if(spriteData != null)
            {
                foreach(var kvp in spriteData)
                {
                    var coll = FindCollectionOfName(kvp.Key);

                    if (coll == null || coll.materials == null || coll.materials.Length == 0)
                    {
                        if (coll != null)
                            continue;

                        var cname = kvp.Key.Replace("_", " ");

                        if (!unprocessedJsons.TryGetValue(cname, out var dfs) || dfs == null)
                            unprocessedJsons[cname] = dfs = new();

                        dfs.AddRange(kvp.Value);

                        continue;
                    }

                    HandleCollectionAttachPoints(coll, kvp.Value);
                }
            }
        }

        /// <summary>
        /// Attempts to find a collection with a given name. Returns null if no collection is found.
        /// </summary>
        /// <param name="name">The name of the collection to find.</param>
        /// <returns>The found collection or null if no collection was found.</returns>
        public static tk2dSpriteCollectionData FindCollectionOfName(string name)
        {
            if (Collections == null)
                return null;

            return Collections.Find(x => x != null && !string.IsNullOrEmpty(x.spriteCollectionName) && x.spriteCollectionName.Replace("_", " ").ToLowerInvariant() == name.Replace("_", " ").ToLowerInvariant());
        }

        /// <summary>
        /// Replaces all sprites in a collection using a spritesheet.
        /// </summary>
        /// <param name="coll">The target collection.</param>
        /// <param name="sheetTex">The spritesheet used for replacements.</param>
        public static void HandleCollectionSheetReplacement(tk2dSpriteCollectionData coll, Texture2D sheetTex)
        {
            if (coll == null || sheetTex == null)
                return;

            var material = coll.materials[0];
            if (!material)
                return;

            var mainTexture = material.mainTexture;
            if (!mainTexture)
                return;

            var atlasName = mainTexture.name;
            if (string.IsNullOrEmpty(atlasName))
                return;

            if (atlasName.StartsWith("~"))
                return;

            var textureCache = new Dictionary<int, Texture>();

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
                        textureCache[i] = def.materialInst.mainTexture;
                }
            }

            sheetTex.name = '~' + atlasName;

            for (int i = 0; i < coll.materials.Length; i++)
            {
                if (coll.materials[i] == null || coll.materials[i].mainTexture == null)
                    continue;

                coll.materials[i].mainTexture = sheetTex;
            }

            if (coll.inst == null)
                return;

            coll.inst.materialInsts = null;
            coll.inst.Init();

            foreach (var kvp in textureCache)
            {
                if (kvp.Key >= coll.Count)
                    continue;

                var def = coll.spriteDefinitions[kvp.Key];

                if (def == null || def.materialInst == null)
                    continue;

                def.materialInst = new(def.material)
                {
                    mainTexture = kvp.Value
                };
            }

            if (!coll.inst != coll)
                return;

            if (coll.inst.materials != null)
            {
                for (int i = 0; i < coll.inst.materials.Length; i++)
                {
                    if (coll.inst.materials[i]?.mainTexture == null)
                        continue;

                    coll.inst.materials[i].mainTexture = sheetTex;
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

        /// <summary>
        /// Replaces or adds specific sprites to a collection.
        /// </summary>
        /// <param name="coll">The target collection.</param>
        /// <param name="sprites">Sprites to replace or add. Keys are definition names and values are the sprites.</param>
        public static void HandleCollectionDefinitionReplacements(tk2dSpriteCollectionData coll, Dictionary<string, Texture2D> sprites)
        {
            if(coll == null || sprites == null || sprites.Count <= 0)
                return;

            if(coll.Count <= 0)
                return;

            var newSprites = new List<tk2dSpriteDefinition>(coll.spriteDefinitions ?? []);
            var newSpriteInst = coll.inst != null ? new List<tk2dSpriteDefinition>(coll.inst.spriteDefinitions ?? []) : [];

            var instIsNew = coll.inst != null && coll.inst != coll;

            foreach (var kvp in sprites)
            {
                var defName = kvp.Key;
                var tex = kvp.Value;

                var existingIdx = coll.GetSpriteIdByName(defName, -1);

                TextureMap[$"sprites/{coll.spriteCollectionName}/{defName}"] = tex;

                if (existingIdx >= 0)
                {
                    coll.spriteDefinitions[existingIdx].ReplaceTexture(tex);

                    if (instIsNew)
                        coll.inst.spriteDefinitions[existingIdx].ReplaceTexture(tex);

                    continue;
                }

                var frame = new tk2dSpriteDefinition
                {
                    name = defName,
                    material = coll.materials[0]
                };

                frame.ReplaceTexture(tex);

                frame.normals = [];
                frame.tangents = [];
                frame.indices = [ 0, 3, 1, 2, 3, 0 ];

                var pixelScale = 0.0625f;

                var w = tex.width * pixelScale;
                var h = tex.height * pixelScale;

                frame.position0 = new Vector3(0f, 0f, 0f);
                frame.position1 = new Vector3(w, 0f, 0f);
                frame.position2 = new Vector3(0f, h, 0f);
                frame.position3 = new Vector3(w, h, 0f);

                frame.boundsDataCenter = frame.untrimmedBoundsDataCenter = new Vector3(w / 2f, h / 2f, 0f);
                frame.boundsDataExtents = frame.untrimmedBoundsDataExtents = new Vector3(w, h, 0f);

                newSprites.Add(frame);
                newSpriteInst.Add(frame);
            }

            coll.spriteDefinitions = [.. newSprites];
            coll.spriteNameLookupDict = null;

            if (instIsNew)
            {
                coll.inst.spriteDefinitions = [.. newSpriteInst];
                coll.inst.spriteNameLookupDict = null;
            }
        }

        /// <summary>
        /// Replaces or adds attach points to a collection.
        /// </summary>
        /// <param name="coll">The target collection.</param>
        /// <param name="attachPoint">Attach points to replace or add. Keys are definition names and values are the attach point information.</param>
        public static void HandleCollectionAttachPoints(tk2dSpriteCollectionData coll, Dictionary<string, AssetSpriteData> attachPoint)
        {
            if(coll == null || attachPoint == null || attachPoint.Count <= 0)
                return;

            if (coll.Count <= 0)
                return;

            var instIsNew = coll.inst != null && coll.inst != coll;

            foreach (var kvp in attachPoint)
            {
                var id = coll.GetSpriteIdByName(kvp.Key, -1);

                if (id < 0)
                    continue;

                coll.SetAttachPoints(id, kvp.Value.attachPoints);

                if (instIsNew)
                    coll.inst.SetAttachPoints(id, kvp.Value.attachPoints);
            }
        }

        /// <summary>
        /// Sets up sprites from a folder, adding sprites to collections or replacing existing sprites.
        /// </summary>
        /// <param name="folder">The root folder for the sprite setup.</param>
        public static void SetupSpritesFromFolder(string folder)
        {
            if (processedFolders.Contains(folder))
                return;

            if (!Directory.Exists(folder))
            {
                ETGModConsole.Log($"Part of the path to {folder} is missing!");
                return;
            }

            var sheet =         new Dictionary<string, Texture2D>();
            var definition =    new Dictionary<string, Dictionary<string, Texture2D>>();
            var attachPoint =   new Dictionary<string, Dictionary<string, AssetSpriteData>>();

            foreach (var image in Directory.GetFiles(folder, "*.png"))
            {
                var trimmed = Path.GetFileNameWithoutExtension(image);
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);

                try
                {
                    tex.LoadImage(File.ReadAllBytes(image));
                }
                catch { }

                tex.filterMode = FilterMode.Point;

                sheet[trimmed] = tex;
            }

            foreach (var collection in Directory.GetDirectories(folder))
            {
                var collName = Path.GetFileName(collection);

                var images = Directory.GetFiles(collection, "*.png");
                var metadata = Directory.GetFiles(collection, "*.json").Concat(Directory.GetFiles(collection, $"*{FULL_DEFINITION_METADATA_EXTENSION}"));

                foreach (var im in images)
                {
                    if (!definition.TryGetValue(collName, out var dfs))
                        definition[collName] = dfs = new();

                    var defName = Path.GetFileNameWithoutExtension(im);
                    var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);

                    try
                    {
                        tex.LoadImage(File.ReadAllBytes(im));
                    }
                    catch { }

                    dfs[defName] = tex;
                }

                foreach(var j in metadata)
                {
                    if (!attachPoint.TryGetValue(collName, out var dfs))
                        attachPoint[collName] = dfs = new();

                    using var stream = File.OpenRead(j);
                    var defName = Path.GetFileNameWithoutExtension(j);

                    try
                    {
                        dfs[defName] = JSONHelper.ReadJSON<AssetSpriteData>(stream);
                    }
                    catch { ETGModConsole.Log("Error: invalid json at path " + j); }
                }
            }

            SetupSprites(sheet, definition, attachPoint);
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
                path += ".";

            var sheet = new Dictionary<string, Texture2D>();
            var definition = new Dictionary<string, Dictionary<string, Texture2D>>();
            var attachPoint = new Dictionary<string, Dictionary<string, AssetSpriteData>>();

            var resources = asmb.GetManifestResourceNames();

            foreach (var resource in resources)
            {
                if (resource.StartsWith(path) && resource.Length > path.Length)
                {
                    var names = resource.Substring(path.Length).Split('.');

                    // Folder
                    if (names.Length == 3)
                    {
                        var collName = names[0];
                        var defName = names[1];
                        var extension = names[2];

                        if (extension.ToLowerInvariant() == "png")
                        {
                            using var strem = asmb.GetManifestResourceStream(resource);
                            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);

                            try
                            {
                                var ba = new byte[strem.Length];
                                strem.Read(ba, 0, ba.Length);

                                tex.LoadImage(ba);
                            }
                            catch { }

                            tex.filterMode = FilterMode.Point;

                            if (!definition.TryGetValue(collName, out var df) || df == null)
                                definition[collName] = df = new();

                            df[defName] = tex;
                        }

                        else if (extension.ToLowerInvariant() == "json" || extension.ToLowerInvariant() == DEFINITION_METADATA_EXTENSION)
                        {
                            using var stream = asmb.GetManifestResourceStream(resource);

                            if (!attachPoint.TryGetValue(collName, out var df) || df == null)
                                attachPoint[collName] = df = new();

                            try
                            {
                                df[defName] = JSONHelper.ReadJSON<AssetSpriteData>(stream);
                            }
                            catch { ETGModConsole.Log("Error: invalid json at project path " + resource); continue; }
                        }
                    }

                    // Single sheet
                    else if(names.Length == 2)
                    {
                        var name = names[0];
                        var extension = names[1];

                        if (extension.ToLowerInvariant() != "png")
                            continue;

                        using var strem = asmb.GetManifestResourceStream(resource);
                        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);

                        try
                        {
                            var ba = new byte[strem.Length];
                            strem.Read(ba, 0, ba.Length);

                            tex.LoadImage(ba);
                        }
                        catch { }

                        tex.filterMode = FilterMode.Point;

                        sheet[name] = tex;
                    }
                }
            }

            SetupSprites(sheet, definition, attachPoint);
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