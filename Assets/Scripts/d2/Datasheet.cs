using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;
using System.IO;
using UnityEngine;


/// <summary>
/// 定义泛型结构体Datasheet，T : new()代表必须有无参构造函数(默认构造函数)
/// </summary>
/// <typeparam name="T"></typeparam>
public struct Datasheet<T> where T : new()
{
    /// <summary>
    /// 新建一个T类型列表容器,文件取出的数据导入到这个类型中
    /// </summary>
    public List<T> rows;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="type"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    /// <exception cref="System.FormatException"></exception>
    // 静态方法，用于将字符串值转换为指定类型
    static object CastValue(string value, System.Type type, object defaultValue)
    {
        // 如果值为空或为"xxx"，返回默认值
        if (value == "" || value == "xxx")
            return defaultValue;

        // 如果目标类型是bool
        if (type == typeof(bool))
        {
            // 如果值为"1"，返回true
            if (value == "1")
                return true;
            // 如果值为"0"，返回false
            else if (value == "0")
                return false;
            // 否则抛出格式异常
            else
                throw new System.FormatException("无法将 '" + value + "' 转换为bool值");
        }
        else
        {
            // 对于其他类型，使用Convert.ChangeType进行转换
            return System.Convert.ChangeType(value, type);
        }
    }


    /// <summary>
    /// 静态方法，从指定文件加载数据
    ///    >读取整个文件内容,并进行分割
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    /// <exception cref="System.Exception"></exception>
    public static Datasheet<T> Load(string filename)
    {
        // 读取整个文件内容
        string csv = File.ReadAllText(filename);
        // 获取T类型的所有可序列化成员数组
        //    ·成员数组:MemberInfo[],这是类型中所有成员的基类,包括字段,属性,方法等
        //    ·可序列化成员:公开的字段和属性,或者私有但被[Serializable]特性标记了的字段和属性(如果类被标记,那么整个类的所有成员都是可序列化的)
        MemberInfo[] members = FormatterServices.GetSerializableMembers(typeof(T));

        // 初始化预期字段数量为0
        int expectedFieldCount = 0;
        // 创建一个T类型的实例
        T dummy = new T();
        // 遍历所有可序列化成员
        foreach (MemberInfo member in members)
        {
            // 将成员转换为字段信息
            FieldInfo fi = (FieldInfo)member;
            // 如果字段类型是数组
            if (fi.FieldType.IsArray)
            {
                // 获取数组的长度并累加到预期字段数量中
                expectedFieldCount += ((System.Collections.IList)fi.GetValue(dummy)).Count;
            }
            else
            {
                // 否则，预期字段数量加1
                expectedFieldCount += 1;
            }
        }

        // 创建新的Datasheet实例
        Datasheet<T> sheet = new Datasheet<T>();
        // 初始化rows列表
        sheet.rows = new List<T>();
        // 按换行符分割文件内容
        var lines = csv.Split('\n');
        
        // 逐行处理
        for (int lineIndex = 0; lineIndex < lines.Length; ++lineIndex)
        {
            // 去除行首尾空白
            string line = lines[lineIndex].Trim();
            // 如果是空行则跳过
            if (line.Length == 0)
                continue;

            // 按制表符(Tab)分割字段
            var fields = line.Split('\t');
            // 检查分割的string数量是否和类型的可序列化成员数量匹配
            if (fields.Length != expectedFieldCount)
                throw new System.Exception("导入数据与类型不匹配,类型: " + typeof(T) + " (" + expectedFieldCount + " 个成员) 导入数据: " + filename + ":" + (lineIndex + 1) + " (" + fields.Length + " 个字段)");

            // 跳过第一行（假设是标题行）
            if (lineIndex == 0)
                continue;

            // 创建新的T类型实例,这里就是约束了T必须有无参构造函数的原因
            T obj = new T();
            // 初始化成员索引为0
            int memberIndex = 0;
            // 遍历所有成员
            for (int fieldIndex = 0; fieldIndex < fields.Length; ++memberIndex)
            {
                // 获取当前遍历的成员
                MemberInfo member = members[memberIndex];
                // 获取成员信息
                FieldInfo fi = (FieldInfo)member;
                // 尝试解析字段值
                try
                {
                    // 如果字段类型是数组
                    if (fi.FieldType.IsArray)
                    {
                        // 获取数组元素的类型
                        var elementType = fi.FieldType.GetElementType();
                        // 获取数组实例
                        var array = (System.Collections.IList)fi.GetValue(obj);
                        // 遍历数组元素
                        for (int i = 0; i < array.Count; ++i)
                        {
                            // 将字段值转换为数组元素类型并赋值
                            array[i] = CastValue(fields[fieldIndex], elementType, array[i]);
                            // 移动到下一个字段
                            ++fieldIndex;
                        }
                    }
                    else
                    {
                        // 对于非数组字段，直接转换并赋值
                        var value = CastValue(fields[fieldIndex], fi.FieldType, fi.GetValue(obj));
                        fi.SetValue(obj, value);
                        // 移动到下一个字段
                        ++fieldIndex;
                    }
                }
                // 捕获并处理异常
                catch (System.Exception e)
                {
                    // 抛出详细的解析错误信息
                    throw new System.Exception("数据表解析错误 " + filename + ":" + (lineIndex + 1) + " 列 " + (fieldIndex + 1) + " 成员索引 " + memberIndex + " 成员 " + member);
                }
            }
            // 将对象添加到rows列表
            sheet.rows.Add(obj);
        }
        // 返回加载的数据表
        return sheet;
    }
}
/// <summary>
/// 导入角色数据的类
/// 特性:全部成员可序列化
/// </summary>
[System.Serializable]
public class Obj
{
    /// 上面Datasheet<T>的T主要就是给这个类用
    /// 当然也通用其他类型
    // 定义各种字段
    public int act;
    public int type;
    public int id;
    public string description;
    public int objectId = -1;
    public int monstatId = -1;
    public int direction = 0;
    public string _base;
    public string token;
    public string mode;
    public string _class;
    public string[] layers = new string[16];
    public string colormap;
    public string index;
    public string eol;

