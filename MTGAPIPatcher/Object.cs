using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoMod;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MTGAPIPatcher
{
    [MonoModPatch("UnityEngine.Object")]
    internal class patch_Object : Object
    {
        public static new T Instantiate<T>(T original) where T : Object
        {
            return (T)(object)orig_Instantiate(original);
        }

        public static new Object Instantiate(Object original)
        {
            return orig_Instantiate(original);
        }

        public static extern Object orig_Instantiate(Object original);


    }
}
