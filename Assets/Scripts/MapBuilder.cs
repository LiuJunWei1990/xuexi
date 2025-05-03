using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地图构建器
/// 用于将DS1格式的地图文件导入到Unity中
/// </summary>
public class MapBuilder : MonoBehaviour
{
    // 地图文件路径
    public string path;
    // 玩家角色预制体
    public Character playerPrefab;
    // 怪物预制体
    public GameObject monsterPrefab;

    // 游戏开始时执行
    void Start ()
    {
        // 导入地图数据，返回包含入口位置和生成怪物的结果
        var result = DS1.Import("Assets/d2/data/global/tiles/" + path, monsterPrefab);
        // 获取玩家出生位置
        var playerPos = result.entry;

        // 在指定位置实例化玩家角色
        var player = Instantiate(playerPrefab, playerPos, Quaternion.identity);
        // 将生成的玩家角色设置给玩家控制器
        PlayerController.instance.SetCharacter(player);
    }
}
