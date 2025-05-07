using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// DT1索引类，用于管理和查找DT1瓦片数据
class DT1Index
{
    // 存储瓦片种类与对应瓦片列表映射表的字典
    Dictionary<int, List<DT1.Tile>> tiles = new Dictionary<int, List<DT1.Tile>>();
    // 存储瓦片种类与稀有度总和映射表的字典
    new Dictionary<int, int> rarities = new Dictionary<int, int>();
    // DT1文件计数器
    int dt1Count = 0;

    //将列表中的瓦片添加到字典中去
    public void Add(DT1.Tile[] newTiles)
    {
        // 遍历这个列表中的所有瓦片
        foreach (var tile in newTiles)
        {
            // 当前遍历到的瓦片的种类,字典里是否有,如果有就把字典里这个瓦片列表拿出来赋值,不存在就赋值null
            List<DT1.Tile> list = tiles.GetValueOrDefault(tile.index, null);
            // 如果不存在，那就是字典里没有这类瓦片
            // 那就为这类瓦片新建一个列表,并且加入到字典中
            if (list == null)
            {
                list = new List<DT1.Tile>();
                tiles[tile.index] = list;
            }

            // 如果是第一个DT1文件，遍历到的瓦片是从列表的开头加入
            // 如果不是第一个DT1文件,遍历到的瓦片是从列表的结尾加入
            if (dt1Count == 0)
                list.Insert(0, tile);
            else
                list.Add(tile);

            // 查找当前瓦片的种类是否存在于稀有度映射中
            // 如果不存在，就把这个种类和这个瓦片的稀有度加入到映射中
            if (!rarities.ContainsKey(tile.index)) rarities[tile.index] = tile.rarity;
            // 如果存在，就把这个种类的原稀有度加上这个瓦片的稀有度
            else rarities[tile.index] += tile.rarity;
        }
        // 遍历完了这组列表,DT1文件计数加1
        dt1Count += 1;
    }

    // 根据瓦片类型索引查找瓦片
    public bool Find(int index, out DT1.Tile tile)
    {
        //新建一个瓦片列表,用来储存找到的这类瓦片
        List<DT1.Tile> tileList;
        // 根据瓦片类型索引查找瓦片列表
        //如果找到了就把找到的列表赋值给tileList
        // 如果没找到就给个默认值的瓦片,返回结果为false
        if (!tiles.TryGetValue(index, out tileList))
        {
            tile = new DT1.Tile();
            return false;
        }

        // 获取当前索引的稀有度总和
        int raritySum = rarities[index];
        // 如果稀有度总和为0，直接out第一个瓦片
        if (raritySum == 0)
        {
            tile = tileList[0];
        }
        // 如果稀有度总和不为0
        else
        {
            // 随机选择一个瓦片，跳过稀有度为0的瓦片
            int randomIndex = Random.Range(0, tileList.Count - 1);
            //如果稀有度是0,就会跳过这个瓦片,选它的下一个
            while (tileList[randomIndex].rarity == 0)
            {
                //稀有度为0,就+1;;余一下可以保证不溢出,并且+1能一直循环
                randomIndex = (randomIndex + 1) % tileList.Count;
            }
            // 随机选择一个瓦片，跳过稀有度为0的瓦片
            tile = tileList[randomIndex];
        }
        
        // 返回查找成功
        return true;
    }
}

/// <summary>
/// DS1文件读取器
/// </summary>
/// <remarks>
/// 读取
/// </remarks>
public class DS1
{
    /// <summary>
    /// 单元格,这里的单元格貌似指的是瓦片对应的格子（每个格子10字节）
    /// </summary>
    struct Cell
    {
        /// <summary>
        /// 属性1（通常为材质索引）
        /// </summary>
        public byte prop1;
        /// <summary>
        /// 属性2（子材质索引/动画帧数）
        /// </summary>
        public byte prop2;
        /// <summary>
        /// 属性3（高位4bit为主材质索引）
        /// </summary>
        public byte prop3;
        /// <summary>
        /// 属性4（低2bit补充主材质索引）
        /// </summary>
        public byte prop4;
    };
    // 导入结果结构体，用于给Import导入方法,返回结果用
    public struct ImportResult
    {
        public Vector3 center;  // 地图中心点坐标
        public Vector3 entry;    // 玩家入口点坐标
    }

