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
            index = Index(mainIndex, subIndex, orientation); // 计算索引
        }
        // 计算瓦片的索引
        static public int Index(int mainIndex, int subIndex, int orientation)
        {
            // 将主索引左移6位，加上子索引，结果再左移5位，最后加上朝向
            return (((mainIndex << 6) + subIndex) << 5) + orientation;
        }
    }
    /// <summary>
    /// 瓦片像素点数组:是一个颜色数组,看数量应该是对应的像素点数量,2048*2048
    /// </summary>
    static Color32[] transparentColors = new Color32[2048 * 2048];  // 透明颜色数组
    /// <summary>
    /// 导入DT1文件
    /// </summary>
    /// <param name="dt1Path">文件路径</param>
    /// <returns>返回瓦片数组</returns>
    static public Tile[] Import(string dt1Path)
    {
        var stream = new BufferedStream(File.OpenRead(dt1Path));  // 打开文件流
        var reader = new BinaryReader(stream);  // 创建二进制读取器
        int version1 = reader.ReadInt32();  // 读取版本号1
        int version2 = reader.ReadInt32();  // 读取版本号2
        if (version1 != 7 || version2 != 6)  // 检查版本号
        {
            //版本是7.6就退出,这个版本读取不了
            Debug.Log(string.Format("无法读取dt1文件, 错误的版本: ({0}.{1})", version1, version2));
            return null;
        }
        reader.ReadBytes(260);  // 跳过260字节的未知数据(只读取没赋值)
        int tileCount = reader.ReadInt32();  // 读取瓦片的总数
        int tileCountFit = 0;  // 新建一个变量,代表瓦片数组已容纳的瓦片数
        reader.ReadInt32(); // 跳过4个字节
        Tile[] tiles = new Tile[tileCount];  // 创建瓦片数组,长度是瓦片总数
        int height = 2048;  // 纹理高度
        int width = 2048;  // 纹理宽度
        int xPos = 0;  // 当前X位置
        int yPos = 0;  // 当前Y位置
        int rowHeight = 0;  // 行高
        //遍历瓦片总数,把所有合规的瓦片加入到数组中
        for (int i = 0; i < tileCount; ++i)
        {
            //调用上面那个Read方法,读取单个瓦片的数据
            tiles[i].Read(reader);
            //朝向索引不为0的瓦片跳过
            if (tiles[i].orientation != 0)
            {
                //因为流已经往前走了,下标退回原本位置,
                i -= 1;
                //瓦片总数减1
                tileCount -= 1;
                //相当于跳过了当前这个流,下一次循环会读取下一个流
                continue;
            }
            tiles[i].textureX = xPos;  // 设置瓦片的纹理X坐标
            tiles[i].textureY = yPos;  // 设置瓦片的纹理Y坐标
            rowHeight = Mathf.Max(-tiles[i].height, rowHeight);  // 计算行高,不能低于一个瓦片的高度
            //如果超出纹理宽度
            if (xPos + tiles[i].width > width)
            {
                //重置X位置
                xPos = 0;
                //Y位置加上行高
                yPos += rowHeight;
                //重置行高
                rowHeight = -tiles[i].height;
                //瓦片纹理X轴为0
                tiles[i].textureX = 0;
                //瓦片纹理Y轴为yPos
                tiles[i].textureY = yPos;
            }

            xPos += tiles[i].width;  // 更新X位置

            if (yPos + rowHeight > height)  // 如果超出纹理高度,就中止循环
            {
                break;
            }
            //成功到了这步说明瓦片加入到数组了,容纳的瓦片数+1
            tileCountFit += 1;
        }
        
        var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);  // 创建纹理
        texture.filterMode = FilterMode.Point;  // 设置纹理过滤模式
        texture.SetPixels32(transparentColors);  // 设置透明颜色

        var material = new Material(Shader.Find("Sprite"));  // 创建材质
        material.name += "(" + dt1Path + ")";  // 设置材质名称
        material.mainTexture = texture;  // 设置材质纹理

        Debug.Log("容纳瓦片: " + tileCountFit + " / " + tileCount);  // 输出容纳的瓦片数量
        byte[] blockData = new byte[1024];  // 创建块数据数组
        //遍历已容纳的瓦片数组
        for (int i = 0; i < tileCountFit; ++i)
        {
            tiles[i].material = material;  // 设置瓦片材质
            tiles[i].texture = texture;  // 设置瓦片纹理
            //获取当前遍历到的瓦片
            var tile = tiles[i];
            //如果瓦片的宽度或高度为0,就跳过
            if (tile.width == 0 || tile.height == 0) 
            {

                Debug.Log(string.Format("瓦片尺寸: {0}x{1}", tile.width, tile.height));
                continue;
            }

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
                if (tile.orientation > 0 && tile.orientation < 15)  // 如果朝向在1到14之间
                {
                    // 旋转180度,Y轴-瓦片长度,那就是反转了
                    y += (-tile.height);
                }

                if (format == 1)  // 如果是等距格式
                {
                    drawBlockIsometric(texture, tile.textureX + x, tile.textureY + y, blockData, length);
                }
                else  // 如果是普通格式
                {
                    drawBlockNormal(texture, tile.textureX + x, tile.textureY + y, blockData, length);
                }

                stream.Seek(positionBeforeSeek, SeekOrigin.Begin);  // 恢复到之前的位置
            }
        }

        texture.Apply();  // 应用纹理
        stream.Close();  // 关闭流

        return tiles;  // 返回瓦片数组
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
