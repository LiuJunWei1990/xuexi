using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

// DC6类，用于处理DC6格式的字体文件
class DC6
{
    // 从DC6文件创建字体
    static public void CreateFontFromDC6(string filename)
    {
        // 加载调色板1
        Palette.LoadPalette(1);

        // 打开文件流并创建二进制读取器
        var stream = File.OpenRead(filename);
        var reader = new BinaryReader(stream);

        // 读取DC6文件头信息,包括版本号等
        int dc6_ver1 = reader.ReadInt32();
        var dc6_ver2 = reader.ReadInt32();
        var dc6_ver3 = reader.ReadInt32();
        reader.ReadInt32(); // 跳过字段
        var dc6_dir = reader.ReadInt32(); // 方向数（可能用于动画方向，但未在后续代码中使用）
        var dc6_fpd = reader.ReadInt32(); // 用于确定字符信息的数量
        var dc6_fptr = stream.Position; // 记录帧指针表位置

        // 检查DC6版本是否匹配,下面这3个版本的都是错的,有其中一个就报错,返回
        if ((dc6_ver1 != 6) || (dc6_ver2 != 1) || (dc6_ver3 != 0))
        {
            Debug.LogWarning("未知dc6版本: " + dc6_ver1 + " " + dc6_ver2 + " " + dc6_ver3);
            return;
        }

        // 初始化纹理打包器和相关变量
        //新建一个纹理尺寸的常量
        const int textureSize = 512;
        //新建一个纹理
        var texture = new Texture2D(textureSize, textureSize, TextureFormat.ARGB32, false);
        //新建一个像素点数组
        var pixels = new Color32[textureSize * textureSize];

        //新建一个纹理打包器
        var packer = new TexturePacker(textureSize, textureSize);
        //新建一个字节数据数组,长度为1024
        byte[] data = new byte[1024];
        //CharacterInfo,Character代表多个意思,这里应该是指字符,这个类型是字符信息类
        //这是Unity的一个自带类型,用来储存艺术字的纹理信息,位置,大小,偏移量等等
        var characterInfo = new CharacterInfo[dc6_fpd];

        //deepseek说这个临时变量没用,我也不知道干什么的,先保留吧
        int dir = 0;
        // 处理每个字符信息
        for (int i = 0; i < dc6_fpd; i++)
        {
            // 定位到当前帧的偏移量(由于dir是0,一顿操作实际上dc6_fptr加了个0)
            stream.Seek(dc6_fptr + (dir * dc6_fpd + i) * 4, SeekOrigin.Begin);
            long offset = reader.ReadInt32();
            stream.Seek(offset, SeekOrigin.Begin);

            // 读取帧信息
            reader.ReadInt32(); // 跳过未知字段
            int f_w = reader.ReadInt32(); // 帧宽度
            int f_h = reader.ReadInt32(); // 帧高度
            int f_offx = reader.ReadInt32(); // X偏移
            int f_offy = reader.ReadInt32(); // Y偏移
            reader.ReadInt32(); // 跳过未知字段
            reader.ReadInt32(); // 跳过未知字段
            int f_len = reader.ReadInt32(); // 帧数据长度

            // 如果数据缓冲区不够大，重新分配
            if (data.Length < f_len)
                data = new byte[f_len];

            // 读取帧数据
            reader.Read(data, 0, f_len);

            // 将帧打包到纹理中
            var pack = packer.put(f_w, f_h);
            drawFrame(data, f_len, pixels,textureSize, pack.x, pack.y + f_h);

            // 设置字符信息
            // 设置字符的索引值
            characterInfo[i].index = i;
            // 设置字符的宽度（用于字符间距）
            characterInfo[i].advance = f_w;

            // 设置字符在X轴上的最小边界
            characterInfo[i].minX = 0;
            // 设置字符在X轴上的最大边界
            characterInfo[i].maxX = f_w;
            // 设置字符在Y轴上的最小边界
            characterInfo[i].minY = -f_h;
            // 设置字符在Y轴上的最大边界
            characterInfo[i].maxY = 0;

            // 设置字符左下角的UV坐标
            characterInfo[i].uvBottomLeft = new Vector2(
                pack.x / (float)textureSize, 
                (textureSize - (pack.y + f_h)) / (float)textureSize);
            // 设置字符右下角的UV坐标
            characterInfo[i].uvBottomRight = new Vector2(
                (pack.x + f_w) / (float)textureSize, 
                (textureSize - (pack.y + f_h)) / (float)textureSize);
            // 设置字符左上角的UV坐标
            characterInfo[i].uvTopLeft = new Vector2(
                pack.x / (float)textureSize, 
                (textureSize - pack.y) / (float)textureSize);
            // 设置字符右上角的UV坐标
            characterInfo[i].uvTopRight = new Vector2(
                (pack.x + f_w) / (float)textureSize, 
                (textureSize - pack.y) / (float)textureSize);
        }

        // 关闭文件流
        stream.Close();

        // 生成输出文件名和路径
        var name = Path.GetFileNameWithoutExtension(filename);
        var filepath = Path.GetDirectoryName(filename) + "/" + name;

        // 处理生成的纹理
        texture.SetPixels32(pixels);
        texture.Apply();
        var pngData = texture.EncodeToPNG();
        Object.DestroyImmediate(texture);
        var texturePath = filepath + ".png";
        File.WriteAllBytes(texturePath, pngData);

        // 创建字体资源
        var fontPath = filepath + ".fontsettings";
        AssetDatabase.CreateAsset(new Font(name), fontPath);
        var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        font.characterInfo = characterInfo;
        EditorUtility.SetDirty(font);

        // 刷新资源数据库
        AssetDatabase.Refresh();
    }

    // 绘制单个帧到纹理
    static void drawFrame(byte[] data, int size, Color32[] pixels, int textureSize, int x0, int y0)
    {
        int dst = textureSize * textureSize - y0 * textureSize - textureSize;
        int ptr = 0;
        int i2, x = x0, y = y0, c, c2;

        // 处理帧数据
        for (int i = 0; i < size; i++)
        {
            c = data[ptr];
            ++ptr;

            if (c == 0x80) // 换行标记
            {
                x = x0;
                y--;
                dst += textureSize;
            }
            else if ((c & 0x80) != 0) // 透明像素
            {
                x += c & 0x7F;
            }
            else // 绘制像素
            {
                for (i2 = 0; i2 < c; i2++)
                {
                    c2 = data[ptr];
                    ++ptr;
                    i++;
                    pixels[dst + x] = Palette.palette[c2];
                    x++;
                }
            }
        }
    }
}
