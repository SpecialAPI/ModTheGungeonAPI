using System;
using System.IO;
using System.Reflection;
using System.Runtime;

namespace Gungeon {
    /// <summary>
    /// Contains the IDPools for Items and Enemies.
    /// </summary>
    public static partial class Game {
        private static readonly IDPool<PickupObject> _Items = new();
        private static readonly IDPool<AIActor> _Enemies = new();
        private static bool initialized;

        /// <summary>
        /// The IDPool for items, containing both modded and basegame items.
        /// </summary>
        public static IDPool<PickupObject> Items
        {
            get
            {
                if(_Items == null)
                {
                    Initialize();
                }
                return _Items;
            }
        }
        /// <summary>
        /// The IDPool for enemies, containing both modded and basegame enemies.
        /// </summary>
        public static IDPool<AIActor> Enemies
        {
            get
            {
                if (_Enemies == null)
                {
                    Initialize();
                }
                return _Enemies;
            }
        }

        private static Assembly _Assembly = Assembly.GetExecutingAssembly();

        private static void _SetupPool<T>(string map_file_name, IDPool<T> pool, Action<string, string> add_method)
        {
            using (Stream stream = _Assembly.GetManifestResourceStream($"ModTheGungeonAPI.Content.{map_file_name}"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string line;
                    while (true)
                    {
                        line = reader.ReadLine();
                        if (line == null)
                            break;
                        if (line.StartsWithInvariant("#"))
                            continue;
                        string[] split = line.Split(' ');
                        add_method.Invoke(split[0], split[1]);
                    }
                }
            }
        }

        internal static void Initialize()
        {
            if (initialized)
            {
                return;
            }
            _SetupPool("items.txt", Items, (string real, string mapped) => {
                int id;
                if (!int.TryParse(real, out id)) throw new Exception("Failed parsing item id map");
                Items.Add(mapped, PickupObjectDatabase.GetById(id)); // adding resolves the id (nonamespace becomes gungeon:nonamespace)
            });
            _SetupPool("enemies.txt", Enemies, (string real, string mapped) => {
                Enemies.Add(mapped, EnemyDatabase.GetOrLoadByGuid(real));
            });
            initialized = true;
        }

    }
}
