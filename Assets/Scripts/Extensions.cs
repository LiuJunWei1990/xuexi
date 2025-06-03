﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static public class Extensions {
    public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
    {
        TValue value;
        return dictionary.TryGetValue(key, out value) ? value : defaultValue;
    }
}
