// 引入IO命名空间，用于流操作
using System.IO;

// 定义BitReader类，用于按bit读取数据
public class BitReader
{
    // 用于读取的流对象
    private Stream stream;
    // 当前读取的字节
    private int current;
    // 索引,默认再已经读取完的位置,即8,这样第一次读取的时候会读取一个新的字节
    private int index = 8;

    // 构造函数，接收一个流对象
    public BitReader(Stream stream)
    {
        // 初始化流对象
        this.stream = stream;
    }

    // 读取单个bit
    public int ReadBit()
    {
        // 如果当前字节的所有bit都已读取
        if (index >= 8)
        {
            // 读取一个新的字节
            current = stream.ReadByte();
            // 重置bit位置
            index = 0;
        }
        // 读取current字节的第index个字
        int result = (current >> index) & 1;
        // 移动到下一个bit位置
        ++index;
        // 返回读取的bit值
        return result;
    }

    // 读取指定数量的bit
    public int ReadBits(int count)
    {
        // 初始化结果
        int result = 0;
        // 循环读取每个bit
        for (int i = 0; i < count; ++i)
        {
            // 将读取的bit值按位组合
            result += ReadBit() << i;
        }
        // 返回组合后的结果
        return result;
    }

    // 读取有符号数
    public int ReadSigned(int count)
    {
        // 先读取指定数量的bit
        int result = ReadBits(count);
        // 如果最高位是1（表示负数）
        if ((result & (1 << (count - 1))) != 0)
        {
            // 负数：扩展符号位
            result |= ~((1 << count) - 1);
        }
        // 返回最终的有符号数
        return result;
    }

    // 重置读取状态
    public void Reset()
    {
        // 将bit位置重置为8，表示需要读取新字节
        index = 8;
    }
}
