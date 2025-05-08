using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 定义 AnimData 结构体，用于存储动画数据
public struct AnimData
{
    // COF 文件名
    public string cofName;
    // 每个方向的帧数
    public int framesPerDir;
    // 动画速度
    public int speed;
    // 标志位数组
    public byte[] flags;

    // 静态方法，用于根据名称查找动画数据
    public static bool Find(string name, ref AnimData animData)
    {
        // 计算名称的哈希值
        byte hash = Hash(name);
        // 如果哈希桶为空，返回 false
        if (buckets[hash].data == null)
            return false;

        // 遍历哈希桶中的数据
        foreach(var data in buckets[hash].data)
        {
            // 如果找到匹配的 COF 文件名
            if (data.cofName == name)
            {
                // 将找到的动画数据赋值给引用参数
                animData = data;
                return true;
            }
        }
        // 未找到匹配的动画数据，返回 false
        return false;
    }

    // 定义 Bucket 结构体，用于存储动画数据数组
    struct Bucket
    {
        public AnimData[] data;
    }

    // 静态哈希桶数组，用于存储动画数据
    static Bucket[] buckets = new Bucket[256];

    // 静态方法，用于计算字符串的哈希值
    static byte Hash(string name)
    {
        // 将名称转换为大写
        string upperName = name.ToUpper();
        // 初始化名称长度为字符串长度
        int nb = name.Length;
        // 初始化哈希值为 0
        byte hash = 0;

        // 遍历字符串，找到第一个 '.' 的位置
        for (int i = 0; i < name.Length; ++i)
        {
            if (name[i] == '.')
                nb = i;
        }
        // 计算哈希值
        for (int i = 0; i < nb; i++)
            hash += (byte) upperName[i];
        return hash;
    }

    // 静态构造函数，用于初始化动画数据
    static AnimData()
    {
        // 读取 animdata.d2 文件的字节数据
        byte[] bytes = File.ReadAllBytes("Assets/d2/data/global/animdata.d2");
        // 创建内存流
        var stream = new MemoryStream(bytes);
        // 创建二进制读取器
        var reader = new BinaryReader(stream);
        // 遍历文件内容
        while (stream.Position < stream.Length)
        {
            // 读取当前哈希桶中的动画数据数量
            int count = reader.ReadInt32();
            // 创建新的哈希桶
            var bucket = new Bucket();
            // 初始化动画数据数组
            bucket.data = new AnimData[count];
            // 初始化哈希值
            byte hash = 0;
            // 遍历动画数据
            for (int i = 0; i < count; ++i)
            {
                // 创建新的动画数据实例
                var animData = new AnimData();
                // 读取 COF 文件名（7 字节）
                animData.cofName = System.Text.Encoding.Default.GetString(reader.ReadBytes(7));
                // 跳过字符串结束符（0 字节）
                reader.ReadByte();
                // 读取每个方向的帧数
                animData.framesPerDir = reader.ReadInt32();
                // 读取动画速度
                animData.speed = reader.ReadInt32();
                // 读取标志位数组（144 字节）
                animData.flags = reader.ReadBytes(144);
                // 将动画数据添加到哈希桶中
                bucket.data[i] = animData;
                // 如果是第一个动画数据，计算哈希值
                if (i == 0)
                    hash = Hash(animData.cofName);
            }
            // 将哈希桶存储到哈希桶数组中
            buckets[hash] = bucket;
        }
    }
}
