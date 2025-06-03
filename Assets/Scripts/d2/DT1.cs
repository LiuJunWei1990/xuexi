using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
/// <summary>
/// 存储DT1文件的信息的类
/// </summary>
/// <remarks>
/// 主要功能: 
///   1. DT1数据缓存字典
///   2. DT1数据对应的ImportResult结构体,以及单个瓦片结构体
///   3. 读取DT1文件的方法
///   ; 读取数据赋值给ImportResult,存储在缓存字典里
/// </remarks>
public class DT1
{
    /// <summary>
    /// DT1的瓦片结构体,用来存储单个瓦片的信息
    /// </summary>
    public struct Tile
    {
        /// <summary>
        /// 方向?,没用上过
        /// </summary>
        public int direction;
        /// <summary>
        /// 屋顶高度偏移
        /// </summary>
        public short roofHeight;
        /// <summary>
        /// 声音索引?,没用上过
        /// </summary>
        public byte soundIndex;
        /// <summary>
        /// 是否动画?,没用上过
        /// </summary>
        public byte animated;
        /// <summary>
        /// 瓦片素材的高度
        /// </summary>
        public int height;
        /// <summary>
        /// 瓦片素材的宽度
        /// </summary>
        public int width;
        /// <summary>
        /// 朝向
        /// </summary>
        /// <remarks>
        /// 0和15就是正向,地板和房顶都是0或者15, 其他的物体,比如树,墙这类的都是在0-15之间; 
        /// 瓦片名字的第三个字
        /// </remarks>
        public int orientation;
        /// <summary>
        /// 主索引
        /// </summary>
        /// <remarks>
        /// 瓦片名字的第一个字
        /// </remarks>
        public int mainIndex;
        /// <summary>
        /// 子索引
        /// </summary>
        /// <remarks>
        /// 瓦片名字的第二个字
        /// </remarks>
        public int subIndex;
        /// <summary>
        /// 稀有度
        /// </summary>
        public int rarity;
        /// <summary>
        /// 瓦片碰撞,就是游戏本体代码中的单元格
        /// </summary>
        public byte[] flags;
        /// <summary>
        /// 块的头指针
        /// </summary>
        public int blockHeaderPointer;
        /// <summary>
        /// 块的数据的长度
        /// </summary>
        public int blockDatasLength;
        /// <summary>
        /// 块的数量
        /// </summary>
        public int blockCount;

