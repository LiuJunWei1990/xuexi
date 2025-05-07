using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

// 定义COF类,COF文件貌似是一个保存角色身体模型的文件
// 它包含了角色的各个部位的纹理信息,比如透明度,阴影,武器等
public class COF
{
    // 定义导入结果结构体
    public struct ImportResult
    {
        // 存储图层文件路径的数组
        public string[] layers;
    }

    // 定义静态的图层名称数组
    static readonly string[] layerNames = { "HD", "TR", "LG", "RA", "LA", "RH", "LH", "SH", "S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8" };
    // 创建缓存字典，用于存储已加载的COF文件
    static Dictionary<string, ImportResult> cache = new Dictionary<string, ImportResult>();

    // 定义静态的Load方法，用于加载COF文件
    static public ImportResult Load(string _base, string token, string mode, string _class)
    {
        // 将路径中的反斜杠替换为正斜杠
        _base = _base.Replace('\\', '/');
        // 构建COF文件路径
        string cofFilename = "Assets/d2/" + _base + "/" + token + "/cof/" + token + mode + _class + ".cof";
        // 将文件名转换为小写（注意：这行代码实际上没有效果，因为ToLower()返回新字符串）
        cofFilename.ToLower();
        // 如果文件已在缓存中，直接返回缓存结果
        if (cache.ContainsKey(cofFilename))
        {
            return cache[cofFilename];
        }

        // 创建新的导入结果对象
        ImportResult result = new ImportResult();

        // 读取COF文件的字节数据
        byte[] bytes = File.ReadAllBytes(cofFilename);
        // 创建内存流
        var stream = new MemoryStream(bytes);
        // 创建二进制读取器
        var reader = new BinaryReader(stream);

        // 读取图层数量
        byte layerCount = reader.ReadByte();
        // 读取每个方向的帧数
        byte framesPerDirection = reader.ReadByte();
        // 读取方向数量
        byte directionCount = reader.ReadByte();
        // 跳过25个字节（未知数据）
        stream.Seek(25, SeekOrigin.Current);

        // 初始化图层路径数组
        result.layers = new string[layerCount];

        // 遍历每个图层
        for (int i = 0; i < layerCount; ++i)
        {
            // 读取图层索引
            int compositIndex = reader.ReadByte();
            // 根据索引获取图层名称
            string compositName = layerNames[compositIndex];

            // 跳过阴影相关数据（2字节）
            reader.ReadByte();
            reader.ReadByte();

            // 跳过透明度相关数据（2字节）
            reader.ReadByte();
            reader.ReadByte();

            // 读取武器类别（3字节）
            string weaponClass = System.Text.Encoding.Default.GetString(reader.ReadBytes(3));
            // 跳过武器类别的零终止字节
            reader.ReadByte();
            // 定义sptr字符串为"lit"
            string sptr = "lit";
            // 构建DCC文件路径
            string filename = "Assets/d2/" + _base + "/" + token + "/" + compositName + "/" + token + compositName + sptr + mode + weaponClass + ".dcc";
            // 将文件路径存入结果数组
            result.layers[i] = filename;
        }

        // 将结果加入缓存
        cache.Add(cofFilename, result);
        // 返回导入结果
        return result;
    }
}
