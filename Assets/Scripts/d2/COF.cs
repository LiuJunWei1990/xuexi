using System.Collections.Generic;
using System.IO;

/// <summary>
/// COF文件导入器,COF文件是用于存储DCC文件的游戏对象的动画信息的文件格式。
/// </summary>
public class COF
{
    // 定义COF文件的各个属性
    public Layer[] layers; // 图层数组
    public int framesPerDirection; // 每个方向的帧数
    public int directionCount; // 方向数量
    public int layerCount; // 图层数量
    public int mode; // 模式
    public byte[] priority; // 优先级数组

    // 定义Layer结构体
    public struct Layer
    {
        public string dccFilename; // DCC文件名
        public string name; // 图层名称
    }

    // 定义不同对象的模式名称
    public static readonly string[][] ModeNames = {
        new string[] { "DT", "NU", "WL", "RN", "GH", "TN", "TW", "A1", "A2", "BL", "SC", "TH", "KK", "S1", "S2", "S3", "S4", "DD", "GH", "GH" }, // 玩家模式
        new string[] { "DT", "NU", "WL", "GH", "A1", "A2", "BL", "SC", "S1", "S2", "S3", "S4", "DD", "GH", "xx", "RN" }, // 怪物模式
        new string[] { "NU", "OP", "ON", "S1", "S2", "S3", "S4", "S5" } // 对象模式
    };

    // 定义图层名称
    static public readonly string[] layerNames = { "HD", "TR", "LG", "RA", "LA", "RH", "LH", "SH", "S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8" };

    // 缓存已加载的COF文件
    static Dictionary<string, COF> cache = new Dictionary<string, COF>();

    // 加载COF文件的静态方法
    static public COF Load(Obj obj, string mode)
    {
        // 获取对象的基本路径、token和class
        string basePath = obj._base;
        string token = obj.token;
        string _class = obj._class;
        
        // 构建COF文件路径
        string cofFilename = "Assets/d2/" + basePath + "/" + token + "/cof/" + token + mode + _class + ".cof";
        cofFilename.ToLower();

        // 如果文件已缓存，直接返回缓存结果
        if (cache.ContainsKey(cofFilename))
        {
            return cache[cofFilename];
        }

        // 创建新的COF对象
        COF cof = new COF();

        // 读取COF文件字节数据
        byte[] bytes = File.ReadAllBytes(cofFilename);
        var stream = new MemoryStream(bytes);
        var reader = new BinaryReader(stream);

        // 读取COF文件头信息
        cof.layerCount = reader.ReadByte(); // 读取图层数量
        cof.framesPerDirection = reader.ReadByte(); // 读取每个方向的帧数
        cof.directionCount = reader.ReadByte(); // 读取方向数量
        cof.mode = System.Array.IndexOf(ModeNames[obj.type], mode); // 获取模式索引
        stream.Seek(25, SeekOrigin.Current); // 跳过25个字节

        // 初始化图层数组
        cof.layers = new Layer[16];

        // 读取每个图层的信息
        for (int i = 0; i < cof.layerCount; ++i)
        {
            int compositIndex = reader.ReadByte(); // 读取图层索引
            string compositName = layerNames[compositIndex]; // 获取图层名称

            // 跳过阴影相关字节
            reader.ReadByte();
            reader.ReadByte();

            // 跳过透明度相关字节
            reader.ReadByte();
            reader.ReadByte();

            // 读取武器类别
            string weaponClass = System.Text.Encoding.Default.GetString(reader.ReadBytes(3));
            reader.ReadByte(); // 跳过武器类别的结束字节
            string sptr = obj.layers[compositIndex]; // 获取图层后缀

            // 构建DCC文件路径
            cof.layers[compositIndex].dccFilename = "Assets/d2/" + basePath + "/" + token + "/" + compositName + "/" + token + compositName + sptr + mode + weaponClass + ".dcc";
            cof.layers[compositIndex].name = compositName + " " + sptr; // 设置图层名称
        }

        // 跳过帧数相关字节
        stream.Seek(cof.framesPerDirection, SeekOrigin.Current);

        // 读取优先级数据
        cof.priority = reader.ReadBytes(cof.directionCount * cof.framesPerDirection * cof.layerCount);

        // 尝试查找动画数据
        AnimData animData = new AnimData();
        if (AnimData.Find(token + mode + _class, ref animData))
        {
            //Debug.Log(cofFilename + " " + framesPerDirection + " anim data found " + animData.framesPerDir + " " + animData.speed);
        }

        // 将COF对象加入缓存
        cache.Add(cofFilename, cof);
        return cof;
    }
}
