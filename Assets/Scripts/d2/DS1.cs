using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
/// <summary>
/// DT1文件目录
/// </summary>
/// <remarks>
/// 用于存储DT1文件
/// 这里的瓦片和单元格 = 宏瓦片 = Unity世界坐标的1单位 = 等距坐标的5单位
/// </remarks>
class DT1Index
{
    /// <summary>
    /// DT1文件字典
    /// </summary>
    /// <remarks>
    /// 储存地图中所有的瓦片对象; 
    /// key为瓦片的索引，value为同类瓦片对象列表;
    /// </remarks>
    Dictionary<int, List<DT1.Tile>> tiles = new Dictionary<int, List<DT1.Tile>>();
    /// <summary>
    /// 稀有度映射
    /// </summary>
    /// <remarks>
    /// DT1文件的稀有度映射; 
    /// key和上面字典一样，value为这类瓦片的总稀有度; 
    /// 总体稀有度 = 同类瓦片数量 * 这个瓦片的稀有度;
    /// </remarks>
    new Dictionary<int, int> rarities = new Dictionary<int, int>();
    int dt1Count = 0;
    /// <summary>
    /// 导入Tile列表
    /// </summary>
    /// <param name="newTiles"></param>
    /// <remarks>
    /// 目前只有DT1文件导入时用一次
    /// 因为DT1文件是储存瓦片信息,所以每种瓦片只有一个
    /// </remarks>
    public void Add(DT1.Tile[] newTiles)
    {
        foreach (var tile in newTiles)
        {
            //如果tiles中没有这个key,则创建一个新的List,有就直接覆盖原来的List
            List<DT1.Tile> list = tiles.GetValueOrDefault(tile.index, null);
            if (list == null)
            {
                list = new List<DT1.Tile>();
                tiles[tile.index] = list;
            }

            //DT1Index的第一个DT1文件的瓦片对象排在最前面,其他的都排在最后面
            if (dt1Count == 0)
                list.Insert(0, tile);
            else
                list.Add(tile);

            //计算每个瓦片的rarity,并添加进稀有度映射
            //两个字典的key是一样
            //没key就新建一个稀有度,有key就加上这个瓦片的稀有度
            //总体稀有度 = 同类瓦片数量 * 这个瓦片的稀有度
            if (!rarities.ContainsKey(tile.index))
                rarities[tile.index] = tile.rarity;
            else
                rarities[tile.index] += tile.rarity;
        }
        //DT1文件数量加1
        dt1Count += 1;
    }
    
    /// <summary>
    /// 根据索引找到DT1文件字典中的对应瓦片
    /// </summary>
    /// <param name="index"></param>
    /// <param name="tile"></param>
    /// <returns></returns>
    /// <remarks>
    /// 索引是对应同类的瓦片,这个方法会在这类瓦片的列表种随机一个瓦片返回
    /// </remarks>
    public bool Find(int index, out DT1.Tile tile)
    {
        //临时列表,用来存要找的那类瓦片
        List<DT1.Tile> tileList;
        //有Key就赋值列表,没key就新建一个返回false
        if (!tiles.TryGetValue(index, out tileList))
        {
            tile = new DT1.Tile();
            return false;
        }

        //计算总体稀有度,如果总体稀有度为0,就返回第一个瓦片,否则随机一个不为零的返回
        int raritySum = rarities[index];
        if (raritySum == 0)
        {
            tile = tileList[0];
        }
        else
        {
            int randomIndex = Random.Range(0, tileList.Count - 1);
            while (tileList[randomIndex].rarity == 0)
            {
                randomIndex = (randomIndex + 1) % tileList.Count;
            }
            tile = tileList[randomIndex];
        }

        return true;
    }
}

