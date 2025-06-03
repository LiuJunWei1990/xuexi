using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 调色板
/// </summary>
/// <remarks>
/// rgb+分隔; 256色
/// </remarks>

public class Palette
{
    /// <summary>
    /// 调色板
    /// </summary>
    /// <remarks>
    /// 256色调色板,一个调色板对应一幕
    /// </remarks>
    static public Color32[] palette;
    /// <summary>
    /// 所有调色板
    /// </summary>
    /// <remarks>
    /// key是幕编号,value是调色板
    /// </remarks>
    static Dictionary<int, Color32[]> palettes = new Dictionary<int, Color32[]>();
    /// <summary>
    /// 加载调色板
    /// </summary>
    /// <param name="act"></param>
    /// <returns></returns>
    /// <remarks>
    /// 三个字段代表RGB,第四个字段用于分隔,或者后续的灰度,以此组成一个色; 一共256个色
    /// </remarks>
	static public Color32[] LoadPalette(int act)
    {
        //字典里有就直接返回
        if (palettes.ContainsKey(act))
        {
            palette = palettes[act];
            return palette;
        }

        // 如果没有就创建调色板
        palette = new Color32[256];
        // 读取二进制流,using的作用类似于for,使用完会释放流
        using (var stream = new MemoryStream(File.ReadAllBytes("Assets/d2/data/global/palette/ACT" + act + "/Pal.PL2")))
        using (var reader = new BinaryReader(stream))
        {
            for (int i = 0; i < 256; ++i)
            {
                //读一个字节
                byte r = reader.ReadByte();
                byte g = reader.ReadByte();
                byte b = reader.ReadByte();
                //过一个分隔字节
                reader.ReadByte();

                palette[i] = new Color32(r, g, b, 255);
            }
        }
        //幕是从1开始的,0是废弃的
        palette[0] = new Color(0, 0, 0, 0);
        //添加到字典中
        palettes[act] = palette;
        return palette;
    }
}
