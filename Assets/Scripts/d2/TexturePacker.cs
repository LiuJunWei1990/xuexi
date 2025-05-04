using System.Collections.Generic;
using UnityEngine;

// 纹理打包器类，用于将多个小纹理打包到一个大纹理中
class TexturePacker
{
    // 静态数组，用于存储透明颜色，大小为2048x2048
    static Color32[] transparentColors = new Color32[2048 * 2048];

    // 存储所有生成的纹理的列表
    public List<Texture2D> textures = new List<Texture2D>();

    // 当前正在使用的纹理
    Texture2D texture;
    // 纹理的最大宽度
    int maxWidth;
    // 纹理的最大高度
    int maxHeight;

    // 当前纹理的X坐标
    int xPos = 0;
    // 当前纹理的Y坐标
    int yPos = 0;
    // 当前行的高度
    int rowHeight = 0;

    // 打包结果结构体
    public struct PackResult
    {
        public int x;  // X坐标
        public int y;  // Y坐标
        public bool newTexture;  // 是否创建了新纹理
        public Texture2D texture;  // 纹理对象
    }

    // 构造函数，初始化最大宽度和高度
    public TexturePacker(int maxWidth, int maxHeight)
    {
        this.maxWidth = maxWidth;
        this.maxHeight = maxHeight;
    }

    // 创建新纹理的私有方法
    private Texture2D CreateTexture()
    {
        // 创建新的纹理对象，使用ARGB32格式，不生成mipmap
        var texture = new Texture2D(maxWidth, maxHeight, TextureFormat.ARGB32, false);
        // 设置纹理过滤模式为Point
        texture.filterMode = FilterMode.Point;
        // 将纹理的所有像素设置为透明
        texture.SetPixels32(transparentColors);
        // 将新纹理添加到纹理列表中
        textures.Add(texture);
        return texture;
    }

    // 将指定大小的区域放入纹理中
    public PackResult put(int width, int height)
    {   
        // 更新当前行的高度为最大高度
        rowHeight = Mathf.Max(height, rowHeight);
        // 如果当前X坐标加上宽度超过最大宽度
        if (xPos + width > maxWidth)
        {
            xPos = 0;  // 重置X坐标
            yPos += rowHeight;  // 增加Y坐标
            rowHeight = height;  // 重置行高度
        }

        // 判断是否需要创建新纹理
        bool newTexture = texture == null || yPos + rowHeight > maxHeight;
        if (newTexture)
        {
            // 创建新纹理
            texture = CreateTexture();
            // 重置坐标和行高度
            xPos = 0;
            yPos = 0;
            rowHeight = height;
        }

        // 创建打包结果
        var result = new PackResult();
        result.x = xPos;  // 设置X坐标
        result.y = yPos;  // 设置Y坐标
        result.texture = texture;  // 设置纹理对象
        result.newTexture = newTexture;  // 设置是否创建了新纹理

        // 更新X坐标
        xPos += width;

        return result;
    }
}
