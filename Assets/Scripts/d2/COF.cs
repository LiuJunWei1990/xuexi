using System.Collections.Generic;
using System.IO;

/// <summary>
/// 游戏对象动画数据
/// </summary>
public class COF
{
    /// <summary>
    /// 单个游戏对象图层容器(单个对象貌似可以有多个图层)
    /// </summary>
    /// <remarks>
    /// 用于游戏对象的显示图像的子节点(挂spriteRenderer组件的那个); 
    /// 容器中的数据用来给spriteRenderer组件赋值
    /// </remarks>
    public Layer[] layers;
    /// <summary>
    /// 一个动作的一个方向的帧数
    /// </summary>
    /// <remarks>
    /// 例如:向右跑/向左攻击,这种是一个动作
    /// </remarks>
    public int framesPerDirection;
    public int directionCount;
    public int layerCount;
    public int mode;
    public byte[] priority;
    /// <summary>
    /// 游戏对象图层
    /// </summary>
    /// <remarks>
    /// 保存游戏对象的图像参数
    /// <remarks>
    public struct Layer
    {
        public string dccFilename;
        public string name;
    }

    public static readonly string[][] ModeNames = {
        new string[] { "DT", "NU", "WL", "RN", "GH", "TN", "TW", "A1", "A2", "BL", "SC", "TH", "KK", "S1", "S2", "S3", "S4", "DD", "GH", "GH" }, // player (plrmode.txt)
        new string[] { "DT", "NU", "WL", "GH", "A1", "A2", "BL", "SC", "S1", "S2", "S3", "S4", "DD", "GH", "xx", "RN" }, // monsters (monmode.txt)
        new string[] { "NU", "OP", "ON", "S1", "S2", "S3", "S4", "S5" } // objects (objmode.txt)
    };
    static public readonly string[] layerNames = { "HD", "TR", "LG", "RA", "LA", "RH", "LH", "SH", "S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8" };

    /// <summary>
    /// 缓存
    /// </summary>
    /// <remarks>
    /// 键是cof文件路径,值是COF对象
    /// </remarks>
    static Dictionary<string, COF> cache = new Dictionary<string, COF>();

    static public COF Load(Obj obj, string mode)
    {
        //读obj的cof文件路径
        string basePath = obj._base;
        string token = obj.token;
        string _class = obj._class;

        //生成cof文件路径
        string cofFilename = "Assets/d2/" + basePath + "/" + token + "/cof/" + token + mode + _class + ".cof";
        //把字符串转换成小写,但是没赋值,好像没起作用...
        cofFilename.ToLower();
        //缓存里有就用缓存的
        if (cache.ContainsKey(cofFilename))
        {
            return cache[cofFilename];
        }
        
        //缓存里没有就创建一个COF对象
        COF cof = new COF();
        //读cof文件
        byte[] bytes = File.ReadAllBytes(cofFilename);
        var stream = new MemoryStream(bytes);
        var reader = new BinaryReader(stream);

        //读取cof文件的头信息
        cof.layerCount = reader.ReadByte();                                //层级数量
        cof.framesPerDirection = reader.ReadByte();                        //每方向动画的帧数量
        cof.directionCount = reader.ReadByte();                            //方向数量
        cof.mode = System.Array.IndexOf(ModeNames[obj.type], mode);        //mode字符串在,ModeNames[obj.type]数组中的索引编号
        stream.Seek(25, SeekOrigin.Current);                                //跳过25个字节

        cof.layers = new Layer[16];

        for (int i = 0; i < cof.layerCount; ++i)
        {
            int compositIndex = reader.ReadByte();
            //当前层级名字
            string compositName = layerNames[compositIndex];

            // shadows
            reader.ReadByte();
            reader.ReadByte();

            // transparency
            reader.ReadByte();
            reader.ReadByte();

            //读取武器类型,读三个字节,通过系统默认编码转成字符串,代表哪个种类的武器
            string weaponClass = System.Text.Encoding.Default.GetString(reader.ReadBytes(3));
            reader.ReadByte(); // zero byte from zero-terminated weapon class string
            string sptr = obj.layers[compositIndex];
            //
            cof.layers[compositIndex].dccFilename = "Assets/d2/" + basePath + "/" + token + "/" + compositName + "/" + token + compositName + sptr + mode + weaponClass + ".dcc";
            cof.layers[compositIndex].name = compositName + " " + sptr;
        }

        stream.Seek(cof.framesPerDirection, SeekOrigin.Current);
        cof.priority = reader.ReadBytes(cof.directionCount * cof.framesPerDirection * cof.layerCount);

        AnimData animData = new AnimData();
        if (AnimData.Find(token + mode + _class, ref animData))
        {
            //Debug.Log(cofFilename + " " + framesPerDirection + " anim data found " + animData.framesPerDir + " " + animData.speed);
        }

        cache.Add(cofFilename, cof);
        return cof;
    }
}
