using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 傀儡控制器组件
/// </summary>
/// <remarks>
/// [表驱动]与玩家控制器类似,用于自动驱动怪物的巡逻,反击等功能
/// </remarks>
public class DummyController : MonoBehaviour {

    Character character;
    Iso iso;
    /// <summary>
    /// 傀儡控制器的目标,通过它把目标传递给角色组件
    /// </summary>
    Character target;
    /// <summary>
    /// 同伴,Add
    /// </summary>
    static GameObject[] siblings = new GameObject[1024];

	void Awake() {
        character = GetComponent<Character>();
        iso = GetComponent<Iso>();
    }

    void Start()
    {
        //这个事件在角色的TakeDamage挨打方法中被调用
        character.OnTakeDamage += OnTakeDamage;
        StartCoroutine(Roam());
    }
    /// <summary>
    /// 挨打发生时触发
    /// </summary>
    /// <param name="originator"></param>
    /// <param name="damage"></param>
    /// <remarks>
    /// 玩家攻击其中一个傀儡时, 傀儡还击, 并20*20范围内的其他傀儡会ADD
    /// </remarks>
    void OnTakeDamage(Character originator, int damage)
    {
        Attack(originator);
        // 获取20*20之内的所有游戏对象
        int siblingsCount = Tilemap.OverlapBox(iso.pos, new Vector2(20, 20), siblings);
        //遍历取有傀儡控制器的对象,协助一起攻击玩家
        for (int i = 0; i < siblingsCount; ++i)
        {
            DummyController sibling = siblings[i].GetComponent<DummyController>();
            if (sibling != null && sibling != this)
            {
                sibling.Attack(originator);
            }
        }
    }
    /// <summary>
    /// 巡逻
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// 携程在Start时调用,随机巡逻8*8范围内
    /// </remarks>
    IEnumerator Roam()
    {
        //等待一帧
        yield return new WaitForEndOfFrame();
        while (!this.target)
        {
            var target = iso.pos + new Vector2(Random.Range(-8f, 8f), Random.Range(-8f, 8f));
            character.GoTo(target);
            //等待1-3秒
            yield return new WaitForSeconds(Random.Range(1f, 3f));
        }
    }
    /// <summary>
    /// 傀儡的攻击方法,调携程攻击
    /// </summary>
    /// <param name="target"></param>
    void Attack(Character target)
    {
        this.target = target;
        StartCoroutine(Attack());
    }
    /// <summary>
    /// 携程攻击
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// 每个0.15-1秒攻击一次傀儡控制器的目标
    /// </remarks>
    IEnumerator Attack()
    {
        while (true)
        {
            character.Attack(target);
            yield return new WaitForSeconds(Random.Range(0.15f, 1f));
        }
    }
}
