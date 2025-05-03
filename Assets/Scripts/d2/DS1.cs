using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
/// <summary>
/// DS1文件读取器
/// </summary>
/// <remarks>
/// 读取
/// </remarks>
public class DS1
{
    /// <summary>
    /// 单元格,地图单元数据结构（每个格子10字节）
    /// </summary>
    struct Cell
    {
        public byte prop1;     // 属性1（通常为材质索引）
        public byte prop2;     // 属性2（子材质索引/动画帧数）
        public byte prop3;     // 属性3（高位4bit为主材质索引）
        public byte prop4;     // 属性4（低2bit补充主材质索引）
        public byte orientation; // 方向（0-7代表8个方向）
        public int bt_idx;       // 区块类型索引
        public byte flags;       // 标志位（是否可通行等）
    };
    /// <summary>
    /// 导入一个ds1文件
    /// </summary>
    /// <param name="ds1Path">ds1文件路径</param>
	static public void Import(string ds1Path)
    {
        //System.Diagnostics.Stopwatch时.NET自带的计时器,可以用来测量代码执行时间
        //StartNew()方法用于创建一个新的计时器实例并开始计时
        var sw = System.Diagnostics.Stopwatch.StartNew();

        //>>>>>>>>生成文件流
        //File.OpenRead(ds1Path)打开一个文件进行读取,返回一个FileStream对象
        //BufferedStream是用于缓冲读取,提高读取效率,适用于频繁的小数据读取
        var stream = new BufferedStream(File.OpenRead(ds1Path));
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
            //    t_num = 1;
        }

        //[[[版本号 >= 3]]]
        //新建一个瓦片数组,用来存储地图的瓦片数据(不再版本分支中,任何版本都要新建这个数组)
        DT1.Tile[] tiles = new DT1.Tile[4096];
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
                //读取dt1文件的方法,返回瓦片数组(DT1文件的瓦片结构体)
                //并遍历它
                foreach (var tile in DT1.Import("Assets" + filename))
                {
                    //如果瓦片信息中的材质为空,就跳过这个瓦片,继续下一个
                    if (tile.texture == null) continue;
                    //如果没跳出,那就加入到之前新建的瓦片数组中去
                    tiles[tile.index] = tile;
                }
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
        int w_num = 1; // 墙壁和方向图层数量
        int f_num = 1; // 地板图层数量
        int s_num = 1; // 阴影图层数量
        int t_num = 0; // 标签图层数量

