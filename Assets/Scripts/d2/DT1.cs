using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
/// <summary>
/// DT1文件读取器
/// </summary>
public class DT1
{
    /// <summary>
    /// 瓦片结构体
    /// </summary>
    public struct Tile
    {
        public int direction;  // 瓦片的方向
        public short roofHeight;  // 屋顶高度
        public byte soundIndex;  // 声音索引
        public byte animated;  // 是否动画
        public int height;  // 瓦片高度
        public int width;  // 瓦片宽度
        public int orientation;  // 瓦片朝向
        public int mainIndex;  // 主索引
        public int subIndex;  // 子索引
        public int rarity;  // 稀有度
        public byte[] flags;  // 标志位数组,就是瓦片里面的5*5单元格
        public int blockHeaderPointer;  // 块头指针
        public int blockDatasLength;  // 块数据长度
        public int blockCount;  // 块数量

        public Material material;  // 材质
        public Texture2D texture;  // 纹理
        public int textureX;  // 纹理X坐标
        public int textureY;  // 纹理Y坐标
        /// <summary>
        /// 瓦片的种类的索引
        /// </summary>
        public int index; 
        /// <summary>
        /// 读取单个瓦片数据
        /// </summary>
        /// <param name="reader"></param>
        public void Read(BinaryReader reader)
        {
            direction = reader.ReadInt32();  // 读取方向
            roofHeight = reader.ReadInt16();  // 读取屋顶高度
            soundIndex = reader.ReadByte();  // 读取声音索引
            animated = reader.ReadByte();  // 读取是否动画
            height = reader.ReadInt32();  // 读取高度
            width = reader.ReadInt32();  // 读取宽度
            reader.ReadBytes(4); // 跳过4字节
            orientation = reader.ReadInt32();  // 读取朝向
            mainIndex = reader.ReadInt32();  // 读取主索引
            subIndex = reader.ReadInt32();  // 读取子索引
            rarity = reader.ReadInt32();  // 读取稀有度
            reader.ReadBytes(4); // 跳过4字节
            flags = reader.ReadBytes(25); // 读取25字节的标志位,就是瓦片内5*5单元格的信息
            reader.ReadBytes(7); // 跳过7字节的
            blockHeaderPointer = reader.ReadInt32();  // 读取块头指针
            blockDatasLength = reader.ReadInt32();  // 读取块数据长度
            blockCount = reader.ReadInt32();  // 读取块数量
            reader.ReadBytes(12); // 跳过读取12字节
            index = Index(mainIndex, subIndex, orientation); // 计算材质索引
        }
        // 计算瓦片的索引
        static public int Index(int mainIndex, int subIndex, int orientation)
        {
            // 将主索引左移6位，加上子索引，结果再左移5位，最后加上朝向
            return (((mainIndex << 6) + subIndex) << 5) + orientation;
        }
    }
    /// <summary>
    /// 导入结果结构体
    /// </summary>
    public struct ImportResult
    {
        /// <summary>
        /// 瓦片数组
        /// </summary>
        public Tile[] tiles;
        /// <summary>
        /// 纹理数组
        /// </summary>
        public Texture2D[] textures;
    }
    /// <summary>
    /// 缓存字典,键是文件路径,值是导入结果
    /// </summary>
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
    /// <returns>返回瓦片数组</returns>
    static public ImportResult Import(string dt1Path)
    {
        ///如果字典中有这个文件,直接返回字典中的结果
        if(cache.ContainsKey(dt1Path))
        {
            return cache[dt1Path];
        }
        //新建一个变量,代表导入的结果
        var importResult = new ImportResult();
        var stream = new BufferedStream(File.OpenRead(dt1Path));  // 打开文件流
        var reader = new BinaryReader(stream);  // 创建二进制读取器
        int version1 = reader.ReadInt32();  // 读取版本号1
        int version2 = reader.ReadInt32();  // 读取版本号2
        if (version1 != 7 || version2 != 6)  // 检查版本号
        {
            //版本是7.6就退出,这个版本读取不了
            Debug.Log(string.Format("无法读取dt1文件, 错误的版本: ({0}.{1})", version1, version2));
            //返回空的导入结果
            return importResult;
        }
        reader.ReadBytes(260);  // 跳过260字节的未知数据(只读取没赋值)
        int tileCount = reader.ReadInt32();  // 读取瓦片的总数
        reader.ReadInt32(); // 跳过4个字节
        Tile[] tiles = new Tile[tileCount];  // 创建瓦片数组,长度是瓦片总数
        //新建一个变量,代表纹理打包器
        var packer = new TexturePacker(2048, 2048);
        //新建一个变量,代表材质
        Material material = null;
        //遍历瓦片总数,把所有合规的瓦片加入到数组中
        // 遍历所有瓦片
        for (int i = 0; i < tileCount; ++i)
        {
            // 调用Tile结构体的Read方法，读取单个瓦片的数据
            tiles[i].Read(reader);
            // 将瓦片的宽度和高度（取负）放入纹理打包器，获取打包结果
            var result = packer.put(tiles[i].width, -tiles[i].height);
            // 如果打包器生成了新的纹理
            if (result.newTexture)
            {
                // 创建一个新的材质，使用"Sprite"着色器
                material = new Material(Shader.Find("Sprite"));
                // 给材质命名，附加文件路径
                material.name += "(" + dt1Path + ")";
                // 设置材质的主纹理为打包器生成的纹理
                material.mainTexture = result.texture;
            }

            // 设置瓦片的纹理X坐标
            tiles[i].textureX = result.x;
            // 设置瓦片的纹理Y坐标
            tiles[i].textureY = result.y;
            // 设置瓦片的纹理
            tiles[i].texture = result.texture;
            // 设置瓦片的材质
            tiles[i].material = material;

            // 如果瓦片朝向为0或15，并且高度不为0
            if ((tiles[i].orientation == 0 || tiles[i].orientation == 15) && tiles[i].height != 0)
            {
                // 地板或屋顶类型，设置固定高度为-80
                tiles[i].height = -80;
            }
            // 如果瓦片朝向在1到14之间
            else if (tiles[i].orientation > 0 && tiles[i].orientation < 15)
            {
                // 调整纹理Y坐标，减去瓦片高度
                tiles[i].textureY += (-tiles[i].height);
            }
        }

        // 输出日志，显示文件路径、瓦片总数和纹理数量
        Debug.Log(dt1Path + ", 瓦片总数: " + tileCount + ", " +  " 纹理总数: " + packer.textures.Count);
        // 创建一个1024字节的块数据缓冲区
        byte[] blockData = new byte[1024];
        // 遍历所有瓦片
        for (int i = 0; i < tileCount; ++i)
        {
            // 获取当前瓦片
            var tile = tiles[i];

            // 如果瓦片的宽度或高度为0
            if (tile.width == 0 || tile.height == 0)
            {
                // 输出日志，显示无效的瓦片尺寸
                Debug.Log(string.Format("错误,瓦片尺寸: {0}x{1}", tile.width, tile.height));
                // 跳过当前瓦片，继续处理下一个
                continue;
            }

            // 将文件流定位到当前瓦片的块头指针位置
            stream.Seek(tile.blockHeaderPointer, SeekOrigin.Begin);  // 定位到块头指针
            for (int block = 0; block < tile.blockCount; ++block)  // 遍历块
            {
                int x = reader.ReadInt16();  // 读取X坐标
                int y = reader.ReadInt16();  // 读取Y坐标
                reader.ReadBytes(2); // 跳过2字节
                reader.ReadByte(); // 跳过1字节
                reader.ReadByte(); // 跳过1字节
                short format = reader.ReadInt16();  // 读取格式
                int length = reader.ReadInt32();  // 读取长度
                reader.ReadBytes(2); // 跳过两字节
                int fileOffset = reader.ReadInt32();  // 读取文件偏移量
                int blockDataPosition = tile.blockHeaderPointer + fileOffset;  // 计算块数据位置

                var positionBeforeSeek = stream.Position;  // 保存当前位置
                stream.Seek(blockDataPosition, SeekOrigin.Begin);  // 定位到块数据位置

                if (blockData.Length < length)  // 如果块数据数组长度不足,那就扩展到标准长度
                    blockData = new byte[length];
                reader.Read(blockData, 0, length);  // 读取块数据
                if (format == 1)  // 如果是等距格式
                {
                    // 绘制等距块
                    drawBlockIsometric(tile.texture, tile.textureX + x, tile.textureY + y, blockData, length);
                }
                else  // 如果是普通格式
                {
                    // 绘制普通块
                    drawBlockNormal(tile.texture, tile.textureX + x, tile.textureY + y, blockData, length);
                }

                stream.Seek(positionBeforeSeek, SeekOrigin.Begin);  // 恢复到之前的位置
            }
        }
          // 遍历所有纹理,并且应用纹理
        foreach(var texture in packer.textures) texture.Apply();
        //关闭文件流
        stream.Close();
        //把瓦片数组赋值给导入结果
        importResult.tiles = tiles;
        //把纹理数组赋值给导入结果
        importResult.textures = packer.textures.ToArray();
        //把导入结果添加到缓存字典中
        cache[dt1Path] = importResult;
        //返回导入结果
        return importResult;
    }
    // 静态方法，用于将DT1文件转换为PNG格式
    static public void ConvertToPng(string assetPath)
    {
        // 加载调色板，参数1表示使用第一个调色板
        Palette.LoadPalette(1);
        // 导入DT1文件，获取导入结果
        ImportResult result = Import(assetPath);
        // 初始化计数器
        int i = 0;
        // 遍历导入结果中的所有纹理
        foreach(var texture in result.textures)
        {
            // 将纹理编码为PNG格式的字节数组
            var pngData = texture.EncodeToPNG();
            // 生成PNG文件的保存路径，附加计数器作为后缀
            var pngPath = assetPath + "." + i + ".png";
            // 将PNG数据写入文件
            File.WriteAllBytes(pngPath, pngData);
            // 通知Unity资源数据库导入新创建的PNG文件
            AssetDatabase.ImportAsset(pngPath);
            // 计数器递增
            i++;
        }
    }

