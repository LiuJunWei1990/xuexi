using System.Collections.Generic;
using UnityEngine;

// 纹理打包器类，用于将多个小纹理打包到一个大纹理中
class TexturePacker
{
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
    }

    // 构造函数，初始化最大宽度和高度
    public TexturePacker(int maxWidth, int maxHeight)
    {
        this.maxWidth = maxWidth;
        this.maxHeight = maxHeight;
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

        if (yPos + rowHeight > maxHeight)
        {
            // 重置坐标和行高度
            xPos = 0;
            yPos = 0;
            rowHeight = height;
        }

        // 创建打包结果
        var result = new PackResult();
        result.x = xPos;  // 设置X坐标
        result.y = yPos;  // 设置Y坐标
        result.newTexture = xPos == 0 && yPos == 0;  // 设置是否创建了新纹理

        // 更新X坐标
        xPos += width;

        return result;
    }
}