        //[[[版本号 >= 4]]]
        if (version >= 4)
        {
            //读取墙壁和方向图层数量
            w_num = reader.ReadInt32();
            //同时如果[[[版本号 >= 16]]]
            if (version >= 16)
            {
                //读取地板图层数量
                f_num = reader.ReadInt32();
            }
        }
        //否则
        else
        {
            //标签图层数量设为1
            t_num = 1;
        }
        //输出图层数量:(2 * w_num 墙壁) + f_num 地板 + s_num 阴影 + t_num 标签
        Debug.Log("图层数量分别为 : (2 * " + w_num + " 墙壁) + " + f_num + " 地板 + " + s_num + " 阴影 + " + t_num + " 标签");
        //新建一个单元格数组,用来存储墙壁数据,Cell[第几图层][第几块墙壁]
        Cell[][] walls = new Cell[w_num][];
        //遍历地板图层数量,给墙壁的每一个图层添加全地图大小的瓦片数组
        for (int i = 0; i < f_num; ++i) walls[i] = new Cell[width * height];
        //新建一个单元格数组,用来存储地板数据,Cell[第几图层][第几块地板]
        Cell[][] floors = new Cell[f_num][];
        //遍历地板图层数量,给地板的每一个图层添加全地图大小的瓦片数组
        for (int i = 0; i < f_num; ++i) floors[i] = new Cell[width * height];
        //新建图层总数变量
        int layerCount = 0;
        //新建一个布局数组,用来存储图层顺序
        int[] layout = new int[14];
        //如果版本号小于4
        if (version < 4)
        {

            layout[0] = 1; // 墙 1
            layout[1] = 9; // 地板 1
            layout[2] = 5; // 朝向 1
            layout[3] = 12; // 标签
            layout[4] = 11; // 阴影
            layerCount = 5; // 总图层数
        }
        //否则
        else
        {
            //总图层数量为0
            layerCount = 0;
            //遍历墙壁和方向图层数量
            for (int x = 0; x < w_num; x++)
            {
                //每次遍历:把值添加到数组里面,墙从1开始,朝向从5开始
                layout[layerCount++] = 1 + x; // wall x
                layout[layerCount++] = 5 + x; // orientation x
            }
            //遍历地板的图层数量
            for (int x = 0; x < f_num; x++)
            {
                //地板图层从9开始
                layout[layerCount++] = 9 + x; // floor x
            }
            //阴影和标签加入到数组中,分别时11和12
            if (s_num != 0) layout[layerCount++] = 11;    // shadow
            if (t_num != 0) layout[layerCount++] = 12;    // tag
        }
        //创建一个空对象做为父,命名为tristram,就是大名鼎鼎的崔斯特瑞姆,这应该就是第一幕地图的原点了
        GameObject parent = new GameObject("tristram");
        //遍历整个图层总数
        for (int n = 0; n < layerCount; n++)
        {
            int p;
            int i = 0;
            //遍历地图的每一格
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //根据当前当前所在的图层,进行不同处理
                    switch (layout[n])
                    {
                        //1-4是墙图层,跳过4个字节(读取了没赋值)
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                            reader.ReadBytes(4);
                            //p = layout[n] - 1;
                            //walls[p][i].prop1 = reader.ReadByte();
                            //walls[p][i].prop2 = reader.ReadByte();
                            //walls[p][i].prop3 = reader.ReadByte();
                            //walls[p][i].prop4 = reader.ReadByte();
                            break;
                        //5-8是朝向图层,p=n-5,应该就是朝向的第几层,从0-3,读取一个字节做o,再跳过三个字节(读取了没赋值)
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                            p = layout[n] - 5;
                            byte o = reader.ReadByte();
                            //if (version < 7)
                            //    walls[p][i].orientation = dir_lookup[o];
                            //else
                            //    walls[p][i].orientation = o;

                            reader.ReadBytes(3);
                            break;
                        //9-10是地板层,p=n-9,应该就是地板的第几层,从0-1,读取4个字节,分别是prop1,prop2,prop3,prop4,四个属性
                        case 9:
                        case 10:
                            p = layout[n] - 9;
                            int prop1 = reader.ReadByte();   // 属性1（通常为材质索引）
                            int prop2 = reader.ReadByte();   // 属性2（子材质索引/动画帧数）
                            int prop3 = reader.ReadByte();   // 属性3（高位4bit为主材质索引）
                            int prop4 = reader.ReadByte();   // 属性4（低2bit补充主材质索引）
                            //>>>>下面这段看不懂了,需要了解暗黑2MOD的原理才能看懂
                            //注释一下大概意思
                            //计算主材质索引
                            int mainIndex = (prop3 >> 4) + ((prop4 & 0x03) << 4);
                            //子材质索引直接使用prop2
                            int subIndex = prop2;
                            //计算最终的材质索引
                            int index = (mainIndex << 6) + subIndex;
                            //根据索引,从tiles数组中获取对应的瓦片
                            DT1.Tile tile = tiles[index];
                            //如果瓦片材质不为空,就创建一个地板对象,并设置它的位置,父对象,和顺序
                            if (tile.texture != null)
                            {
                                //创建一个地板对象
                                var tileObject = CreateFloor(tile, orderInLayer: -p);
                                //设置它的位置
                                var pos = Iso.MapToWorld(new Vector3(x, y)) * 5;
                                pos.y = -pos.y;
                                tileObject.transform.position = pos;
                                //设置它的父对象
                                tileObject.transform.SetParent(parent.transform);
                                break;
                            }
                            //上面的if不成功就代表这个瓦片材质是空的,那就跳出switch继续循环
                            break;

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
                            //    s_ptr[p] += s_num;
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
                            //    t_ptr[p] += t_num;
                            //}
                            //bptr += 4;
                            break;
                    }
                }
            }
            //这个i在整个循环中都没用上,暂时没有作用
            ++i;
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
            }
        }
        //关闭流
        stream.Close();
        //中止计时器,并debug输出DS1文件的加载时间
        sw.Stop();
        Debug.Log("DT1 加载 用 " + sw.Elapsed + "时间");
    }
    /// <summary>
    /// 创建一个地板对象
    /// </summary>
    /// <param name="tile">DT1文件中提取的地板对象</param>
    /// <param name="orderInLayer">同图层的堆叠排序</param>
    /// <returns>返回地板的gameObject</returns>
    static GameObject CreateFloor(DT1.Tile tile, int orderInLayer)
    {
        //获取瓦片材质
        var texture = tile.texture;
        //新建一个游戏对象
        GameObject gameObject = new GameObject();
        //给游戏对象命名:floor_ + 材质主索引 + _ + 材质子索引,例子:floor
        gameObject.name = "floor_" + tile.mainIndex + "_" + tile.subIndex;
        //给游戏对象添加一个MeshRenderer组件和一个MeshFilter组件并引用
        var meshRenderer = gameObject.AddComponent<MeshRenderer>();
        var meshFilter = gameObject.AddComponent<MeshFilter>();
        //新建一个材质网格对象
        Mesh mesh = new Mesh();
        //给网格取个名字
        mesh.name = "generated floor mesh";
        //赋值网格顶点坐标
        mesh.vertices = new Vector3[] { new Vector3(-1, 0.5f), new Vector3(-1, -0.5f), new Vector3(1, -0.5f), new Vector3(1, 0.5f) };
        //赋值网格的三角形(这个赋值应该是和三角形有关,但是不知道具体代表什么)
        mesh.triangles = new int[] { 2, 1, 0, 3, 2, 0 };
        //赋值网格的UV坐标
        float x0 = tile.textureX;
        float y0 = tile.textureY;
        mesh.uv = new Vector2[] {
                  new Vector2 (x0 / texture.width, -y0 / texture.height),
                  new Vector2 (x0 / texture.width, (-y0 -80f) / texture.height),
                  new Vector2 ((x0 + 160f) / texture.width, (-y0 -80f) / texture.height),
                  new Vector2 ((x0 + 160f) / texture.width, -y0 / texture.height)
        };
        //赋值网格的法线
        meshFilter.mesh = mesh;
        //给材质网格对象赋值材质
        meshRenderer.material = tile.material;
        //设置材质网格对象的排序层和顺序
        meshRenderer.sortingLayerName = "Floor";
        //设置材质网格对象的排序顺序
        meshRenderer.sortingOrder = orderInLayer;
        //返回瓦片对象
        return gameObject;
    }
}
