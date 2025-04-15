using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 扩展类
/// </summary>
static public class Extensions
{
    /// <summary>
    /// 从字典中获取值，若不存在则返回默认值
    /// </summary>
    /// <typeparam name="TKey">键的类型</typeparam>
    /// <typeparam name="TValue">值得类型</typeparam>
    /// <param name="dictionary">字典</param>
    /// <param name="key">键</param>
    /// <param name="defaultValue">值,初始化了默认值</param>
    /// <returns></returns>
    public static TValue GetValueOrDefault<TKey,TValue>(this Dictionary<TKey, TValue> dictionary,TKey key,TValue defaultValue = default(TValue))
    {
        //声明一下值，避免报错
        TValue value;
        //若字典中存在键，则返回值，否则返回默认值
        return dictionary.TryGetValue(key, out value) ? value : defaultValue;
    }
}
