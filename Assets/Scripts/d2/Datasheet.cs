using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;
using System.IO;
using UnityEngine;

/// <summary>
/// [自定义容器类]CSV文件读取器
/// </summary>
/// <typeparam name="T"></typeparam>
/// <remarks>
/// 用于读取和储存CSV文件中的数据,存在成员rows列表里
/// </remarks>
public struct Datasheet<T> where T : new()
{
    /// <summary>
    /// 用CSV文件中读取的数据生成的T对象,存在这里
    /// 一个T类对象对应的是一行数据
    /// </summary>
    public List<T> rows;

    /// <summary>
    /// 尝试重新生成值
    /// </summary>
    /// <param name="value">CSV字段数据</param>
    /// <param name="type">需要生成目标类型</param>
    /// <param name="defaultValue">本体</param>
    /// <returns>生成的值或者本体(如果生成失败)</returns>
    /// <remarks>
    /// 因为GetValue获取的数组是object类型, 为了能用需要重新生成,生成成功就返回新生成的,没成功就继续用本体
    /// </remarks>
    static object CastValue(string value, System.Type type, object defaultValue)
    {
        //如果是空值或xxx,返回本体
        if (value == "" || value == "xxx")
            return defaultValue;
        
        //如果是bool类型
        if (type == typeof(bool))
        {
            if (value == "1")
                return true;
            else if (value == "0")
                return false;
            else
                throw new System.FormatException("无法将值‘" + value + "’转换为布尔类型");
        }
        //其他非bool类型
        else
        {
            //把value数据转换成type类型,并返回
            return System.Convert.ChangeType(value, type);
        }
    }

    public static Datasheet<T> Load(string filename)
    {
        #region 前期准备,读取CSV文件,计算T成员数
        //CSV文件就是用制表符分割的数据文件
        string csv = File.ReadAllText(filename);
        //把T的所有可序列化成员都拿出来转成数组
        MemberInfo[] members = FormatterServices.GetSerializableMembers(typeof(T));
        //计算T成员数
        int expectedFieldCount = 0;
        //T临时对象变量
        T dummy = new T();
        //计算members数组的长度(如包含数组成员,数组内的元素也会被计算)
        foreach (MemberInfo member in members)
        {
            //成员属性类转为字段属性类,这样可以进行一些字段专属的操作
            FieldInfo fi = (FieldInfo)member;
            //如果时数组
            if (fi.FieldType.IsArray)
            {
                //获取dummy中的fi类型实例>>>转成Ilist接口类型>>>返回数组长度
                expectedFieldCount += ((System.Collections.IList)fi.GetValue(dummy)).Count;
            }
            //如果不是数组
            else
            {
                //成员长度+1就行
                expectedFieldCount += 1;
            }
        }
        #endregion

        //新建一个临时的Datasheet容器,并初始化rows成员,用于返回
        Datasheet<T> sheet = new Datasheet<T>();
        sheet.rows = new List<T>();

        #region 分割CSV文件
        //行数组
        var lines = csv.Split('\n');
        //遍历每行
        for (int lineIndex = 0; lineIndex < lines.Length; ++lineIndex)
        {
            #region 处理行
            //去掉行首尾的空白字符
            string line = lines[lineIndex].Trim();
            //直接清空了,那这行就拜拜
            if (line.Length == 0)
                continue;
            //每行按制表符再分一遍,字段数组
            var fields = line.Split('\t');
            //字段和成员数不匹配就报错
            if (fields.Length != expectedFieldCount)
                throw new System.Exception("字段数量不匹配: " + typeof(T) + "（期望: " + expectedFieldCount + "个字段）在" + filename + "文件中：第" + (lineIndex + 1) + "行（实际: " + fields.Length + "个字段）");
            //第一行是标题,跳过
            if (lineIndex == 0)
                continue;
            #endregion

            //新建一个T类型临时实例,用作赋值给字典的对象
            T obj = new T();
            //指针,对应行的字段和T的成员
            int memberIndex = 0;
            //遍历行的每个字段
            for (int fieldIndex = 0; fieldIndex < fields.Length; ++memberIndex)
            {
                #region 处理字段
                //获取T的成员
                MemberInfo member = members[memberIndex];
                //成员转字段
                FieldInfo fi = (FieldInfo)member;
                //尝试
                try
                {
                    //如果T的这个字段是数组
                    if (fi.FieldType.IsArray)
                    {
                        //获取数组中元素的类型
                        var elementType = fi.FieldType.GetElementType();
                        //获取obj中fi的实例
                        var array = (System.Collections.IList)fi.GetValue(obj);
                        //遍历这个实例数组
                        for (int i = 0; i < array.Count; ++i)
                        {
                            //尝试通过CastValue重新生成值,并赋值给数组元素,因为GetValue生成的数组是object类型,用不了
                            array[i] = CastValue(fields[fieldIndex], elementType, array[i]);
                            ++fieldIndex;
                        }
                    }
                    //如果T的这个字段不是数组
                    else
                    {
                        //重新生成和上面那个一个意思
                        var value = CastValue(fields[fieldIndex], fi.FieldType, fi.GetValue(obj));
                        //给obj的fi赋值
                        fi.SetValue(obj, value);
                        ++fieldIndex;
                    }
                }
                //如果尝试失败
                catch (System.Exception e)
                {
                    throw new System.Exception("数据表解析错误在, " + filename + "文件:" + (lineIndex + 1) + " 列 " + (fieldIndex + 1) + " 字段, " + typeof(T) + " 类型的第" + memberIndex + " 个成员: " + member);
                }
                #endregion
            }
            //生成T类型实例Add到Datasheet容器类的列表中,对应的是一行数据
            sheet.rows.Add(obj);
        }
        #endregion

        return sheet;
    }
}