public class DS1
{
    /// <summary>
    /// [DS1类]的单元格结构体
    /// </summary>
    /// <remarks>
    /// 这个类中的单元格和瓦片指的是瓦片素材,不是游戏中的瓦片和单元格,对应的是宏瓦片; 
    /// </remarks>
    struct Cell
    {
        /// <summary>
        /// 这四个属性是用来计算索引的,每种瓦片的索引都是单独的
        /// </summary>
        public byte prop1;
        /// <summary>
        /// 这四个属性是用来计算索引的,每种瓦片的索引都是单独的
        /// </summary>
        public byte prop2;
        /// <summary>
        /// 这四个属性是用来计算索引的,每种瓦片的索引都是单独的
        /// </summary>
        public byte prop3;
        /// <summary>
        /// 这四个属性是用来计算索引的,每种瓦片的索引都是单独的
        /// </summary>
        public byte prop4;
    };
    /// <summary>
    /// 导入地图后返回的结果
    /// </summary>
    /// <remarks>
    /// 导入的地图的原点和入口
    /// </remarks>
    public struct ImportResult
    {
        /// <summary>
        /// 原点
        /// </summary>
        public Vector3 center;
        /// <summary>
        /// 入口
        /// </summary>
        public Vector3 entry;
    }

    /// <summary>
    /// 朝向转换表
    /// </summary>
    /// <remarks>
    /// 版本7以下的朝向用的数据是这个数组的下标,7以上的就不需要转换了
    /// </remarks>
    static byte[] dirLookup = {
                  0x00, 0x01, 0x02, 0x01, 0x02, 0x03, 0x03, 0x05, 0x05, 0x06,
                  0x06, 0x07, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E,
                  0x0F, 0x10, 0x11, 0x12, 0x14
               };

    //特殊点位的索引
    
