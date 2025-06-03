
/// <summary>
/// 整数矩形
/// </summary>
/// <remarks>
/// 类似于vector类型,这个类代表一个矩形的几何形状,它是整型的
/// </remarks>
public struct IntRect
{
    /// <summary>
    /// 一个0矩形
    /// </summary>
    public static IntRect zero = new IntRect(0, 0, 0, 0);

    /// <summary>
    /// 锚点x(左上)
    /// </summary>
    int _x;
    /// <summary>
    /// 锚点y(左上)
    /// </summary>
    int _y;
    /// <summary>
    /// 宽
    /// </summary>
    int _width;
    /// <summary>
    /// 高
    /// </summary>
    int _height;

    /// <summary>
    /// 整数矩形的构造函数
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public IntRect(int x, int y, int width, int height)
    {
        _x = x;
        _y = y;
        _width = width;
        _height = height;
    }

    /// <summary>
    /// 矩形在X轴上的最大值
    /// </summary>
    public int xMax
    {
        get
        {
            return _x + _width - 1;
        }

        set
        {
            _width = value - _x + 1;
        }
    }
    /// <summary>
    /// 矩形在Y轴上的最大值
    /// </summary>
    public int yMax
    {
        get
        {
            return _y;
        }

        set
        {
            int delta = _y - value;
            _height -= delta;
            _y = value;
        }
    }

    /// <summary>
    /// 矩形在X轴上的最小值
    /// </summary>
    public int xMin
    {
        get
        {
            return _x;
        }

        set
        {
            int delta = value - _x;
            _width -= delta;
            _x = value;
        }
    }

    /// <summary>
    /// 矩阵在Y轴上的最小值
    /// </summary>
    public int yMin
    {
        get
        {
            return _y - _height + 1;
        }

        set
        {
            _height = _y - value + 1;
        }
    }

    /// <summary>
    /// 宽
    /// </summary>
    public int width
    {
        get
        {
            return _width;
        }

        set
        {
            _width = value;
        }
    }

    /// <summary>
    /// 高
    /// </summary>
    public int height
    {
        get
        {
            return _height;
        }

        set
        {
            _height = value;
        }
    }

    /// <summary>
    /// 输出字符串
    /// </summary>
    /// <returns></returns>
    public string AsString()
    {
        return string.Format("({0}, {1})  --->  ({2}, {3})  =  {4} * {5}", xMin, yMin, xMax, yMax, width, height);
    }
}