        /// <summary>
        /// 原图材质
        /// </summary>
        public Material material;
        /// <summary>
        /// 原图纹理
        /// </summary>
        public Texture2D texture;
        /// <summary>
        /// 原图纹理的像素点矩阵
        /// </summary>
        public Color32[] texturePixels;
        /// <summary>
        /// 瓦片切片在原图纹理中的锚点的X
        /// </summary>
        public int textureX;
        /// <summary>
        /// 瓦片切片在原图纹理中的锚点的Y
        /// </summary>
        public int textureY;
        /// <summary>
        /// 索引
        /// </summary>
        /// <remarks>
        /// 做为在DS1文件中瓦片字典的Key; 主索引*64权重; 子索引*32权重; 朝向不加权重
        /// </remarks>
        public int index;
        /// <summary>
        /// 读取
        /// </summary>
        /// <param name="reader">二进制读取器</param>
        /// <remarks>
        /// 读取一个瓦片基础信息, 不包含块(就是图片的像素点矩阵)的数据
        /// </remarks>
        public void Read(BinaryReader reader)
        {
            //方向, 4字节
            direction = reader.ReadInt32();
            //屋顶高度偏移, 2字节
            roofHeight = reader.ReadInt16();
            //声音索引?, 1字节
            soundIndex = reader.ReadByte();
            //是否动画?, 1字节
            animated = reader.ReadByte();
            //高度, 4字节
            height = reader.ReadInt32();
            //宽度, 4字节
            width = reader.ReadInt32();
            //跳,4字节
            reader.ReadBytes(4); // zeros
            //素材的朝向, 地板和房顶都是0或者15, 其他的物体,比如树这类的都是在0-15之间, 4字节
            orientation = reader.ReadInt32();
            //主索引, 4字节
            mainIndex = reader.ReadInt32();
            //子索引, 4字节
            subIndex = reader.ReadInt32();
            //稀有度, 4字节
            rarity = reader.ReadInt32();
            //跳, 4个字节,这种时返回4个单字节数组; 前面是返回1个四字节的值
            reader.ReadBytes(4); // unknown
            //单元格?, 25个单字节
            flags = reader.ReadBytes(25); // Left to Right, and Bottom to Up
            //跳, 7个单字节
            reader.ReadBytes(7); // unused
            //块头指针, 4字节
            blockHeaderPointer = reader.ReadInt32();
            //块的数据的长度, 4字节
            blockDatasLength = reader.ReadInt32();
            //块的数量
            blockCount = reader.ReadInt32();
            //跳12个字节
            reader.ReadBytes(12); // zeros
            //计算索引
            index = Index(mainIndex, subIndex, orientation);
        }
        /// <summary>
        /// 算索引
        /// </summary>
        /// <param name="mainIndex">主索引</param>
        /// <param name="subIndex">子索引</param>
        /// <param name="orientation">朝向?目标?定位?</param>
        /// <returns>计算后的索引值</returns>
        /// <remarks>
        /// 主索引*64权重; 子索引*32权重; 朝向不加权重
        /// </remarks>
        static public int Index(int mainIndex, int subIndex, int orientation)
        {
            /// "<<"运算符是二进制运算,"<<"6相当于乘以64;"<<"5相当于乘以32
            return (((mainIndex << 6) + subIndex) << 5) + orientation;
        }
    }
    /// <summary>
    /// 导入结果
    /// </summary>
    /// <remarks>
    /// 包含所有瓦片和纹理
    /// </remarks>
    public struct ImportResult
    {
        /// <summary>
        /// 瓦片实例数组
        /// </summary>
        public Tile[] tiles;
        /// <summary>
        /// 原图数组
        /// </summary>
        public Texture2D[] textures;
    }
    /// <summary>
    /// 缓存
    /// </summary>
    /// <remarks>
    /// 缓存，避免重复导入,key为dt1文件路径,value为导入结果
    /// </remarks>
    static Dictionary<string, ImportResult> cache = new Dictionary<string, ImportResult>();
    /// <summary>
    /// 重置缓存
    /// </summary>
    static public void ResetCache()
    {
        cache.Clear();
    }
    /// <summary>
    /// 导入DT1文件
    /// </summary>
    /// <param name="dt1Path">文件路径</param>
    /// <returns>分割好的瓦片数组 和 用于分割的原图数组</returns>
    static public ImportResult Import(string dt1Path)
    {
        #region 准备阶段
        if (cache.ContainsKey(dt1Path))
        {
            return cache[dt1Path];
        }

        var importResult = new ImportResult();
        var bytes = File.ReadAllBytes(dt1Path);
        var stream = new MemoryStream(bytes);
        var reader = new BinaryReader(stream);
        #endregion

        #region 读取DT1文件头信息
        int version1 = reader.ReadInt32();
        int version2 = reader.ReadInt32();
        if (version1 != 7 || version2 != 6)
        {
            //版本1必须为7或者版本2必须为6
            Debug.Log(string.Format("无法读取dt1文件,错误版本({0}.{1})", version1, version2));
            return importResult;
        }
        //跳过260字节
        reader.ReadBytes(260);
        //到达了17行的第13个字节,16进制的15,10进制的21,这是瓦片种数
        int tileCount = reader.ReadInt32();
        reader.ReadInt32(); // 到此指针指向了文件中瓦片信息的开头（= 276）
        #endregion

        #region 声明瓦片和原图的临时变量
        //瓦片对象数组,tileCount多少种瓦片
        Tile[] tiles = new Tile[tileCount];

        //原图的尺寸
        const int textureSize = 2048;
        //储存原图的列表, 一个DT1文件可能会有多张图,所以用列表储存
        var textures = new List<Texture2D>();
        //原图像素矩阵列表, 和原图列表一一对应,储存的是像素矩阵
        var texturesPixels = new List<Color32[]>();
        //纹理包,用于计算切片的锚点,切片放满了会返回newTexture提示新建一张图
        var packer = new TexturePacker(textureSize, textureSize);
        //当前切片图片的材质
        Material material = null;
        //当前切片图片的纹理
        Texture2D texture = null;
        //当前切片图片的像素矩阵
        Color32[] pixels = null;
        #endregion

        #region 遍历瓦片数据
        //tileCount是头信息中获取的瓦片种数
        for (int i = 0; i < tileCount; ++i)
        {
            //导入文件中的参数
            tiles[i].Read(reader);
            //获取瓦片材质的锚点
            var result = packer.put(tiles[i].width, -tiles[i].height);


            if (result.newTexture)
            {
                #region 如果结果需要新建一张图片
                //新建一张图片(空的)
                texture = new Texture2D(textureSize, textureSize, TextureFormat.ARGB32, false);
                //设置纹理的过滤模式,本模式能让纹理缩放时更清晰
                texture.filterMode = FilterMode.Point;
                //把图片添加到图片数组中
                textures.Add(texture);
                //新建一个材质
                material = new Material(Shader.Find("Sprite"));
                //名字后面加上DT1文件的路径,名字没什么特殊作用,可能用来找或者DEBUG输出,暂时没发现哪里引用过
                material.name += "(" + dt1Path + ")";
                //把刚才新建的空图片赋值给材质
                material.mainTexture = texture;
                //新建一个像素矩阵(实际的图像,现在也是空的)
                pixels = new Color32[textureSize * textureSize];
                //像素矩阵添加到[图片的像素矩阵]数组中
                texturesPixels.Add(pixels);
                #endregion
            }

            //赋值切片信息
            tiles[i].textureX = result.x;  //锚点X
            tiles[i].textureY = result.y;  //锚点Y
            tiles[i].texture = texture;    //原图纹理
            tiles[i].material = material;  //原图材质
            tiles[i].texturePixels = pixels;  //原图像素矩阵

            //朝向是0或者15并且有高度的瓦片是地面或者屋顶
            if ((tiles[i].orientation == 0 || tiles[i].orientation == 15) && tiles[i].height != 0)
            {
                //判定是地面或者屋顶的瓦片,高度强制为-79
                tiles[i].height = -79;
            }
            else if (tiles[i].orientation > 0 && tiles[i].orientation < 15)
            {
                //非地板和屋顶的瓦片,锚点要往上移一个瓦片高度的距离
                tiles[i].textureY += (-tiles[i].height);
            }
        }
        //输出遍历的结果
        Debug.Log("导入DT1文件: " + dt1Path + ", 瓦片种类: " + tileCount + " 个, " + textures.Count + " 张原图");
        #endregion


        #region 在像素矩阵中打印图片
        //根据DT1文件中每种瓦片的信息,在像素矩阵的不同位置中打印出图片来
        //按瓦片种类遍历
        for (int i = 0; i < tileCount; ++i)
        {
            //取出来方便操作
            var tile = tiles[i];
            //如果瓦片的宽高为0,就跳过
            if (tile.width == 0 || tile.height == 0)
            {
                Debug.Log(string.Format("瓦片" + i + "尺寸数据为空 {0}x{1}", tile.width, tile.height));
                continue;
            }

            
            //定位到块头指针的位置,这段数据在前面导入的瓦片信息后面
            //以树的瓦片为例,瓦片信息结尾在2019,块头在2292
            stream.Seek(tile.blockHeaderPointer, SeekOrigin.Begin);
            for (int block = 0; block < tile.blockCount; ++block)
            {
                #region 定位到块头
                //像素矩阵起始位置的XY轴偏移
                int x = reader.ReadInt16();
                int y = reader.ReadInt16();
                reader.ReadBytes(2); // zeros
                reader.ReadByte(); // gridX
                reader.ReadByte(); // gridY
                //图片格式
                short format = reader.ReadInt16();
                //块数据的长度
                int length = reader.ReadInt32();
                reader.ReadBytes(2); // zeros
                //文件偏移量
                int fileOffset = reader.ReadInt32();
                //块数据的位置,块头指针+文件偏移量
                int blockDataPosition = tile.blockHeaderPointer + fileOffset;
                #endregion

                #region 打印
                if (format == 1)
                {
                    //绘制等距的块
                    drawBlockIsometric(tile.texturePixels, textureSize, tile.textureX + x, tile.textureY + y, bytes, blockDataPosition, length);
                }
                else
                {
                    //绘制默认的块
                    drawBlockNormal(tile.texturePixels, textureSize, tile.textureX + x, tile.textureY + y, bytes, blockDataPosition, length);
                }
                #endregion
            }
        }
        #endregion

        
        for (int i = 0; i < textures.Count; ++i)
        {
            //Color32[]导入到Texture2D中
            textures[i].SetPixels32(texturesPixels[i]);
            //应用一下,使其生效
            textures[i].Apply();
        }

        //导入到结果
        importResult.tiles = tiles;
        importResult.textures = textures.ToArray();
        //结果导入到缓存,避免重复导入
        cache[dt1Path] = importResult;
        return importResult;
    }
    /// <summary>
    /// 把DT1文件打印成PNG文件,方便查看
    /// </summary>
    /// <param name="assetPath"></param>
    static public void ConvertToPng(string assetPath)
    {
        Palette.LoadPalette(1);
        ImportResult result = Import(assetPath);
        int i = 0;
        foreach (var texture in result.textures)
        {
            var pngData = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            var pngPath = assetPath + "." + i + ".png";
            File.WriteAllBytes(pngPath, pngData);
            AssetDatabase.ImportAsset(pngPath);
            ++i;
        }
    }
    /// <summary>
    /// 把块的信息打印到瓦片对应的像素矩阵中
    /// </summary>
    /// <param name="texturePixels"></param>
    /// <param name="textureSize"></param>
    /// <param name="x0"></param>
    /// <param name="y0"></param>
    /// <param name="data"></param>
    /// <param name="ptr"></param>
    /// <param name="length"></param>
    /// <remarks>
    /// 找到块和像素矩阵的起始位,然后一个对一个像素打印; 
    /// 由于多个瓦片公用一个像素矩阵,所以很多时候是在同一个矩阵的不同位置打印
    /// </remarks>
    static void drawBlockNormal(Color32[] texturePixels, int textureSize, int x0, int y0, byte[] data, int ptr, int length)
    {
        //计算像素矩阵打印的起始位置
        int dst = texturePixels.Length - y0 * textureSize - textureSize + x0;
        //矩阵的打印头,代表当前在这个坐标导入单个像素的颜色
        int x = 0;
        int y = 0;
        //开始导入像素信息,length是像素信息的长度
        while (length > 0)
        {
            //Date是Dt1文件,这是第一步的字节数组,不是流
            //ptr: blockDataPosition,DT1像素信息的头
            //B1是X轴偏移,B2是块的像素数量
            byte b1 = data[ptr];
            byte b2 = data[ptr + 1];
            ptr += 2;
            length -= 2;
            if (b1 != 0 || b2 != 0)
            {
                x += b1;
                length -= b2;
                while (b2 != 0)
                {
                    //打印
                    texturePixels[dst + x] = Palette.palette[data[ptr]];
                    //两边的指针++
                    ptr++;
                    x++;
                    //块的像素数量--
                    b2--;
                }
            }
            //b1b2都是0代表这行像素都是空,跳过这行就行
            else
            {
                x = 0;
                y++;
                dst -= textureSize;
            }
        }
    }
    //drawBlockIsometric方法用的两个静态量
    static int[] xjump = { 14, 12, 10, 8, 6, 4, 2, 0, 2, 4, 6, 8, 10, 12, 14 };
    static int[] nbpix = { 4, 8, 12, 16, 20, 24, 28, 32, 28, 24, 20, 16, 12, 8, 4 };
    /// <summary>
    /// 把3D素材块的信息打印到瓦片对应的像素矩阵中
    /// </summary>
    /// <param name="texturePixels"></param>
    /// <param name="textureSize"></param>
    /// <param name="x0"></param>
    /// <param name="y0"></param>
    /// <param name="data"></param>
    /// <param name="ptr"></param>
    /// <param name="length"></param>
    /// <remarks>
    /// 房子这一类的3D素材,在DT1文件中是等距的,所以用的是这个方法
    /// </remarks>

