using System;

// 二叉堆类，T 必须实现 IComparable 接口以便比较大小
public class BinaryHeap<T> where T: IComparable<T>
{
    // 存储堆元素的数组
    T[] items;
    // 当前堆中元素的数量
    int count = 0;
    // 堆的最大容量
    int maxSize;

    // 构造函数，初始化堆
    public BinaryHeap(int maxSize)
    {
        // 数组大小为 maxSize + 1，因为索引 0 不使用
        items = new T[maxSize + 1];
        this.maxSize = maxSize;
    }

    // 向堆中添加元素
    public void Add(T item)
    {
        // 元素数量加 1
        ++count;
        // 新元素的初始位置
        int index = count;
        // 将新元素放入数组
        items[index] = item;

        // 获取父节点位置
        int parent = index / 2;
        // 上浮操作：如果当前节点比父节点小，就交换位置
        while (index > 1 && items[parent].CompareTo(items[index]) > 0)
        {
            //交换两个变量的引用
            Swap(ref items[parent], ref items[index]);
            index = parent;
            parent = index / 2;
        }
    }

    // 取出堆顶元素（最小元素）
    public T Take()
    {
        // 获取堆顶元素
        T result = items[1];
        // 将最后一个元素移到堆顶
        items[1] = items[count];
        // 元素数量减 1
        --count;

        // 迭代计数器，防止死循环
        int iterCount = 0;

        // 下沉操作
        int index = 1;
        int child = index * 2;
        while (child <= count)
        {
            // 防止无限循环
            if (iterCount++ > 1000)
                throw new Exception("溢出");

            // 获取左右子节点
            int left = child;
            int right = child + 1;
            // 假设当前节点是最小的
            int smallest = index;
            // 如果左子节点更小
            if (items[smallest].CompareTo(items[left]) > 0)
                smallest = left;
            // 如果右子节点存在且更小
            if (right <= count && items[smallest].CompareTo(items[right]) > 0)
                smallest = right;
            // 如果当前节点不是最小的
            if (index != smallest)
            {
                // 交换位置
                Swap(ref items[index], ref items[smallest]);
                index = smallest;
                child = index * 2;
            }
            else
            {
                break;
            }
        }

        return result;
    }

    // 清空堆
    public void Clear()
    {
        count = 0;
    }

    // 获取当前堆中元素数量
    public int Count
    {
        get
        {
            return count;
        }
    }

    // 获取堆的最大容量
    public int MaxSize
    {
        get
        {
            return maxSize;
        }
    }

    // 将堆转换为字符串表示
    public override string ToString()
    {
        if (count == 0)
            return "";

        string result = items[1].ToString();

        for(int i = 2; i < count + 1; ++i)
        {
            result += ", " + items[i];
        }
        
        return "[" + result + "]";
    }

    // 交换两个元素的值
    static void Swap(ref T a, ref T b)
    {
        T tmp = a;
        a = b;
        b = tmp;
    }
}