    /// <summary>
    /// 地图入口
    /// </summary>
    static readonly int mapEntryIndex = DT1.Tile.Index(30, 11, 10);
    /// <summary>
    /// 城镇入口1
    /// </summary>
    static readonly int townEntryIndex = DT1.Tile.Index(30, 0, 10);
    /// <summary>
    /// 城镇入口2
    /// </summary>
    static readonly int townEntry2Index = DT1.Tile.Index(31, 0, 10);
    /// <summary>
    /// 尸体位置索引(未实装)
    /// </summary>
    static readonly int corpseLocationIndex = DT1.Tile.Index(32, 0, 10);
    /// <summary>
    /// 传送门位置索引(未实装)
    /// </summary>
    static readonly int portalLocationIndex = DT1.Tile.Index(33, 0, 10);
    /// <summary>
    /// 导入DS1文件
    /// </summary>
    /// <param name="ds1Path">文件路径</param>
    /// <param name="monsterPrefab">需要布置的怪物的预制体</param>
    /// <returns>导入结果</returns>
    static public ImportResult Import(string ds1Path, GameObject monsterPrefab = null)
    {
        //开始记录导入时间
        var sw = System.Diagnostics.Stopwatch.StartNew();
        //把DS1文件导入成内存流
        var stream = new MemoryStream(File.ReadAllBytes(ds1Path));
        //把内存流转换为二进制流
        var reader = new BinaryReader(stream);
        //读取DS1文件的版本号(4个字节)
        int version = reader.ReadInt32();
        //地图长度(瓦片对象数量)
        int width = reader.ReadInt32() + 1; //读取的数值加1,因为0也算了一个
        //地图宽度(瓦片对象数量)
        int height = reader.ReadInt32() + 1;
        //幕数
        int act = 1;
        
        #region 版本8及以上读幕数,否则默认幕数1
        if (version >= 8)
        {
            //读取幕数
            act = reader.ReadInt32() + 1; //第0幕不算,从1开始
            act = Mathf.Min(act, 5);      //最多5幕
        }
        #endregion
        
        //读取对应幕的调色板
        Palette.LoadPalette(act);
        //标签类型? 貌似没有用过
        int tagType = 0;

        #region 版本10及以上读取标签类型,否则默认为0
        if (version >= 10)
        {
            tagType = reader.ReadInt32();

            //// adjust eventually the # of tag layer
            //if ((tagType == 1) || (tagType == 2))
            //    t_num = 1;
        }
        #endregion


        var dt1Index = new DT1Index();

        //总瓦片数
        int totalTiles = 0;

       #region 版本3及以上读取DT1文件,并储存
        if (version >= 3)
        {
            //文件数量
            int fileCount = reader.ReadInt32();

            for (int i = 0; i < fileCount; i++)
            {
                string filename = "";
                char c;
                //读取文件路径
                while ((c = reader.ReadChar()) != 0)
                {
                    filename += c;
                }
                //把文件路径中的.tg1替换成.dt1
                filename = filename.Replace(".tg1", ".dt1");
                //导入
                var imported = DT1.Import("Assets" + filename);
                //总瓦片数,增量
                totalTiles += imported.tiles.Length;
                //储存到DT1文件目录,这导入的是一个DT1文件,每种瓦片只有一个
                dt1Index.Add(imported.tiles);
            }
        }
        #endregion

        //暂停记录时间
        sw.Stop();
        //输出日志
        Debug.Log("DT1 载入耗时: " + sw.Elapsed + " (总瓦皮数: " + totalTiles + " )");
        //重置计时器
        sw.Reset();
        //开始记录时间
        sw.Start();

        #region 版本9-13(包含)跳过两个字节
        // skip 2 dwords ?
        if ((version >= 9) && (version <= 13))
            reader.ReadBytes(2);
        #endregion

        //设定各层级数量默认值
        int wallLayerCount = 1;         // 墙壁层级数量
        int floorLayerCount = 1;        // 地板层级数量
        int shadowLayerCount = 1;       // 阴影层级数量
        int tagLayerCount = 0;          // 标签层级数量

        #region 版本4及以上读取各层级数量,否则就用上面的默认值
        if (version >= 4)
        {
            wallLayerCount = reader.ReadInt32();

            //版本16及以上读取地板层级数量
            if (version >= 16)
            {
                floorLayerCount = reader.ReadInt32();
            }
        }
        //4以下版本读取标签层级数量
        else
        {
            tagLayerCount = 1;
        }
        #endregion

        //输出日志
        Debug.Log("瓦片层级 : 墙壁: (2 * " + wallLayerCount + " ) ; 地板: " + floorLayerCount + " ; 阴影: " + shadowLayerCount + "; 标签: " + tagLayerCount);

        #region 新建墙壁瓦片数组,用于导入墙壁是存储,导入朝向时使用
        Cell[][] walls = new Cell[wallLayerCount][];
        for (int i = 0; i < wallLayerCount; ++i)
            //这里是整张地图的宽高,估计是排列瓦片的
            walls[i] = new Cell[width * height];

        #endregion


        #region 新建层级数组,用来遍历层级用的,并创建根节点和层级子节点

        #region 层级说明
        // 层级说明
        //  layout[0] = 1; // wall 1
        //  layout[1] = 9; // floor 1
        //  layout[2] = 5; // orientation 1
        //  layout[3] = 12; // tag
        //  layout[4] = 11; // shadow
        //这些层级是有多层的,比如layout[0]是w1, layout[1]是w2
        //wallLayerCount, floorLayerCount, shadowLayerCount, tagLayerCount只有这4个会创建父节点
        //orientation层就是wall层的朝向,它在游戏中并不表现出来
        //当前数据游戏中的实际节点就是f1 f2 w1 w2,它是wall层和floor层的父节点
        #endregion

        //层级数量
        int layerCount = 0;
        //层级数组
        int[] layout = new int[14];
        //版本小于4的层级直接赋值
        if (version < 4)
        {
            layout[0] = 1; // wall 1
            layout[1] = 9; // floor 1
            layout[2] = 5; // orientation 1
            layout[3] = 12; // tag
            layout[4] = 11; // shadow
            layerCount = 5;
        }
        //版本大于等于4的层级,由于一个种类有几个层级,比如墙壁,起始层数依然是1 9 5 12 11
        else
        {
            layerCount = 0;
            //墙壁和朝向都按墙壁层数算
            for (int x = 0; x < wallLayerCount; x++)
            {
                layout[layerCount++] = 1 + x; // wall x
                layout[layerCount++] = 5 + x; // orientation x
            }
            //地板,阴影,标签都按地板层数算
            for (int x = 0; x < floorLayerCount; x++)
                layout[layerCount++] = 9 + x; // floor x
            if (shadowLayerCount != 0)
                layout[layerCount++] = 11;    // shadow
            if (tagLayerCount != 0)
                layout[layerCount++] = 12;    // tag
        }

        //创建根节点,名字是DS1文件名
        GameObject root = new GameObject(Path.GetFileName(ds1Path));

        //地板节点数组,每个节点的父节点是根
        var floorLayers = new GameObject[floorLayerCount];
        for (int i = 0; i < floorLayerCount; ++i)
        {
            floorLayers[i] = new GameObject("f" + (i + 1));
            floorLayers[i].transform.SetParent(root.transform);
        }

        //墙壁节点数组,每个节点的父节点是根
        var wallLayers = new GameObject[wallLayerCount];
        for (int i = 0; i < wallLayerCount; ++i)
        {
            wallLayers[i] = new GameObject("w" + (i + 1));
            wallLayers[i].transform.SetParent(root.transform);
        }

        #endregion

        #region 创建地图,给所有瓦片赋值并创建
        //新建导入结果类,准备开始导入数据
        var importResult = new ImportResult();
        //设置原点
        importResult.center = MapToWorld(width, height) / 2;
        //设置入口,默认是原点
        importResult.entry = importResult.center;

        //遍历所有的层级
        for (int n = 0; n < layerCount; n++)
        {
            //临时变量,用处储存当前遍历到的层级,做为瓦片数组的下标用
            int p;
            //临时变量,代表当前层级的第几个瓦片,做为瓦片数组的下标用
            int i = 0;
            //每个层级都要遍历一遍的全地图瓦片点位(一个瓦片对象一个点)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //层级不为空就开始读取,层级空就不做操作++i
                    switch (layout[n])
                    {
                        #region 墙壁层

                        // 墙 1-4层级
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                            {
                                p = layout[n] - 1;
                                walls[p][i].prop1 = reader.ReadByte();
                                walls[p][i].prop2 = reader.ReadByte();
                                walls[p][i].prop3 = reader.ReadByte();
                                walls[p][i].prop4 = reader.ReadByte();
                                break;
                            }

                        // 朝向 5-8层级
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                            {
                                p = layout[n] - 5;
                                int orientation = reader.ReadByte();
                                // 版本小于7的朝向要做一个转换成16进制
                                if (version < 7)
                                    orientation = dirLookup[orientation];

                                reader.ReadBytes(3);

                                //属性1为0直接跳过这个瓦片
                                if (walls[p][i].prop1 == 0)
                                    break;

                                //属性234用来算索引
                                int prop2 = walls[p][i].prop2;
                                int prop3 = walls[p][i].prop3;
                                int prop4 = walls[p][i].prop4;

                                //计算索引
                                int mainIndex = (prop3 >> 4) + ((prop4 & 0x03) << 4);
                                int subIndex = prop2;
                                int index = DT1.Tile.Index(mainIndex, subIndex, orientation);

                                //遍历到地图入口和城镇入口,存在结果中,并输出日志
                                if (index == mapEntryIndex)
                                {
                                    importResult.entry = MapToWorld(x, y);
                                    Debug.Log("在 " + x + " " + y + " 找到地图入口");
                                    break;
                                }
                                else if (index == townEntryIndex)
                                {
                                    importResult.entry = MapToWorld(x, y);
                                    Debug.Log("在 " + x + " " + y + " 找到小镇入口");
                                    break;
                                }
                                //下面这些暂不做处理
                                else if (index == townEntry2Index)
                                {
                                    break;
                                }
                                else if (index == corpseLocationIndex)
                                {
                                    break;
                                }
                                else if (index == portalLocationIndex)
                                {
                                    break;
                                }


                                DT1.Tile tile;
                                //随机取一个索引种类的瓦片,并创建瓦片对象,设置父节点
                                if (dt1Index.Find(index, out tile))
                                {
                                    var tileObject = CreateTile(tile, x, y);
                                    tileObject.transform.SetParent(wallLayers[p].transform);
                                }
                                else
                                {

                                    Debug.LogWarning("未找到墙壁瓦片 (索引: " + mainIndex + " " + subIndex + " " + orientation + ") 点位: " + x + ", " + y);
                                }

                                if (orientation == 3)
                                {
                                    // 朝向3的瓦片在计算索引时朝向值要改成4,4只用于索引计算,朝向3是左上角的墙壁
                                    index = DT1.Tile.Index(mainIndex, subIndex, 4);
                                    //后面都是一样的
                                    if (dt1Index.Find(index, out tile))
                                    {
                                        var tileObject = CreateTile(tile, x, y);
                                        tileObject.transform.SetParent(wallLayers[p].transform);
                                    }
                                    else
                                    {
                                        Debug.LogWarning("未找到墙壁瓦片 (索引: " + mainIndex + " " + subIndex + " " + orientation + ") 点位: " + x + ", " + y);
                                    }
                                }

                                break;
                            }
                        #endregion

                        #region 地板层
                        // floors
                        case 9:
                        case 10:
                            {
                                p = layout[n] - 9;
                                int prop1 = reader.ReadByte();
                                int prop2 = reader.ReadByte();
                                int prop3 = reader.ReadByte();
                                int prop4 = reader.ReadByte();

                                if (prop1 == 0) // 属性1为0,代表没有瓦片数据
                                    break;

                                //计算索引
                                int mainIndex = (prop3 >> 4) + ((prop4 & 0x03) << 4);
                                int subIndex = prop2;
                                //地板朝向只有0
                                int orientation = 0;
                                int index = DT1.Tile.Index(mainIndex, subIndex, orientation);

                                //创建瓦片对象
                                DT1.Tile tile;
                                if (dt1Index.Find(index, out tile))
                                {
                                    //地板有层内排序,按父节点的数字排
                                    var tileObject = CreateTile(tile, x, y, orderInLayer: p);
                                    tileObject.transform.SetParent(floorLayers[p].transform);
                                }
                                break;
                            }
                        #endregion

                        #region 阴影层
                        // 阴影跳过
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
                            //    s_ptr[p] += s_num;
                            //}
                            //else
                            //    bptr += 4;
                            break;
                        #endregion

                        #region 标签层
                        // 标签跳过
                        case 12:
                            reader.ReadBytes(4);
                            //if ((x < new_width) && (y < new_height))
                            //{
                            //    p = layout[n] - 12;
                            //    t_ptr[p]->num = (UDWORD) * ((UDWORD*)bptr);
                            //    t_ptr[p] += t_num;
                            //}
                            //bptr += 4;
                            break;
                        #endregion
                    }
                    ++i;
                }
            }
        }
        #endregion

        #region 版本2以上读取怪物数据并创建怪物游戏对象
        if (version >= 2)
        {
            int objectCount = reader.ReadInt32();
            Debug.Log("对象总数: " + objectCount);

            for (int n = 0; n < objectCount; n++)
            {
                int type = reader.ReadInt32();
                int id = reader.ReadInt32();
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();

                if (version > 5)
                {
                    int flags = reader.ReadInt32();
                }

                //游戏本体代码那种小瓦片转世界坐标
                var pos = MapSubCellToWorld(x, y);
                //在容器中找到对应的对象参数
                Obj obj = Obj.Find(act, type, id);
                var gameObject = CreateObject(obj, pos);
                gameObject.transform.SetParent(root.transform);
            }
        }
        #endregion

        sw.Stop();
        Debug.Log("DS1文件加载完成,耗时: " + sw.Elapsed);

        return importResult;
    }

    /// <summary>
    /// 瓦片点位转世界坐标
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    static Vector3 MapToWorld(int x, int y)
    {
        var pos = Iso.MapToWorld(new Vector3(x, y)) / Iso.tileSize;
        pos.y = -pos.y;
        return pos;
    }

    /// <summary>
    /// 游戏本体代码瓦片转世界坐标
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    static Vector3 MapSubCellToWorld(int x, int y)
    {
        var pos = Iso.MapToWorld(new Vector3(x - 2, y - 2));
        pos.y = -pos.y;
        return pos;
    }
    /// <summary>
    /// 创建瓦片实体
    /// </summary>
    /// <param name="tile">瓦片</param>
    /// <param name="x">瓦片点位X(一个瓦片对象一个点)</param>
    /// <param name="y">瓦片点位Y(一个瓦片对象一个点)</param>
    /// <param name="orderInLayer">图层内顺序</param>
    /// <returns>生成的瓦片对象</returns>
    /// <remarks>
    /// 创建瓦片, 并设置位置, 材质, UV, 排序, 阻挡
    /// </remarks>
    static GameObject CreateTile(DT1.Tile tile, int x, int y, int orderInLayer = 0)
    {
        // 获取瓦片的纹理
        var texture = tile.texture;
        // 将地图坐标转换为世界坐标
        var pos = MapToWorld(x, y);
    
        // 创建新的游戏对象
        GameObject gameObject = new GameObject();
        // 设置对象名称为"主索引_子索引_方向"
        gameObject.name = tile.mainIndex + "_" + tile.subIndex + "_" + tile.orientation;
        // 设置对象位置
        gameObject.transform.position = pos;
        
        // 添加MeshRenderer和MeshFilter组件
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        var meshFilter = gameObject.AddComponent<MeshFilter>();
        
        // 创建新的Mesh
        Mesh mesh = new Mesh();
        
        // 获取纹理坐标和尺寸
        float x0 = tile.textureX;
        float y0 = tile.textureY;
        float w = tile.width / Iso.pixelsPerUnit;
        float h = (-tile.height) / Iso.pixelsPerUnit;
    
        // 根据瓦片方向设置不同的顶点和UV
        if (tile.orientation == 0 || tile.orientation == 15)
        {
            // 处理地板或屋顶瓦片
            var topLeft = new Vector3(-1f, 0.5f);
            if (tile.orientation == 15)
                topLeft.y += tile.roofHeight / Iso.pixelsPerUnit;
        
            // 设置顶点
            mesh.vertices = new Vector3[] {
                topLeft,
                topLeft + new Vector3(0, -h),
                topLeft + new Vector3(w, -h),
                topLeft + new Vector3(w, 0)
            };
            
            // 设置三角形
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
        else
        {
            // 处理其他方向的瓦片（如墙壁）
            var topLeft = new Vector3(-1f, h - 0.5f);
            // 设置顶点
            mesh.vertices = new Vector3[] {
                topLeft,
                topLeft + new Vector3(0, -h),
                topLeft + new Vector3(w, -h),
                topLeft + new Vector3(w, 0)
            };
            // 设置三角形
            mesh.triangles = new int[] { 2, 1, 0, 3, 2, 0 };

            // 设置UV坐标
            mesh.uv = new Vector2[] {
                new Vector2 (x0 / texture.width, (-y0 - tile.height) / texture.height),
                new Vector2 (x0 / texture.width, -y0 / texture.height),
                new Vector2 ((x0 + tile.width) / texture.width, -y0 / texture.height),
                new Vector2 ((x0 + tile.width) / texture.width, (-y0 - tile.height) / texture.height)
            };

            // 设置渲染顺序,按Iso类的排序方法排
            meshRenderer.sortingOrder = Iso.SortingOrder(pos) - 4;
        }
    
        // 将Mesh赋值给MeshFilter
        meshFilter.mesh = mesh;
    
        // 在游戏运行时处理瓦片的碰撞标志(flag = 游戏本体代码中的单元格)
        if (Application.isPlaying)
        {
            //flag指针
            int flagIndex = 0;
            //遍历瓦片内的flag(5*5)
            for (int dx = -2; dx < 3; ++dx)
            {
                for (int dy = 2; dy > -3; --dy)
                {
                    //这是二进制运算,判断flag是否包括(1和8),有任意一个代表不可通行
                    if ((tile.flags[flagIndex] & (1 + 8)) != 0)
                    {
                        //获取flag等距坐标
                        var subCellPos = Iso.MapToIso(pos) + new Vector3(dx, dy);
                        //设置不可通行
                        Tilemap.SetPassable(subCellPos, false);
                    }
                    ++flagIndex;
                }
            }
        }
    
        // 设置材质
        meshRenderer.material = tile.material;
        //返回瓦片对象
        return gameObject;
    }

    static GameObject CreateObject(Obj obj, Vector3 pos)
    {
        GameObject gameObject = new GameObject();
        gameObject.transform.position = pos;
        gameObject.name = obj.description;

        //如果obj的_base为空,就是对象节点,直接返回
        if (obj._base == null)
            return gameObject;

        var animator = gameObject.AddComponent<COFAnimator>();
        
        if (obj.type == 2)
        {
            ObjectInfo objectInfo = ObjectInfo.sheet.rows[obj.objectId];
            gameObject.name += " " + objectInfo.description;

            var staticObject = gameObject.AddComponent<StaticObject>();
            staticObject.obj = obj;
            staticObject.objectInfo = objectInfo;
            staticObject.direction = obj.direction;
        }
        else
        {
            var cof = COF.Load(obj, obj.mode);
            animator.SetCof(cof);
            animator.direction = obj.direction;
        }

        return gameObject;
    }
}
