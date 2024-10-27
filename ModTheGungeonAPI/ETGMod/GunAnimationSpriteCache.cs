using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Used to cache animation frames so that we do not need to recan sprite collections on every update to gun animations.
/// </summary>
internal class GunAnimationSpriteCache
{
    private static readonly Regex GeneralExtractingRegex = CreateGeneralRegularExpression();

    /// <summary>
    /// Organized by sprite collection name, eg WeaponCollection, WeaponCollection02, etc.
    /// </summary>
    private Dictionary<string, GunAnimationSpriteGroup> _gameSpriteCollections;

    public GunAnimationSpriteCache()
    {
        _gameSpriteCollections = new Dictionary<string, GunAnimationSpriteGroup>();
    }

    public bool UpdateCollection(tk2dSpriteCollectionData collection)
    {
        if (!collection)
            return false;

        bool update;
        if (_gameSpriteCollections.TryGetValue(collection.name, out var spriteGroup))
            update = spriteGroup.IdentityObject != collection.spriteDefinitions;
        else
            update = true;

        if (!update)
            return false;

        spriteGroup = new GunAnimationSpriteGroup(collection.name, collection.spriteDefinitions);
        _gameSpriteCollections[collection.name] = spriteGroup;

        for (int i = 0; i < collection.spriteDefinitions.Length; i++)
        {
            var sprite = collection.spriteDefinitions[i];
            if (!sprite.Valid)
                continue;

            var match = GeneralExtractingRegex.Match(sprite.name);

            if (!match.Success)
                continue;

            var prefix = match.Groups["prefix"].Value;
            if (int.TryParse(match.Groups["order"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var order))
                spriteGroup.SetFrame(prefix, order, collection, i);
        }

        return true;
    }

    public tk2dSpriteAnimationFrame[] TryGetAnimationFrames(string collectionName, string gunName, string animation)
    {
        if (!_gameSpriteCollections.TryGetValue(collectionName, out var groupCollection))
        {
            return null;
        }

        if (!groupCollection.TryGetAnimationFrames(gunName, animation, out var frames))
        {
            return null;
        }

        return frames;
    }

    private static Regex CreateGeneralRegularExpression()
    {
        return new Regex($"^(?<prefix>.*)_(?<order>\\d+)$");
    }

    private class GunAnimationSpriteGroup
    {
        private Dictionary<string, FrameCollection> _prefixFrames;

        public GunAnimationSpriteGroup(string name, object identityObject)
        {
            Name = name;
            IdentityObject = identityObject;
            _prefixFrames = new Dictionary<string, FrameCollection>(StringComparer.InvariantCultureIgnoreCase);
        }

        public string Name { get; }

        /// <summary>
        /// If this associated object changes we will rescan and update.
        /// </summary>
        public object IdentityObject { get; set; }

        public void SetFrame(string gunName, string animationName, int order, tk2dSpriteCollectionData collection, int index)
        {
            string key = $"{gunName}_{animationName}";
            if (!_prefixFrames.TryGetValue(key, out var frames))
            {
                frames = new FrameCollection();
                _prefixFrames[key] = frames;
            }

            frames.SetFrame(order, new tk2dSpriteAnimationFrame()
            {
                spriteCollection = collection,
                spriteId = index
            });
        }

        public void SetFrame(string prefix, int order, tk2dSpriteCollectionData collection, int index)
        {
            if (!_prefixFrames.TryGetValue(prefix, out var frames))
            {
                frames = new FrameCollection();
                _prefixFrames[prefix] = frames;
            }

            frames.SetFrame(order, new tk2dSpriteAnimationFrame()
            {
                spriteCollection = collection,
                spriteId = index
            });
        }

        public bool TryGetAnimationFrames(string gunName, string animationName, out tk2dSpriteAnimationFrame[] frames)
        {
            var prefix = $"{gunName}_{animationName}";
            if (_prefixFrames.TryGetValue(prefix, out var frameCollection))
            {
                frames = frameCollection.GetFrames();
                return true;
            }

            frames = null;
            return false;
        }

        public bool TryGetAnimationFrames(string prefix, out tk2dSpriteAnimationFrame[] frames)
        {
            if (_prefixFrames.TryGetValue(prefix, out var frameCollection))
            {
                frames = frameCollection.GetFrames();
                return true;
            }

            frames = null;
            return false;
        }

        internal class FrameCollection
        {
            private SortedList<int, tk2dSpriteAnimationFrame> _orderedFrames;
            private tk2dSpriteAnimationFrame[] _frames;
            private bool _dirty;

            public FrameCollection()
            {
                _orderedFrames = new SortedList<int, tk2dSpriteAnimationFrame>();
                _frames = null;
                _dirty = false;
            }

            public tk2dSpriteAnimationFrame[] GetFrames()
            {
                if (_dirty)
                {
                    if (_orderedFrames.Count == 0)
                    {
                        _frames = null;
                    }
                    else
                    {
                        _frames = _orderedFrames.Values.ToArray();
                    }

                    _dirty = false;
                }

                return _frames;
            }

            public void SetFrame(int order, tk2dSpriteAnimationFrame frame)
            {
                _orderedFrames[order] = frame;
                _frames = null;
                _dirty = true;
            }

            public void Clear()
            {
                _orderedFrames.Clear();
                _frames = null;
                _dirty = false;
            }
        }
    }
}