    // 静态数据表实例，从文件加载
    public static Datasheet<Obj> sheet = Datasheet<Obj>.Load("Assets/d2/obj.txt");
    // 用于快速查找的字典
    static Dictionary<long, Obj> lookup = new Dictionary<long, Obj>();

    // 静态构造函数，初始化查找字典
    static Obj()
    {
        // 遍历所有数据行，添加到查找字典
        foreach (Obj obj in sheet.rows)
            lookup.Add(Key(obj.act, obj.type, obj.id), obj);
    }

    // 生成唯一键的方法
    static long Key(int act, int type, int id)
    {
        long key = act;
        // key的二进制表示下,向左移2位,貌似是用来压缩空间的,反正最终结果是得出了一个唯一的键
        key <<= 2;
        key += type;

        key <<= 32;
        key += id;

        return key;
    }

    // 查找方法，根据act, type, id查找对象
    static public Obj Find(int act, int type, int id)
    {
        Obj obj = null;
        lookup.TryGetValue(Key(act, type, id), out obj);
        return obj;
    }
}
/// <summary>
/// 这段代码定义了一个 ObjectInfo 类，用于存储游戏对象的各种属性信息，包括尺寸、动画、碰撞、光照等。
/// </summary>
[System.Serializable]
public class ObjectInfo
{
    // 对象名称
    public string name;
    // 对象描述
    public string description;
    // 对象ID
    public int id;
    // 对象标识符
    public string token;
    // 最大生成数量
    public int spawnMax;
    // 对象是否可选的布尔数组（8个方向）
    public bool[] selectable = new bool[8];
    // 陷阱概率
    public int trapProb;
    // 对象的X轴尺寸
    public int sizeX;
    // 对象的Y轴尺寸
    public int sizeY;
    // 目标FX数量
    public int nTgtFX;
    // 目标FY数量
    public int nTgtFY;
    // 目标BX数量
    public int nTgtBX;
    // 目标BY数量
    public int nTgtBY;
    // 帧数数组（8个方向）
    public int[] frameCount = new int[8];
    // 帧间隔数组（8个方向）
    public int[] frameDelta = new int[8];
    // 是否循环动画的布尔数组（8个方向）
    public bool[] cycleAnim = new bool[8];
    // 光照强度数组（8个方向）
    public int[] lit = new int[8];
    // 是否阻挡光线的布尔数组（8个方向）
    public bool[] blocksLight = new bool[8];
    // 是否有碰撞的布尔数组（8个方向）
    public bool[] hasCollision = new bool[8];
    // 是否可攻击
    public int isAttackable;
    // 起始帧数组（8个方向）
    public int[] start = new int[8];
    // 环境效果
    public int envEffect;
    // 是否是门
    public bool isDoor;
    // 是否阻挡视野
    public bool blocksVis;
    // 方向
    public int orientation;
    // 透明度
    public int trans;
    // 顺序标志数组（8个方向）
    public int[] orderFlag = new int[8];
    // 预操作
    public int preOperate;
    // 模式布尔数组（8个方向）
    public bool[] mode = new bool[8];
    // Y轴偏移
    public int yOffset;
    // X轴偏移
    public int xOffset;
    // 是否绘制
    public bool draw;
    // 红色值
    public int red;
    // 蓝色值
    public int blue;
    // 绿色值
    public int green;
    // 图层是否可选的布尔数组（16个图层）
    public bool[] layersSelectable = new bool[16];
    // 总部件数
    public int totalPieces;
    // 子类
    public int subClass;
    // X轴间距
    public int xSpace;
    // Y轴间距
    public int ySpace;
    // 名称偏移
    public int nameOffset;
    // 怪物是否可用
    public string monsterOk;
    // 操作范围
    public int operateRange;
    // 神殿功能
    public string shrineFunction;
    // 恢复功能
    public string restore;
    // 参数数组，包含8个整数值
    public int[] parm = new int[8];
    // 游戏章节（Act）
    public int act;
    // 是否可锁定
    public int lockable;
    // 血腥效果
    public int gore;
    // 同步状态
    public int sync;
    // 闪烁效果
    public int flicker;
    // 伤害值
    public int damage;
    // 测试版本标识
    public int beta;
    // 覆盖层
    public int overlay;
    // 碰撞替代
    public int collisionSubst;
    // 左侧位置
    public int left;
    // 顶部位置
    public int top;
    // 宽度
    public int width;
    // 高度
    public int height;
    // 操作函数
    public int operateFn;
    // 生成函数
    public int populateFn;
    // 初始化函数
    public int initFn;
    // 客户端函数
    public int clientFn;
    // 恢复处女状态
    public int restoreVirgins;
    // 是否阻挡导弹
    public int blocksMissile;
    // 是否在下方绘制
    public int drawUnder;
    // 是否开启传送门
    public int openWarp;
    // 自动地图
    public int autoMap;

    // 静态数据表实例，从文件加载
    public static Datasheet<ObjectInfo> sheet = Datasheet<ObjectInfo>.Load("Assets/d2/data/global/excel/objects.txt");
}