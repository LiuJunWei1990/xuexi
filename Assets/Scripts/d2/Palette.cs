using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 调色板类，用于加载和管理游戏中的调色板。
/// </summary>
public class Palette
{
    // 静态变量，存储当前使用的颜色板
    static public Color[] palette;
    // 静态字典，存储不同幕号对应的调色板
    static Dictionary<int, Color[]> palettes = new Dictionary<int, Color[]>();

    // 加载指定幕号的调色板
    static public Color[] LoadPalette(int act)
    {
        // 如果字典中已经存在该幕号的调色板，就返回对应调色板
        if (palettes.ContainsKey(act))
        {
            palette = palettes[act];
            return palette;
        }

        //如果不存在就开始创建一个调色板
        // 创建一个新的调色板数组，包含256种颜色
        palette = new Color[256];
        // 打开对应幕号的调色板文件
        var stream = new BufferedStream(File.OpenRead("Assets/d2/data/global/palette/ACT" + act + "/Pal.PL2"));
        // 创建二进制读取器
        var reader = new BinaryReader(stream);
        // 遍历256种颜色
        for (int i = 0; i < 256; ++i)
        {
            // 读取RGB值
            byte r = reader.ReadByte();
            byte g = reader.ReadByte();
            byte b = reader.ReadByte();
            // 跳过第四个字节（通常为保留字节）
            reader.ReadByte();

            // 将RGB值转换为Unity的Color对象，并存储在调色板数组中
            palette[i] = new Color(r / 255f, g / 255f, b / 255f);
        }
        // 关闭文件流
        stream.Close();
        // 将调色板存储在字典中，以便后续使用
        palettes[act] = palette;
        // 返回调色板
        return palette;
    }
}
