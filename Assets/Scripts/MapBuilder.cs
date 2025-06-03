using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地图构建组件
/// </summary>
/// <remarks>
/// 1. 导入DS1文件数据
/// 2. 按DS1文件分布怪物预制体
/// 3. 按预制体生成玩家角色, 并关联角色控制器
/// </remarks>
public class MapBuilder : MonoBehaviour
{
    public string path;
    public Character playerPrefab;
    public GameObject monsterPrefab;

    void Start()
    {
        var result = DS1.Import("Assets/d2/data/global/tiles/" + path, monsterPrefab);
        var playerPos = result.entry;

        var player = Instantiate(playerPrefab, playerPos, Quaternion.identity);
        PlayerController.instance.SetCharacter(player);
    }
}