    /// <summary>
    /// 绘制块(绘制瓦片的图片)
    /// </summary>
    /// <param name="texture">图片文件</param>
    /// <param name="x0">X轴</param>
    /// <param name="y0">Y轴</param>
    /// <param name="data">数据</param>
    /// <param name="length">数据长度</param>
    static void drawBlockNormal(Texture2D texture, int x0, int y0, byte[] data, int length)
    {
        int ptr = 0;  // 数据索引
        int x = 0;  // X坐标
        int y = 0;  // Y坐标

        while (length > 0)  // 遍历数据
        {
            byte b1 = data[ptr];  // 读取第一个字节
            byte b2 = data[ptr + 1];  // 读取第二个字节
            ptr += 2;  // 移动索引
            length -= 2;  // 减少剩余长度
            if (b1 != 0 || b2 != 0)  // 如果字节不为0
            {
                x += b1;  // 更新X坐标
                length -= b2;  // 减少剩余长度,每次都是1个b2的长度
                while (b2 != 0)  // 绘制像素
                {
                    texture.SetPixel(x0 + x, -y0 - y, Palette.palette[data[ptr]]);  // 设置像素颜色
                    //索引+1
                    ptr++;
                    //x+1
                    x++;
                    //像素数量-1
                    b2--;
                }
            }
            else  // 如果字节为0
            {
                x = 0;  // 重置X坐标
                y++;  // 增加Y坐标
            }
        }
    }

