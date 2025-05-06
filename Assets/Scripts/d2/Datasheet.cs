using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;
using System.IO;

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
            if (fields.Length != members.Length)
                throw new System.Exception("导入数据与类型不匹配,类型: " + typeof(T) + " (" + members.Length + " 个成员) 导入数据: " + filename + ":" + (lineIndex + 1) + " (" + fields.Length + " 个字段)");

            // 跳过第一行（假设是标题行）
            if (lineIndex == 0)
                continue;

            // 创建新的T类型实例,这里就是约束了T必须有无参构造函数的原因
            T obj = new T();
            // 逐个字段处理
            for (int fieldIndex = 0; fieldIndex < fields.Length; ++fieldIndex)
            {
                // 获取字段信息
                FieldInfo fi = (FieldInfo)members[fieldIndex];
                // 转换字段值类型
                var value = System.Convert.ChangeType(fields[fieldIndex], fi.FieldType);
                // 设置字段值,字段信息类.SetValue(包含字段的类型, 字段本事);把形参2的值赋给形参1中的对应字段
                fi.SetValue(obj, value);
            }
            // 将对象添加到rows列表
            sheet.rows.Add(obj);
        }
        // 返回加载的数据表
        return sheet;
    }
}

/// <summary>
/// 导入角色数据的类,上面Datasheet<T>的T主要就是给这个类用,当然也通用其他类型
/// </summary>
/// 特性:全部成员可序列化
[System.Serializable]
public class Obj
{
    // 定义各种字段
    public int act;
    public int type;
    public int id;
    public string description;
    public string objectId;
    public string monstatId;
    public string direction;
    public string _base;
    public string token;
    public string mode;
    public string _class;
    public string HD;
    public string TR;
    public string LG;
    public string RA;
    public string LA;
    public string RH;
    public string LH;
    public string SH;
    public string S1;
    public string S2;
    public string S3;
    public string S4;
    public string S5;
    public string S6;
    public string S7;
    public string S8;
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
        foreach(Obj obj in sheet.rows)
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