    // 方向查找表，用于将原始方向值转换为实际方向
    static byte[] dirLookup = 
    {
        0x00, 0x01, 0x02, 0x01, 0x02, 0x03, 0x03, 0x05, 0x05, 0x06,
        0x06, 0x07, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E,
        0x0F, 0x10, 0x11, 0x12, 0x14
    };
    // 地图入口索引，通过DT1.Tile.Index方法生成，参数为(30, 11, 10),它是静态且只读的
    static readonly int mapEntryIndex = DT1.Tile.Index(30, 11, 10);
    // 城镇入口索引，通过DT1.Tile.Index方法生成，参数为(30, 11, 11),它是静态且只读的
    static readonly int townEntryIndex = DT1.Tile.Index(30, 0, 10);
    // 城镇入口索引2，通过DT1.Tile.Index方法生成，参数为(31, 11, 11),它是静态且只读的
    static readonly int townEntry2Index = DT1.Tile.Index(31, 0, 10);

    /// <summary>
    /// 导入一个ds1文件
    /// </summary>
    /// <param name="ds1Path">ds1文件路径</param>
	static public ImportResult Import(string ds1Path, GameObject monsterPrefab = null, GameObject objectPrefab = null)
    {
        //System.Diagnostics.Stopwatch时.NET自带的计时器,可以用来测量代码执行时间
        //StartNew()方法用于创建一个新的计时器实例并开始计时
        var sw = System.Diagnostics.Stopwatch.StartNew();

        //>>>>>>>>生成文件流
        //File.ReadAllBytes(ds1Path)打开一个文件进行读取,返回一个FileStream对象
        //MemoryStream是用于内存读取,提高读取效率,适用于频繁的小数据读取
        //>>>注释掉的这段代码是缓冲流读取,是读取硬盘中的文件,适用于大文件读取,速度慢
        //>>>但是DS1文件一般都很小,所以用MemoryStream读取就足够了,而且更快
        //var stream = new BufferedStream(File.OpenRead(ds1Path));
        var stream = new MemoryStream(File.ReadAllBytes(ds1Path));
        //BinaryReader是用于从二进制流中读取数据,适用于读取二进制文件
        var reader = new BinaryReader(stream);

        //>>>>>>>>>>>读取文件
        //>>>>>>>>>>>reader.ReadInt32();读取4个字节
        //>>>>>读取之后就会把读取的位置自动往后移4个字节,就好像文件内容跟流水线一样,叫文件流可能是这个原因

        //>>>>>>>读取基础信息
        //读取版本号
        int version = reader.ReadInt32();
        //>>>>>>文件中的计数是从0开始,所以如果是计数的单位,一般都要+1.像版本号这种非计数的单位,就不需要+1了
        //读取地图宽度
        int width = reader.ReadInt32() + 1;
        //读取地图高度
        int height = reader.ReadInt32() + 1;


        //>>>>>>根据版本号,进行不同分支处理,条件基本上都是>=,越大的数字,需要经历的分支就越多

        //[[[版本号 >= 8]]]
        //新建一个幕号的变量
        int act = 1;
        //如果版本号大于等于8,就读取幕号
        if (version >= 8)
        {
            //读取幕号
            act = reader.ReadInt32() + 1;
            //如果幕号大于5,就把幕号设为5
            act = Mathf.Min(act, 5);
        }
        // 根据幕数加载对应的调色板,如果版本号不>=8,就加载默认的调色板1
        Palette.LoadPalette(act);

        //[[[版本号 >= 10]]]
        //新建一个标签类型的变量(如果版本号不>=10,就用默认值0)
        int tagType = 0;
        //如果版本号大于等于10,就读取标签类型
        if (version >= 10)
        {
            //读取标签类型
            tagType = reader.ReadInt32();

            //// adjust eventually the # of tag layer
            //if ((tagType == 1) || (tagType == 2))
            //    tagLayerCount = 1;
        }
         // 创建一个DT1索引对象，用于管理和查找DT1瓦片数据
        var dt1Index = new DT1Index();
        // 总瓦片数量，初始化为0
        int totalTiles = 0;
        //[[[版本号 >= 3]]]
        //如果版本号大于等于3,就读取文件数量
        if (version >= 3)
        {
            //读取文件的长度
            int fileCount = reader.ReadInt32();
            //遍历这个文件数量
            for (int i = 0; i < fileCount; i++)
            {
                //新建一个文件名的变量,用来存储文件名
                string filename = "";
                //新建一个字符的变量,储存当前流读取到的字符
                char c;
                //循环读取流中的单个字符,直到读取到0为止
                while ((c = reader.ReadChar()) != 0)
                {
                    //把读取到的字符拼接到文件名中
                    filename += c;
                }
                //如果文件名以.tg1结尾,就把.tg1替换成.dt1
                filename = filename.Replace(".tg1", ".dt1");
                //导入dt1文件的方法,返回瓦片数组(DT1文件的瓦片结构体)
                var imported = DT1.Import("Assets" + filename);
                //导入的数组长度添加到瓦片总数中
                totalTiles += imported.tiles.Length;
                //把这个DT列表添加到映射字典里去
                dt1Index.Add(imported.tiles);
            }
        }
        //中止计时器,并debug输出DT1文件的加载时间
        sw.Stop();
        Debug.Log("DT1 加载 用 " + sw.Elapsed + "时间");
        //重置计时器并重新开始计时
        sw.Reset();
        sw.Start();

        //如果版本在9--13之间,就跳过2个字节
        if ((version >= 9) && (version <= 13)) reader.ReadBytes(2);

        //新建4个图层
        int wallLayerCount = 1; // 墙壁图层数量
        int floorLayerCount = 1; // 地板图层数量
        int shadowLayerCount = 1; // 阴影图层数量
        int tagLayerCount = 0; // 标签图层数量

        //[[[版本号 >= 4]]]
        if (version >= 4)
        {
            //读取墙壁和方向图层数量
            wallLayerCount = reader.ReadInt32();
            //同时如果[[[版本号 >= 16]]]
            if (version >= 16)
            {
                //读取地板图层数量
                floorLayerCount = reader.ReadInt32();
            }
        }
        //否则
        else
        {
            //标签图层数量设为1
            tagLayerCount = 1;
        }
        //输出图层数量:(2 * wallLayerCount 墙壁) + floorLayerCount 地板 + shadowLayerCount 阴影 + tagLayerCount 标签
        Debug.Log("图层数量分别为 : (2 * " + wallLayerCount + " 墙壁) + " + floorLayerCount + " 地板 + " + shadowLayerCount + " 阴影 + " + tagLayerCount + " 标签");
        //新建一个单元格数组,用来存储墙壁数据,Cell[第几图层][第几块墙壁]
        Cell[][] walls = new Cell[wallLayerCount][];
        //遍历地板图层数量,给墙壁的每一个图层添加全地图大小的瓦片数组
        for (int i = 0; i < wallLayerCount; ++i) walls[i] = new Cell[width * height];
        //新建图层总数变量
        int layerCount = 0;
        //新建一个布局数组,用来存储图层顺序
        int[] layout = new int[14];
        //如果版本号小于4,每种图层只有一层
        if (version < 4)
        {

            layout[0] = 1; // 墙 1
            layout[1] = 9; // 地板 1
            layout[2] = 5; // 朝向 1
            layout[3] = 12; // 标签
            layout[4] = 11; // 阴影
            layerCount = 5; // 总图层数
        }
        //否则,版本号大于等于4,每种图层有多层
        else
        {
            //>>>>根据文件流中读取的图层层数填充图层数组
            //图层计数为0
            layerCount = 0;
            //遍历墙壁和朝向图层数量
            for (int x = 0; x < wallLayerCount; x++)
            {
                //每次遍历:把值添加到数组里面,墙从1开始,朝向从5开始
                layout[layerCount++] = 1 + x; // wall x
                layout[layerCount++] = 5 + x; // orientation x
            }
            //遍历地板的图层数量
            for (int x = 0; x < floorLayerCount; x++)
            {
                //地板图层从9开始
                layout[layerCount++] = 9 + x; // floor x
            }
            //阴影和标签加入到数组中,分别时11和12
            if (shadowLayerCount != 0) layout[layerCount++] = 11;    // shadow
            if (tagLayerCount != 0) layout[layerCount++] = 12;    // tag
        }

        // 创建一个根游戏对象做为根节点，使用DS1文件名作为对象名称
        GameObject root = new GameObject(Path.GetFileName(ds1Path));
        // 创建地板图层数组，大小为地板图层数量
        var floorLayers = new GameObject[floorLayerCount];
        // 遍历地板图层数量，为每个图层创建游戏对象做为本层节点
        for(int i = 0; i < floorLayerCount;  ++i)
        {
            // 创建地板图层对象，命名为f+图层序号,做为本层节点
            floorLayers[i] = new GameObject("f" + (i + 1));
            // 将地板图层节点设置为根节点的子节点,入f1,f2等
            floorLayers[i].transform.SetParent(root.transform);
        }

        // 创建墙壁图层数组，大小为墙壁图层数量
        var wallLayers = new GameObject[wallLayerCount];
        // 遍历墙壁图层数量，为每个图层创建游戏对象做为本层节点
        for (int i = 0; i < wallLayerCount; ++i)
        {
            // 创建墙壁图层对象，命名为w+图层序号,做为本层节点
            wallLayers[i] = new GameObject("w" + (i + 1));
            // 将墙壁图层节点设置为根节点的子节点
            wallLayers[i].transform.SetParent(root.transform);
        }

        // 创建导入结果对象
        var importResult = new ImportResult();
        // 计算地图中心点坐标，将地图宽度和高度转换为世界坐标后除以2
        importResult.center = MapToWorld(width, height) / 2;
        // 将入口点坐标设置为地图中心点坐标
        importResult.entry = importResult.center;
        //遍历整个图层总数
        for (int n = 0; n < layerCount; n++)
        {
            int p;
            //当前图层的第几个瓦片
            int i = 0;
            //每一层图层都会遍历地图的每一瓦片,根据文件信息赋值属性(应该是允许有空白的)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //根据当前当前所在的图层,进行不同处理
                    switch (layout[n])
                    {
                        //1-4是墙图层
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        {
                            //这里推断数据文件内的层级是从0开始,所以都要减去起始层数
                            p = layout[n] - 1;
                            //[墙的第几个图层][本层的第几个瓦片]的4项属性,读取文件流数据
                            walls[p][i].prop1 = reader.ReadByte();
                            walls[p][i].prop2 = reader.ReadByte();
                            walls[p][i].prop3 = reader.ReadByte();
                            walls[p][i].prop4 = reader.ReadByte();
                            //中止switch
                            break;
                        }
                        //5-8是朝向图层,p=n-5,应该就是朝向的第几层,从0-3,读取一个字节做o,再跳过三个字节(读取了没赋值)
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                        {
                            //这里推断数据文件内的层级是从0开始,所以都要减去起始层数
                            p = layout[n] - 5;
                            //可能朝向信息都是一样的没有下标,直接取数据
                            int orientation = reader.ReadByte();
                            //如果版本小于7,orientation代表的是朝向数组的下标,那么需要用朝向数组,取一下实际的朝向信息
                            if (version < 7) orientation = dirLookup[orientation];
                            //跳过三个字节
                            reader.ReadBytes(3);
                            //如果当前单元格墙壁的材质索引是0,那就中止switch,因为这个单元格是没有墙壁的
                            if (walls[p][i].prop1 == 0) break;
                            //新建一个变量,用来存储朝向的剩下三项属性
                            int prop2 = walls[p][i].prop2;
                            int prop3 = walls[p][i].prop3;
                            int prop4 = walls[p][i].prop4;
                            //计算主材质索引,高位4bit为主材质索引
                            int mainIndex = (prop3 >> 4) + ((prop4 & 0x03) << 4);
                            //子材质索引直接使用prop2
                            int subIndex = prop2;
                            //计算最终的材质索引
                            int index = DT1.Tile.Index(mainIndex, subIndex, orientation);
                            //如果这个索引是地图入口,就把这个坐标赋值给importResult.entry
                            if (index == mapEntryIndex)
                            {
                                importResult.entry = MapToWorld(x, y);
                                Debug.Log("地图入口在此坐标 " + x + " " + y);
                                break;
                            }
                            //如果这个索引是城镇入口,就把这个坐标赋值给importResult.entry
                            else if (index == townEntryIndex)
                            {
                                importResult.entry = MapToWorld(x, y);
                                Debug.Log("城镇入口在此坐标 " + x + " " + y);
                                break;
                            }
                            DT1.Tile tile;
                            //在瓦片映射字典里找到index这一类材质的瓦片,随机一个稀有度不为0的瓦片赋值给tile
                            if (dt1Index.Find(index, out tile))
                            {
                                ///创建这个瓦片
                                var tileObject = CreateTile(tile, x, y);
                                //把这个瓦片放到墙壁图层的第p层节点下面
                                tileObject.transform.SetParent(wallLayers[p].transform);
                            }
                            else
                            {
                                Debug.LogWarning("找不到墙壁瓦片的材质 (材质索引: " + mainIndex + " " + subIndex + " " + orientation + ") 等距坐标: " + x + ", " + y);
                            }
                            //朝向信息等于3
                            if (orientation == 3)
                            {
                                //获取材质索引
                                index = DT1.Tile.Index(mainIndex, subIndex, 4);
                                //随机再容器中获取一个这类材质的瓦片对象
                                if (dt1Index.Find(index, out tile))
                                {
                                    //创建瓦片
                                    var tileObject = CreateTile(tile, x, y);
                                    //把这个瓦片放到墙壁图层的第p层节点下面
                                    tileObject.transform.SetParent(wallLayers[p].transform);
                                }
                                //如果找不到就报错
                                else
                                {
                                Debug.LogWarning("找不到墙壁瓦片的材质 (材质索引: " + mainIndex + " " + subIndex + " " + orientation + ") 等距坐标: " + x + ", " + y);
                                }
                            }
                        
                            //中止switch
                            break;
                        }
                        //9-10是地板层,p=n-9,应该就是地板的第几层,从0-1,读取4个字节,分别是prop1,prop2,prop3,prop4,四个属性
                        case 9:
                        case 10:
                        {
                            //这里推断数据文件内的层级是从0开始,所以都要减去起始层数
                            p = layout[n] - 9;
                            int prop1 = reader.ReadByte();   // 属性1（通常为材质索引）
                            int prop2 = reader.ReadByte();   // 属性2（子材质索引/动画帧数）
                            int prop3 = reader.ReadByte();   // 属性3（高位4bit为主材质索引）
                            int prop4 = reader.ReadByte();   // 属性4（低2bit补充主材质索引）
                            //>>>>下面这段看不懂了,需要了解暗黑2MOD的原理才能看懂
                            //注释一下大概意思
                            //材质为空就中止switch
                            if (prop1 == 0) break;
                            //计算主材质索引
                            int mainIndex = (prop3 >> 4) + ((prop4 & 0x03) << 4);
                            //子材质索引直接使用prop2
                            int subIndex = prop2;
                            //新建变量方向索引,默认0
                            int orientation = 0;
                            //计算最终的材质索引
                            int index = DT1.Tile.Index(mainIndex, subIndex, orientation);
                            //新建变量,临时瓦片变量
                            DT1.Tile tile;
                            //找到对应的材质类型的瓦片,随机一个稀有度不为0的瓦片赋值给tile
                            if (dt1Index.Find(index, out tile))
                            {
                                //创建一个地板对象
                                var tileObject = CreateTile(tile, x, y, orderInLayer: p);
                                //放在地板层的第p层节点下面
                                tileObject.transform.SetParent(floorLayers[p].transform);
                            }
                            break;
                        }
                        //11是阴影层,跳过4个字节(读取了没赋值)
                        case 11:
                            reader.ReadBytes(4);
                            //if ((x < new_width) && (y < new_height))
                            //{
                            //    p = layout[n] - 11;
                            //    s_ptr[p]->prop1 = *bptr;
                            //    bptr++;
                            //    s_ptr[p]->prop2 = *bptr;
                            //    bptr++;
                            //    s_ptr[p]->prop3 = *bptr;
                            //    bptr++;
                            //    s_ptr[p]->prop4 = *bptr;
                            //    bptr++;
                            //    s_ptr[p] += shadowLayerCount;
                            //}
                            //else
                            //    bptr += 4;
                            break;

                        //12是标签层,跳过4个字节(读取了没赋值)
                        case 12:
                            reader.ReadBytes(4);
                            //if ((x < new_width) && (y < new_height))
                            //{
                            //    p = layout[n] - 12;
                            //    t_ptr[p]->num = (UDWORD) * ((UDWORD*)bptr);
                            //    t_ptr[p] += tagLayerCount;
                            //}
                            //bptr += 4;
                            break;
                    }
                    //每遍历完一个单元格,就把i+1,表示下一个单元格
                    ++i;
                }
            }
        }
        //[[[如果版本号>=2]]]
        if (version >= 2)
        {
            //读取对象数量,读4个字节
            int objectCount = reader.ReadInt32();
            //debug输出对象数量
            Debug.Log("对象数 " + objectCount);
            //遍历对象数量
            for (int n = 0; n < objectCount; n++)
            {
                //读取对象类型,读4个字节
                int type = reader.ReadInt32();
                //读取对象ID,读4个字节
                int id = reader.ReadInt32();
                //读取对象的x和y坐标,各读4个字节
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                //同时[[[如果版本号>=5]]]
                if (version > 5)
                {
                    //读取对象的flags,读4个字节
                    int flags = reader.ReadInt32();
                }
                //新建一个变量,用来读取地图的对象
                Obj obj = Obj.Find(act, type, id);
                //如果对象类型是1,并且有怪物预制体
                if (type == 1 && monsterPrefab != null)
                {
                    //获取这个子单元格的世界坐标
                    var pos = MapSubCellToWorld(x, y);
                    //创建一个怪物对象,并把这个坐标赋值给它
                    var monster = GameObject.Instantiate(monsterPrefab, pos, Quaternion.identity);
                    //给怪物名字赋值
                    monster.name = obj.description;
                    //放在根目录下面
                    monster.transform.SetParent(root.transform);
                }
                // 如果对象类型是2（物体）并且有物体预制体
                if (type == 2 && objectPrefab != null)
                {
                    // 获取子单元格的世界坐标
                    var pos = MapSubCellToWorld(x, y);
                    // 声明一个游戏对象变量
                    GameObject gameObject;
                    // 如果当前幕是1，对象类型是2，对象ID是2（特定物体）
                    if (act == 1 && type == 2 && id == 2)
                    {
                        // 加载DCC动画文件
                        var dcc = DCC.Load("Assets/d2/data/global/objects/RB/TR/rbtrlitonhth.dcc");
                        // 输出对象的基础信息
                        Debug.Log(obj._base + " " + obj.token + " " + obj.mode + " " + obj._class);
                        // 创建新的游戏对象
                        gameObject = new GameObject();
                        // 设置对象位置
                        gameObject.transform.position = pos;
                        // 添加SpriteRenderer组件
                        gameObject.AddComponent<SpriteRenderer>();
                        // 添加IsoAnimator组件并设置动画
                        var animator = gameObject.AddComponent<IsoAnimator>();
                        animator.anim = dcc.anim;
                    }
                    else
                    {
                        // 使用预制体实例化对象
                        gameObject = GameObject.Instantiate(objectPrefab, pos, Quaternion.identity);
                    }
                    // 设置对象名称
                    gameObject.name = obj.description;
                    // 将对象设置为根节点的子对象
                    gameObject.transform.SetParent(root.transform);                }
            }
        }
        //关闭流
        stream.Close();
        //中止计时器,并debug输出DS1文件的加载时间
        sw.Stop();
        Debug.Log("DT1 加载 用 " + sw.Elapsed + "时间");
        //返回导入结果
        return importResult;
    }

    // 将地图坐标转换为世界坐标
    static Vector3 MapToWorld(int x, int y)
    {
        // 使用Iso.MapToWorld方法将地图坐标转换为世界坐标，并除以瓦片大小
        var pos = Iso.MapToWorld(new Vector3(x, y)) / Iso.tileSize;
        // 反转Y轴坐标
        pos.y = -pos.y;
        // 返回转换后的世界坐标
        return pos;
    }

    /// <summary>
    /// 将子单元格坐标转换为世界坐标
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    static Vector3 MapSubCellToWorld(int x, int y)
    {
        // 使用Iso.MapToWorld方法将子单元格坐标转换为世界坐标，并减去偏移量(2,2)
        var pos = Iso.MapToWorld(new Vector3(x - 2, y - 2));
        // 反转Y轴坐标
        pos.y = -pos.y;
        // 返回转换后的世界坐标
        return pos;
    }

    // 创建一个瓦片的游戏对象
    static GameObject CreateTile(DT1.Tile tile, int x, int y, int orderInLayer = 0)
    {
        // 获取瓦片的纹理
        var texture = tile.texture;
        // 将地图坐标转换为世界坐标
        var pos = MapToWorld(x, y);

        // 创建新的游戏对象
        GameObject gameObject = new GameObject();
        // 设置游戏对象名称：主索引_子索引_朝向
        gameObject.name = tile.mainIndex + "_" + tile.subIndex + "_" + tile.orientation;
        // 设置游戏对象位置
        gameObject.transform.position = pos;
        // 添加MeshRenderer组件
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        // 添加MeshFilter组件
        var meshFilter = gameObject.AddComponent<MeshFilter>();
        // 创建新的Mesh对象
        Mesh mesh = new Mesh();
        // 获取瓦片的纹理坐标
        float x0 = tile.textureX;
        float y0 = tile.textureY;
        // 计算瓦片的宽度和高度（转换为世界单位）
        float w = tile.width / Iso.pixelsPerUnit;
        float h = (-tile.height) / Iso.pixelsPerUnit;

        // 如果瓦片朝向为0或者15（默认朝向）
        if(tile.orientation == 0 || tile.orientation == 15)
        {
            // 设置左上角顶点位置
            var topLeft = new Vector3(-1f, 0.5f);
            //如果朝向是15,就把y坐标加上瓦片的高度
            if (tile.orientation == 15) topLeft.y += tile.roofHeight / Iso.pixelsPerUnit;
            // 设置网格顶点
            mesh.vertices = new Vector3[] {
                topLeft,
                topLeft + new Vector3(0, -h),
                topLeft + new Vector3(w, -h),
                topLeft + new Vector3(w, 0)
            };
            // 设置网格三角形（两个三角形组成四边形）
            mesh.triangles = new int[] { 2, 1, 0, 3, 2, 0 };
            // 设置UV坐标
            mesh.uv = new Vector2[] {
                new Vector2 (x0 / texture.width, -y0 / texture.height),
                new Vector2 (x0 / texture.width, (-y0 +tile.height) / texture.height),
                new Vector2 ((x0 + tile.width) / texture.width, (-y0 +tile.height) / texture.height),
                new Vector2 ((x0 + tile.width) / texture.width, -y0 / texture.height)
            };

            // 设置渲染顺序
            meshRenderer.sortingLayerName = tile.orientation == 0 ? "Floor" : "Roof";
            meshRenderer.sortingOrder = orderInLayer;
        }
        // 其他朝向的瓦片
        else
        {
            // 设置左上角顶点位置
            var topLeft = new Vector3(-1f, h - 0.5f);
            // 设置网格顶点
            mesh.vertices = new Vector3[] {
                topLeft,
                topLeft + new Vector3(0, -h),
                topLeft + new Vector3(w, -h),
                topLeft + new Vector3(w, 0)
            };
            // 设置网格三角形
            mesh.triangles = new int[] { 2, 1, 0, 3, 2, 0 };
            // 设置UV坐标
            mesh.uv = new Vector2[] {
                new Vector2 (x0 / texture.width, (-y0 - tile.height) / texture.height),
                new Vector2 (x0 / texture.width, -y0 / texture.height),
                new Vector2 ((x0 + tile.width) / texture.width, -y0 / texture.height),
                new Vector2 ((x0 + tile.width) / texture.width, (-y0 - tile.height) / texture.height)
            };
            // 根据位置设置渲染顺序
            meshRenderer.sortingOrder = Iso.SortingOrder(pos) - 4;
        }
        // 将网格赋值给MeshFilter
        meshFilter.mesh = mesh;

        // 遍历瓦片的标志位数组（5x5网格）
        if (Application.isPlaying)
        {
            int flagIndex = 0;
            for (int dx = -2; dx < 3; ++dx)
            {
                for (int dy = 2; dy > -3; --dy)
                {
                    // 如果遍历到的标志位结果为1（表示不可通过）(这个数据是在DT1文件中读取的)
                    //(tile.flags[flagIndex] & 1) 由于数据是二进制的,&是二进制和整型的对比,一致的话返回结果true也就是1
                    if ((tile.flags[flagIndex] & (1 + 8)) != 0)
                    {
                        // 计算子单元格位置并设置为不可通过
                        var subCellPos = Iso.MapToIso(pos) + new Vector3(dx, dy);
                        Tilemap.SetPassable(subCellPos, false);
                    }
                    //遍历一个格子就+1
                    ++flagIndex;
                }
            }
        }

        //给材质网格对象赋值材质
        meshRenderer.material = tile.material;
        //返回瓦片对象
        return gameObject;
    }
}