/// <summary>
/// 对象表,对象的主要参数
/// </summary>
/// <remarks>
/// 静态成员被调用或者初始化时,所有静态成员都会被初始化,包括两个容器和实例; 
/// [注意] 这个类看似是个单例,但是它是被内部成员sheet容器初始化的,所以它仍然可以创建很多个实例
/// </remarks>
[System.Serializable]
public class Obj
{
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

    /// <summary>
    /// 工作表 -- 初始化1
    /// </summary>
    /// <remarks>
    /// 在初始化时读取文件中的数据,生成一个储存Obj类型的Datasheet容器
    /// </remarks>
    public static Datasheet<Obj> sheet = Datasheet<Obj>.Load("Assets/d2/obj.txt");
    /// <summary>
    /// 映射字典,Key是通过obj对象的(act,type,id)算出来的
    /// </summary>
    static Dictionary<long, Obj> lookup = new Dictionary<long, Obj>();

    /// <summary>
    /// 实例 -- 初始化3
    /// </summary>
    static Obj()
    {
        //遍历sheet容器中的所有实例,给实例映射一个Key
        foreach (Obj obj in sheet.rows)
        {
            lookup.Add(Key(obj.act, obj.type, obj.id), obj);
        }
    }
    /// <summary>
    /// 生成Key
    /// </summary>
    /// <param name="act">幕</param>
    /// <param name="type">类型</param>
    /// <param name="id">id</param>
    /// <returns>Key</returns>
    /// <remarks>
    /// 通过obj对象的(act,type,id)生成一个唯一的Key,用于查找
    /// </remarks>
    static long Key(int act, int type, int id)
    {
        long key = act;

        key <<= 2;
        key += type;

        key <<= 32;
        key += id;

        return key;
    }

    static public Obj Find(int act, int type, int id)
    {
        Obj obj = null;
        lookup.TryGetValue(Key(act, type, id), out obj);
        return obj;
    }
}

[System.Serializable]
public class ObjectInfo
{
    public string name;
    public string description;
    public int id;
    public string token;
    public int spawnMax;
    public bool[] selectable = new bool[8];
    public int trapProb;
    public int sizeX;
    public int sizeY;
    public int nTgtFX;
    public int nTgtFY;
    public int nTgtBX;
    public int nTgtBY;
    public int[] frameCount = new int[8];
    public int[] frameDelta = new int[8];
    public bool[] cycleAnim = new bool[8];
    public int[] lit = new int[8];
    public bool[] blocksLight = new bool[8];
    public bool[] hasCollision = new bool[8];
    public int isAttackable;
    public int[] start = new int[8];
    public int envEffect;
    public bool isDoor;
    public bool blocksVis;
    public int orientation;
    public int trans;
    public int[] orderFlag = new int[8];
    public int preOperate;
    public bool[] mode = new bool[8];
    public int yOffset;
    public int xOffset;
    public bool draw;
    public int red;
    public int blue;
    public int green;
    public bool[] layersSelectable = new bool[16];
    public int totalPieces;
    public int subClass;
    public int xSpace;
    public int ySpace;
    public int nameOffset;
    public string monsterOk;
    public int operateRange;
    public string shrineFunction;
    public string restore;
    public int[] parm = new int[8];
    public int act;
    public int lockable;
    public int gore;
    public int sync;
    public int flicker;
    public int damage;
    public int beta;
    public int overlay;
    public int collisionSubst;
    public int left;
    public int top;
    public int width;
    public int height;
    public int operateFn;
    public int populateFn;
    public int initFn;
    public int clientFn;
    public int restoreVirgins;
    public int blocksMissile;
    public int drawUnder;
    public int openWarp;
    public int autoMap;

    public static Datasheet<ObjectInfo> sheet = Datasheet<ObjectInfo>.Load("Assets/d2/data/global/excel/objects.txt");
}