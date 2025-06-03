using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 纹理包,就是Unity中图片切精灵的功能
/// </summary>
/// <remarks>
/// 实际并没有分割原图,就是提供了锚点和宽高,读取图片的时候,根据锚点和宽高读取对应的部分
/// </remarks>
class TexturePacker
{
    /// <summary>
    /// 原图最大宽度
    /// </summary>
    /// <remarks>
    /// 在外面给赋值是2048
    /// </remarks>
    int maxWidth;
    /// <summary>
    /// 原图最大高度
    /// </summary>
    /// <remarks>
    /// 在外面给赋值是2048
    /// </remarks>
    int maxHeight;
    /// <summary>
    /// 切片锚点的X指针
    /// </summary>
    int xPos = 0;
    /// <summary>
    /// 切片锚点的Y指针
    /// </summary>
    int yPos = 0;
    /// <summary>
    /// 切片的高度,一行满了,就会以这个为依据换行
    /// </summary>
    int rowHeight = 0;
    /// <summary>
    /// 打包结果
    /// </summary>
    /// <remarks>
    /// 切片的锚点,以及是否满了,满了就需要新建一个图
    /// </remarks>
    public struct PackResult
    {
        /// <summary>
        /// 切片的锚点X
        /// </summary>
        public int x;
        /// <summary>
        /// 切片的锚点Y
        /// </summary>
        public int y;
        /// <summary>
        /// 是否满了,满了就需要新建一个图
        /// </summary>
        public bool newTexture;
    }
    /// <summary>
    /// 纹理包的构造函数,就是定义一下最大宽高
    /// </summary>
    /// <param name="maxWidth">原图最大高</param>
    /// <param name="maxHeight">原图最大宽</param>
    public TexturePacker(int maxWidth, int maxHeight)
    {
        this.maxWidth = maxWidth;
        this.maxHeight = maxHeight;
    }
    /// <summary>
    /// 打包,就是Unity中精灵编辑器
    /// </summary>
    /// <param name="width">瓦片的高度</param>
    /// <param name="height">瓦片的宽度(起始坐标在原图的左上,所以形参会先转负数)</param>
    /// <returns>切片的锚点信息,或者需要创建新的原图</returns>
    /// <remarks>
    /// 按照DT1获取的宽高信息,顺序排列,这刚好对应了需切的图; 
    /// 然后返回锚点信息, 以便后续读取素材
    /// </remarks>
    public PackResult put(int width, int height)
    {
        //行高最小也要大于等于切片的高度
        rowHeight = Mathf.Max(height, rowHeight);
        //行到头了就新起一行
        if (xPos + width > maxWidth)
        {
            xPos = 0;
            yPos += rowHeight;
            rowHeight = height;
        }
        //列到头了就就重置指针,准备新开一张图
        if (yPos + rowHeight > maxHeight)
        {
            xPos = 0;
            yPos = 0;
            rowHeight = height;
        }
        //声明一个结果准备返回
        var result = new PackResult();
        //返回结果是切片的锚点,如果锚点被重置了,就说明需要新建一个图
        result.x = xPos;
        result.y = yPos;
        result.newTexture = xPos == 0 && yPos == 0;
        //指针右移一张图
        xPos += width;
        //返回结果
        return result;
    }
}
