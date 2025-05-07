// 引入必要的命名空间
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

// 定义DCC类，用于处理DCC文件,DCC文件是一种用于存储动画的文件格式
public class DCC
{
    // 定义ImportResult结构体，用于存储导入结果
    public struct ImportResult
    {
        public List<Texture2D> textures;  // 存储生成的纹理列表
        public IsoAnimation anim;         // 存储生成的动画
    }

    // 静态方法，用于加载DCC文件
    static public ImportResult Load(string filename)
    {
        // 初始化导入结果
        ImportResult result = new ImportResult();
        result.textures = new List<Texture2D>();  // 初始化纹理列表
        var sprites = new List<Sprite>();          // 初始化精灵列表

        // 定义纹理大小
        const int textureSize = 2048;
        // 创建纹理打包器
        var packer = new TexturePacker(textureSize, textureSize);
        Texture2D texture = null;  // 当前纹理
        Color32[] pixels = null;   // 当前纹理的像素数据

        // 读取DCC文件的所有字节
        byte[] dcc = File.ReadAllBytes(filename);
        // 创建内存流
        var stream = new MemoryStream(dcc);
        // 创建二进制读取器
        var reader = new BinaryReader(stream);
        // 创建位读取器
        var bitReader = new BitReader(stream);

        // 读取文件签名
        byte fileSignature = reader.ReadByte();
        // 读取版本号
        byte version = reader.ReadByte();
        // 读取方向数量
        byte directionCount = reader.ReadByte();
        // 读取每个方向的帧数
        int framesPerDir = reader.ReadInt32();
        // 读取标签
        int tag = reader.ReadInt32();
        // 读取最终的DC6大小
        int finalDc6Size = reader.ReadInt32();
        // 初始化方向偏移数组
        int[] dirOffset = new int[directionCount];

        // 遍历每个方向，读取偏移量
        for(int dir = 0; dir < directionCount; ++dir)
        {
            dirOffset[dir] = reader.ReadInt32();
        }

        // 定义宽度表
        int[] widthTable = { 0, 1, 2, 4, 6, 8, 10, 12, 14, 16, 20, 24, 26, 28, 30, 32 };

        // 遍历每个方向
        for (int dir = 0; dir < directionCount; ++dir)
        {
            // 跳转到当前方向的偏移位置
            stream.Seek(dirOffset[dir], SeekOrigin.Begin);
            // 读取编码后的大小
            int outsizeCoded = reader.ReadInt32();
            // 读取压缩标志
            int compressionFlag = bitReader.ReadBits(2);
            // 读取variable0的位数
            int variable0Bits = bitReader.ReadBits(4);
            // 读取宽度的位数
            int widthBits = bitReader.ReadBits(4);
            // 读取高度的位数
            int heightBits = bitReader.ReadBits(4);
            // 读取x偏移的位数
            int xoffsetBits = bitReader.ReadBits(4);
            // 读取y偏移的位数
            int yoffsetBits = bitReader.ReadBits(4);
            // 读取可选字节的位数
            int optionalBytesBits = bitReader.ReadBits(4);
            // 读取编码字节的位数
            int codedBytesBits = bitReader.ReadBits(4);

            // 初始化可选字节总和
            int optionalBytesSum = 0;

            // 遍历每个帧
            for (int f = 0; f < framesPerDir; ++f)
            {
                // 读取variable0
                int variable0 = bitReader.ReadBits(widthTable[variable0Bits]);
                // 读取宽度
                int width = bitReader.ReadBits(widthTable[widthBits]);
                // 读取高度
                int height = bitReader.ReadBits(widthTable[heightBits]);
                // 读取x偏移
                int xoffset = bitReader.ReadSigned(widthTable[xoffsetBits]);
                // 读取y偏移
                int yoffset = bitReader.ReadSigned(widthTable[yoffsetBits]);
                // 读取可选字节
                int optionalBytes = bitReader.ReadBits(widthTable[optionalBytesBits]);
                // 读取编码字节
                int codedBytes = bitReader.ReadBits(widthTable[codedBytesBits]);
                // 读取bottomUp标志
                int bottomUp = bitReader.ReadBits(1);

                // 累加可选字节
                optionalBytesSum += optionalBytes;

                // 定义填充大小
                int padding = 2;
                // 将当前帧打包到纹理中
                var pack = packer.put(width + padding, height + padding);
                // 如果需要创建新纹理
                if (pack.newTexture)
                {
                    // 如果当前纹理不为空，应用像素数据
                    if (texture != null)
                    {
                        texture.SetPixels32(pixels);
                        texture.Apply();
                    }
                    // 创建新纹理
                    texture = new Texture2D(textureSize, textureSize, TextureFormat.ARGB32, false);
                    // 初始化像素数据
                    pixels = new Color32[textureSize * textureSize];
                    // 将新纹理添加到结果中
                    result.textures.Add(texture);
                }

                // 调试帧边界
                for (int i = 0; i < 10; ++i)
                    pixels[textureSize * (pack.y + height) + pack.x + i] = Color.red;
                for (int i = 0; i < 10; ++i)
                    pixels[textureSize * (pack.y + height - 1) + pack.x + i] = Color.red;
                for (int i = 0; i < 10; ++i)
                    pixels[textureSize * (pack.y + height - i) + pack.x] = Color.red;
                for (int i = 0; i < 10; ++i)
                    pixels[textureSize * (pack.y + height - i) + pack.x + 1] = Color.red;
                for (int i = 0; i < 10; ++i)
                    pixels[textureSize * pack.y + pack.x - i + width] = Color.blue;
                for (int i = 0; i < 10; ++i)
                    pixels[textureSize * (pack.y + i) + pack.x + width] = Color.blue;
                for (int i = 0; i < 10; ++i)
                    pixels[textureSize * (pack.y + 1) + pack.x - i + width] = Color.blue;
                for (int i = 0; i < 10; ++i)
                    pixels[textureSize * (pack.y + i) + pack.x + width - 1] = Color.blue;

                // 创建精灵矩形
                var spriteRect = new Rect(pack.x, pack.y, width, height);
                // 计算精灵的轴心点
                var pivot = new Vector2(-xoffset / (float)width, yoffset / (float)height);
                // 创建精灵
                Sprite sprite = Sprite.Create(texture, spriteRect, pivot, Iso.pixelsPerUnit);
                // 将精灵添加到列表中
                sprites.Add(sprite);
            }

            // 跳过可选字节
            stream.Seek(optionalBytesSum, SeekOrigin.Current);
            // 重置位读取器
            bitReader.Reset();
            // 如果压缩标志包含0x02
            if ((compressionFlag & 0x02) != 0)
            {
                // 读取相等单元格大小
                int equalCellSize = bitReader.ReadBits(20);
            }

            // 读取像素掩码大小
            int pixelMaskSize = bitReader.ReadBits(20);
            // 如果压缩标志包含0x01
            if ((compressionFlag & 0x01) != 0)
            {
                // 读取编码类型大小
                int encodingTypeSize = bitReader.ReadBits(20);
                // 读取原始像素大小
                int rawPixelSize = bitReader.ReadBits(20);
            }

            // 读取256位的像素值键
            bitReader.ReadBits(256);
        }

        // 如果当前纹理不为空，应用像素数据
        if (texture != null)
        {
            texture.SetPixels32(pixels);
            texture.Apply();
        }

        // 创建动画实例
        result.anim = ScriptableObject.CreateInstance<IsoAnimation>();
        // 设置动画的方向数量
        result.anim.directionCount = directionCount;
        // 初始化动画状态
        result.anim.states = new IsoAnimation.State[1];
        result.anim.states[0] = new IsoAnimation.State();
        // 设置动画状态的名称
        result.anim.states[0].name = "Generated from DCC";
        // 设置动画状态的精灵
        result.anim.states[0].sprites = sprites.ToArray();

        // 返回导入结果
        return result;
    }

    // 静态方法，用于将DCC文件转换为PNG
    static public void ConvertToPng(string assetPath)
    {
        // 加载调色板
        Palette.LoadPalette(1);
        // 加载DCC文件
        ImportResult result = Load(assetPath);
        int i = 0;
        // 遍历所有纹理
        foreach (var texture in result.textures)
        {
            // 将纹理编码为PNG
            var pngData = texture.EncodeToPNG();
            // 销毁纹理
            Object.DestroyImmediate(texture);
            // 定义PNG文件路径
            var pngPath = assetPath + "." + i + ".png";
            // 写入PNG文件
            File.WriteAllBytes(pngPath, pngData);
            // 导入PNG文件到资源数据库
            AssetDatabase.ImportAsset(pngPath);
        }
    }
}