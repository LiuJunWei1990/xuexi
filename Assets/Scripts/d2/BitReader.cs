// 定义 BitReader 类，用于按位读取字节数组
public class BitReader
{
    // 字节数组
    private byte[] bytes;
    // 当前字节的索引
    private int byteIndex = 0;
    // 当前字节
    private int currentByte;
    // 当前位索引
    public int bitIndex = 8;

    // 构造函数，初始化 BitReader
    public BitReader(byte[] bytes, long offset = 0)
    {
        // 设置字节数组
        this.bytes = bytes;
        // 计算字节索引(因为一个字节有8位比特)
        byteIndex = (int) offset / 8;
        // 计算位索引(因为一个字节有8位比特)
        bitIndex = (int) (offset % 8);
        // 读取当前字节
        currentByte = bytes[byteIndex++];
    }

    // 读取一个位
    public int ReadBit()
    {
        // 如果当前位索引超过 8，读取下一个字节
        if (bitIndex >= 8)
        {
            currentByte = bytes[byteIndex++];
            bitIndex = 0;
        }
        // 获取当前位的值
        int result = (currentByte >> bitIndex) & 1;
        // 增加位索引
        ++bitIndex;
        return result;
    }

    // 读取多个位
    public int ReadBits(int count)
    {
        int result = 0;
        // 逐个读取位并组合成结果
        for (int i = 0; i < count; ++i)
        {
            result += ReadBit() << i;
        }
        return result;
    }

    // 读取有符号数
    public int ReadSigned(int count)
    {
        // 读取无符号数
        int result = ReadBits(count);
        // 如果最高位为 1，表示负数，扩展符号位
        if ((result & (1 << (count - 1))) != 0)
        {
            result |= ~((1 << count) - 1);
        }
        return result;
    }

    // 重置位索引
    public void Reset()
    {
        bitIndex = 8;
    }

    // 获取剩余位数
    public int bitsLeft
    {
        get { return 8 - bitIndex; }
    }

    // 获取当前偏移量
    public long offset
    {
        get { return byteIndex * 8 - bitsLeft; }
    }
}
