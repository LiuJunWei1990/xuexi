// 定义 IntRect 结构体，用于表示一个整数矩形
public struct IntRect
{
    // 静态变量，表示一个零矩形
    public static IntRect zero = new IntRect(0, 0, 0, 0);

    // 矩形的 X 坐标
    int _x;
    // 矩形的 Y 坐标
    int _y;
    // 矩形的宽度
    int _width;
    // 矩形的高度
    int _height;

    // 构造函数，初始化矩形
    public IntRect(int x, int y, int width, int height)
    {
        _x = x;
        _y = y;
        _width = width;
        _height = height;
    }

    // 获取或设置矩形的最大 X 坐标
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

    // 获取或设置矩形的最大 Y 坐标
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

    // 获取或设置矩形的最小 X 坐标
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

    // 获取或设置矩形的最小 Y 坐标
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

    // 获取或设置矩形的宽度
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

    // 获取或设置矩形的高度
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

    // 返回矩形的字符串表示
    public string AsString()
    {
        return string.Format("({0}, {1})  --->  ({2}, {3})  =  {4} * {5}", xMin, yMin, xMax, yMax, width, height);
    }
}