    static void drawBlockIsometric(Color32[] texturePixels, int textureSize, int x0, int y0, byte[] data, int ptr, int length)
    {
        // 计算目标像素的起始位置
        int dst = texturePixels.Length - y0 * textureSize - textureSize + x0;
        // 声明变量：x 表示水平偏移，y 表示垂直偏移，n 表示当前行需要绘制的像素数量
        int x, y = 0, n;

        // 3d-isometric subtile is 256 bytes, no more, no less 
        // 断言：等距块的长度必须为 256 字节
        Debug.Assert(length == 256);
        if (length != 256)
            return;

        // 开始绘制像素数据
        while (length > 0)
        {
            // 根据 y 的值从 xjump 数组中获取水平偏移量
            x = xjump[y];
            // 根据 y 的值从 nbpix 数组中获取当前行需要绘制的像素数量
            n = nbpix[y];
            // 更新剩余需要处理的字节长度
            length -= n;
            // 绘制当前行的像素
            while (n != 0)
            {
                // 使用调色板中的颜色绘制像素
                texturePixels[dst + x] = Palette.palette[data[ptr]];
                // 移动数据指针
                ptr++;
                // 移动水平偏移量
                x++;
                // 减少当前行需要绘制的像素数量
                n--;
            }
            // 移动到下一行
            y++;
            // 更新目标像素的起始位置
            dst -= textureSize;
        }
    }
}