    static int[] xjump = { 14, 12, 10, 8, 6, 4, 2, 0, 2, 4, 6, 8, 10, 12, 14 };  // X跳跃数组
    static int[] nbpix = { 4, 8, 12, 16, 20, 24, 28, 32, 28, 24, 20, 16, 12, 8, 4 };  // 像素数量数组

    /// <summary>
    /// 绘制等距块(绘制等距瓦片的图片)
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="x0"></param>
    /// <param name="y0"></param>
    /// <param name="data"></param>
    /// <param name="length"></param>
    static void drawBlockIsometric(Texture2D texture, int x0, int y0, byte[] data, int length)
    {
        int ptr = 0;  // 数据指针
        int x, y = 0, n;  // X坐标, Y坐标, 像素数量

        // 3d-isometric subtile is 256 bytes, no more, no less 
        Debug.Assert(length == 256);  // 断言长度为256
        if (length != 256)
            return;

        while (length > 0)  // 遍历数据
        {
            x = xjump[y];  // 获取X跳跃值
            n = nbpix[y];  // 获取像素数量
            length -= n;  // 减少剩余长度
            while (n != 0)  // 绘制像素
            {
                texture.SetPixel(x0 + x, -y0 - y, Palette.palette[data[ptr]]);  // 设置像素颜色
                ptr++;
                x++;
                n--;
            }
            y++;  // 增加Y坐标
        }
    }
}
