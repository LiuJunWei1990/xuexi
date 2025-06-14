
/// <summary>
/// [自定义几何类] 整数矩形类
/// </summary>
/// <remarks>
/// <para> Unity中Rect只能处理浮点数, 所以自己写一个整数矩形类</para>
/// <para> 用代码表述一个矩形, 可以方便计算</para>
/// </remarks>
public struct IntRect
{
    /// <summary>
    /// 一个0矩形
    /// </summary>
    public static IntRect zero = new IntRect(0, 0, 0, 0);

    /// <summary>
    /// 起始点x(左上)
    /// </summary>
    int _x;
    /// <summary>
    /// 起始点y(左上)
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
    /// 初始化整数矩形类
    /// </summary>
    /// <param name="x">起始点x(左上)</param>
    /// <param name="y">起始点y(左上)</param>
    /// <param name="width">宽</param>
    /// <param name="height">高</param>
    /// <remarks>
    /// 用代码表述一个整数单位的矩形
    /// </remarks>
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
