using System;
/// <summary>
/// 二叉堆(一种强化了找最小数的容器)
/// </summary>
/// <typeparam name="T"></typeparam>
/// <remarks>
/// 叉状树结构的容器,可以迅速找出最小值放在堆顶,添加或者摘取元素都会重新排序,以保证二叉堆的结构
/// </remarks>
public class BinaryHeap<T> where T: IComparable<T>
{
    /// <summary>
    /// 堆本体
    /// </summary>
    T[] items;
    /// <summary>
    /// 堆中元素个数[字段]
    /// </summary>
    int count = 0;
    /// <summary>
    /// 堆的最大容量[字段]
    /// </summary>
    int maxSize;

    public BinaryHeap(int maxSize)
    {
        //原作者注:为了简化处理,堆的第一个元素不使用,所以容量要+1
        items = new T[maxSize + 1];
        this.maxSize = maxSize;
    }
    /// <summary>
    /// 加入元素
    /// </summary>
    /// <param name="item"></param>
    /// <remarks>
    /// 加入新元素会按大小放在容器对应的位置,而不是末尾
    /// </remarks>
    public void Add(T item)
    {
        ++count;
        int index = count;
        items[index] = item;
        //因为是二叉树,所以父节点的索引是当前节点的索引除以2
        int parent = index / 2;
        //拓扑,比大小一路到堆顶或者父更小中止
        while (index > 1 && items[parent].CompareTo(items[index]) > 0)
        {
            Swap(ref items[parent], ref items[index]);
            index = parent;
            parent = index / 2;
        }
    }
    /// <summary>
    /// 摘取堆顶
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception(溢出)"></exception>
    /// <remarks>
    /// 摘走堆顶后,重新排序
    /// </remarks>
    public T Take()
    {
        //摘走第一个,最后一个放第一
        T result = items[1];
        items[1] = items[count];
        --count;

        //赋值计数器,索引,子节点
        int iterCount = 0;
        int index = 1;
        int child = index * 2;
        //循环比大小,直到现在的第一个值到达了应该在的位置
        while (child <= count)
        {
            if (iterCount++ > 1000)
                throw new Exception("溢出");
            //节点的三个点,父,左,右,找最小的
            int left = child;
            int right = child + 1;
            int smallest = index;
            //CompareTo,前大后小为1,反之为0
            if (items[smallest].CompareTo(items[left]) > 0)
                smallest = left;
            if (right <= count && items[smallest].CompareTo(items[right]) > 0)
                smallest = right;
            //例如[3]比[1]小,交换值,index指向[3],child指向[6]
            if (index != smallest)
            {
                Swap(ref items[index], ref items[smallest]);
                index = smallest;
                child = index * 2;
            }
            else
            {
                break;
            }
        }
        //值在第一步就定好了,中间的代码都在重新排序,和它没关系
        return result;
    }
    /// <summary>
    /// 清空
    /// </summary>
    /// <remarks>
    /// 只是把count置0,并不清除主体,因为Add的时候会覆盖掉
    /// </remarks>
    public void Clear()
    {
        count = 0;
    }
    /// <summary>
    /// 堆中元素个数[属性]
    /// </summary>
    /// <remarks>
    /// count[字段]的壳子
    /// </remarks>
    public int Count
    {
        get
        {
            return count;
        }
    }
    /// <summary>
    /// 堆的最大容量[属性]
    /// </summary>
    /// <remarks>
    /// maxSize[字段]的壳子
    /// </remarks>
    public int MaxSize
    {
        get
        {
            return maxSize;
        }
    }
    /// <summary>
    /// 本体所有元素输出字符串
    /// </summary>
    /// <returns></returns>
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
    /// <summary>
    /// 交换两个形参的值
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    static void Swap(ref T a, ref T b)
    {
        T tmp = a;
        a = b;
        b = tmp;
    }
}
