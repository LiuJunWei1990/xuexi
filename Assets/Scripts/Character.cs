using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��ɫ���
/// </summary>
public class Character : MonoBehaviour
{
    /// <summary>
    /// ��������
    /// </summary>
    [Tooltip("��ɫ������������")]
    public int directionCount = 8;
    /// <summary>
    /// �ٶ�
    /// </summary>
    public float speed = 3.5f;
    /// <summary>
    /// �����ٶ�
    /// </summary>
    public float attackSpeed = 1.0f;
    /// <summary>
    /// ������Χ
    /// </summary>
    public float useRange = 1.5f;
    /// <summary>
    /// ������Χ
    /// </summary>
    public float attackRange = 2.5f;
    /// <summary>
    /// �Ƿ����ܶ�
    /// </summary>
    [Tooltip("�Ƿ���")]
    public bool run = false;

    /// <summary>
    /// Ŀ������
    /// </summary>
    //����:����ʾ�������
    [HideInInspector]
    public GameObject target
    {
        get
        {
            ///��ȡĿ���ֶ�
            return m_Target;
        }
        set
        {
            //>>>>>>>>>>>>>>�趨Ŀ��<<<<<<<<<<<<<<<
            //���Ի�ȡ��Ҫ��ΪĿ��Ķ���Ļ������
            var usable = value.GetComponent<Usable>();
            //������������Ϊ�վʹ����ǿɻ����Ķ���,�͵�<ʹ��/����>Use����,<ʹ��>Ŀ��
            if (usable != null) Use(usable);
            //����,��Ŀ��û�л������
            else
            {
                //���Ի�ȡ��Ҫ��ΪĿ��Ķ���Ľ�ɫ���
                var targetCharacter = value.GetComponent<Character>();
                //����н�ɫ���,�ʹ����ǿɹ����Ķ���,�͵�<����>Attack����,<����>Ŀ��
                if (targetCharacter) Attack(targetCharacter);
                //��û��,��return�����κζ���
                else
                {
                    return;
                }
            }

            //�����������ִ����һ����,��Ҫ��Ŀ�긳ֵ����ǰ��ɫ��Ŀ��
            m_Target = value;
        }
    }
    /// <summary>
    /// ����(����)
    /// </summary>
    /// ����:��Inspector���������
    [HideInInspector]
    public int direction = 0;
    /// <summary>
    /// �Ⱦ���������
    /// </summary>
    Iso iso;
    /// <summary>
    /// �������
    /// </summary>
    Animator animator;

    /// <summary>
    /// ·��,����Ϊһ��װ���������
    /// </summary>
    List<Pathing.Step> path = new List<Pathing.Step>();

    /// <summary>
    /// �Ѿ��ƶ��ľ���
    /// </summary>
    float traveled = 0;

    /// <summary>
    /// Ŀ�귽��
    /// </summary>
    int targetDirection = 0;
    /// <summary>
    /// �Ƿ����ڹ���
    /// </summary>
    bool attack = false;
    /// <summary>
    /// ��������(���)
    /// </summary>
    int attackAnimation;
    /// <summary>
    /// ����ɫ��Ŀ��<�����������ɫ>
    /// </summary>
    GameObject m_Target;
    /// <summary>
    /// Ŀ��Ļ������
    /// </summary>
    Usable usable;
    /// <summary>
    /// Ŀ���ɫ���
    /// </summary>
    Character targetCharacter;

    private void Start()
    {
        //��ȡ�������
        iso = GetComponent<Iso>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// ����...(����·��)
    /// </summary>
    /// <param name="target">������Ŀ��</param>
    /// <param name="minRange">��С��Χ?</param>
    void PathTo(Vector2 target,float minRange = 0.1f)
    {
        //�ȷ�����ǰ�����߶���(�ᱣ�����һ����Ϊpath[0].pos)
        AbortMovement();
        //������������һ��,��·����ʼ������path[0].pos.
        //���·��Ϊ��,��������ԭ���Ǿ�ֹ��,��ʼ����������ﵱǰ���������iso.tilePos
        Vector2 startPos = path.Count > 0 ? path[0].pos : iso.tilePos;
        //���·��,����·��,��ʼ������startPos,Ŀ��������target,����������directionCount,��С��Χ��minRange
        path.AddRange(Pathing.BuildPath(Iso.Snap(startPos), target, directionCount,minRange));
    }

    /// <summary>
    /// ʹ��/����
    /// </summary>
    /// <param name="usable">ʹ�õ�Ŀ����</param>
    public void Use(Usable usable)
    {
        //���ڹ��������оͲ���ʹ��,ֱ�ӷ���
        if (attack) return;
        //����·��,ֹ����С��Χ�ǻ�����Χ
        PathTo(usable.GetComponent<Iso>().tilePos, useRange);
        //����·����ѵ�ǰ��������Ϊ����ʹ�õ����,��Ϊ����·�������õ�ǰ��������Ϊ��,���Է��ں���
        this.usable = usable;
    }

    /// <summary>
    /// ������(����·��,�ƶ�����Move()ʵ�ֵ�)
    /// </summary>
    /// <param name="target">Ŀ���(�������)</param>
    public void GoTo(Vector2 target)
    {
        //�������ִ�й�������,������,ֱ������
        if (attack) return;
        //����·��
        PathTo(target);
    }

    /// <summary>
    /// ˲��
    /// </summary>
    /// <param name="target">Ŀ���</param>
    public void Teleport(Vector2 target)
    {
        if (attack) return;
        //�ж�Ŀ�������Ƿ��ͨ��
        if (Tilemap.instance[target])
        {
            //��ͨ�о�ֱ��˲��
            iso.pos = target;
            iso.tilePos = target;
        }
        else
        {
            //����ͨ�оͻ�·��,׼��˲�Ƶ�����Ѱ·�Ĺ����Ŀ������
            var pathToTarget = Pathing.BuildPath(Iso.Snap(iso.tilePos), target,directionCount);
            //·����Ϊ��,Ϊ�վͷ���
            if (pathToTarget.Count == 0) return;
            //����-1,����·�������е����һ��·��,˲�ƹ�ȥ
            iso.pos = pathToTarget[pathToTarget.Count - 1].pos;
            iso.tilePos = iso.pos;
        }
        //��Ȼ��˲��,�Ͱ�·�����
        path.Clear();
        //���߾���Ҳ����
        traveled = 0;
    }

    /// <summary>
    /// �������߶���
    /// </summary>
    private void AbortMovement()
    {
        //�ѹ���·�������б��������
        m_Target = null;
        usable = null;
        targetCharacter = null;

        //���·����û����,�����굱ǰ��һ��(һ֡�����߾���)
        if(path.Count > 0)
        {
            //��·���ĵ�һ����
            var firstStep = path[0];
            //���·��
            path.Clear();
            //�ѵ�һ������ӵ�·����ͷ
            path.Add(firstStep);
        }

        //����,��·���Ѿ�������,�Ͱ�·�����
        else
        {
            //·�����
            path.Clear();
            //�ƶ�����Ҳ����
            traveled = 0;
        }
    }

    private void Update()
    {
        //��������վ����������
        Iso.DebugDrawTile(iso.tilePos);
        //��·����
        Pathing.DebugDrawPath(path);
        //�ƶ���ɫ
        Move();

        //>>>>>>>>>>>>>��ɫ��Ϊ����<<<<<<<<<<<<<<
        //��Ϊ��������ʽ����,����<����>,<����>,<ʹ��>�ȷ����и�usable,targetCharacter�ֶθ�ֵ,������Ĵ����⵽�ֶβ�Ϊ��ʱ,�ͻ�ִ����Ӧ����Ϊ
        //��Ѱ·����
        if (path.Count == 0)
        {
            //���Ŀ���л������
            if (usable)
            {
                //���Ŀ�������ͽ�ɫ���������С�ڵ��ڻ�����Χ,��ִ�л���
                if (Vector2.Distance(usable.GetComponent<Iso>().tilePos, iso.tilePos) <= useRange) usable.Use();
                //ִ����Ϻ�,��Ŀ���ÿ�
                usable = null;
            }
            //���Ŀ���н�ɫ���
            if (targetCharacter)
            {
                //���Ŀ�������ͽ�ɫ���������С�ڵ��ڹ�����Χ,��ִ�й���
                Vector2 target = targetCharacter.GetComponent<Iso>().tilePos;
                if(Vector2.Distance(target,iso.tilePos) <= attackRange)
                {
                    //״̬�޸�Ϊ������
                    attack = true;
                    //�����ֵ1-3����ѡ�񹥻�����
                    attackAnimation = Random.Range(1, 3);
                    //��ȡ��Ŀ��ķ���ı��
                    direction = Iso.Direction(iso.tilePos, target, directionCount);
                }
                //ִ����Ϻ�,��Ŀ���ÿ�
                targetCharacter = null;
            }
            //���ж���ִ����Ϻ�,��Ŀ���ÿ�
            m_Target = null;
        }
        //���¸�������
        UpdateAnimation();
    }
    /// <summary>
    /// �ƶ���ɫ
    /// </summary>
    private void Move()
    {
        //��֧1.·��Ϊ�վͷ���
        if (path.Count == 0) return;


        //��ȡ��һ��·��
        Vector2 step = path[0].direction;
        //�����һ·���ĳ���;
        float stepLen = step.magnitude;

        //���㵱ǰ֡���ƶ�����
        float distance = speed * Time.deltaTime;

        //��֧2.����һ֡Ҫ������һ·����,���߾�����ϵ�ǰ֡���볬����һ·������
        while (traveled + distance >= stepLen)
        {
            //������һ��·��ľ���
            float firstPart = stepLen - traveled;
            //��ɫ�ƶ�����һ·��,��������ǰѵ�һ·����һ�����Ծ���.��һ�����ǰ�·����ɷ���,���Գ��Ⱥ�ͻ���һ��·��.
            iso.pos += step.normalized * firstPart;
            //��ǰ֡����,Ҫ��ȥ�����ɫ�ƶ����ľ���.
            distance -= firstPart;
            //�����Ѿ��ƶ��˵ľ���(������ΪʲôҪ��ȥ��һ��·���ĳ���)
            traveled += firstPart - stepLen;
            //���½�ɫ���������λ��
            iso.tilePos += step;
            //��һ��·���Ѿ�������,ɾ��
            path.RemoveAt(0);
            //·����Ϊ�վͼ�����ȡ�����·��
            if (path.Count > 0)
            {
                step = path[0].direction;
            }
        }
        //��֧3.·����Ϊ�վͿ���
        if (path.Count > 0)
        {
            traveled += distance;
            iso.pos += step.normalized * distance;
        }
        //��֧4.�������ִ����֮��·������,תΪ����
        if (path.Count == 0)
        {
            //����ȡ��
            iso.pos.x = Mathf.Round(iso.pos.x);
            iso.pos.y = Mathf.Round(iso.pos.y);
            //�ƶ��������
            traveled = 0;
        }
    }

    /// <summary>
    /// ���¶���
    /// </summary>
    private void UpdateAnimation()
    {
        //�Ƿ�ά�ֶ�����ʱ�����,�������²��ŵ�ǰ����ʱ,����֮ǰ�Ľ��ȼ�������
        bool preserveTime = false;
        //��������
        string animation;
        //�������������ʼֵ
        animator.speed = 1.0f;
        //������ڹ���
        if (attack)
        {
            //���������Ƹ�ֵ
            animation = "Attack" + attackAnimation;
            //�������ٶȸ�ֵ
            animator.speed = attackSpeed;
        }

        //û��·�����Ǵ���״̬
        else if(path.Count == 0)
        {
            //����������ֵ
            animation = "Idle";
            //�˶�����Ҫά��,ʱ�����
            preserveTime = true;
        }
        //�����������
        else
        {
            //ͨ�����ܱ�ǩ�ж����ܻ�����
            animation = run ? "Run" : "Walk";
            //�˶�����Ҫά��,ʱ�����
            preserveTime = true;
            //Ŀ�귽����·���ĵ�һ���ķ���
            targetDirection = path[0].directionIndex;
        }

        //������ﳯ���Ŀ�귽��һ��,��ת��
        if (!attack && direction != targetDirection)
        {

            //���㵱ǰ�����Ŀ�귽��ļн�,��ȡ�нǵ�����,��������˳ʱ��,����������ʱ��
            int diff = (int)Mathf.Sign(Tools.ShortestDelta(direction, targetDirection, directionCount));
            //ƽ���ĸ��µ�ǰ����,ȷ������ֵ�� [0, directionCount - 1] ��Χ��
            direction = (direction + diff + directionCount) % directionCount;
        }
        //�������Ƽ��Ϸ�����ַ���
        animation += "_" + direction.ToString();
        //GetCurrentAnimatorStateInfo(0),���ض���״̬����Ϣ,0�ǲ������,0�����Ĭ�ϲ�,�ǵ�ǰ������״̬��Ϣ
        //IsName()�жϵ�ǰ�������Ƿ����β���ͬ,�����β���ʽת��Ϊ������
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(animation))
        {
            //���Ҫ���Ŷ���,�뵱ǰ������ͬ�Ͳ���
            //�����ά�ֶ������ȵ�,��������,�ᰴ����ǰ�Ķ������Ȳ���
            //�β�1:������,�β�2:�������(0���ǵ�ǰ����),�β�3:�����Ĺ�һ��ʱ��(���ǵ�ǰ���Ž�����)
            if (preserveTime) animator.Play(animation, 0, animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            //����Ҫά�ֲ��Ž��ȵĶ���,ֱ�Ӿʹ�ͷ��ʼ��
            else animator.Play(animation);
        }
    }

    /// <summary>
    /// �۲�(ת��)
    /// </summary>
    /// <param name="target">���</param>
    public void LookAt(Vector3 target)
    {
        //Ŀ�귽���������-Ŀ��ķ���
        targetDirection = Iso.Direction(iso.tilePos, target,directionCount);
    }

    /// <summary>
    /// ����
    /// </summary>
    public void Attack()
    {
        //������ڹ�����,���ﳯ��ʱĿ��,·��Ϊ����ô�Ϳ�ʼ������
        if (!attack && direction == targetDirection && path.Count == 0)
        {
            //���빥��״̬
            attack = true;
            //��һ����������,���
            attackAnimation = Random.Range(1, 3);
        }
    }

    /// <summary>
    /// ����(����,��Ŀ���)
    /// </summary>
    /// <param name="targetCharacter">Ŀ��</param>
    public void Attack(Character targetCharacter)
    {
        //������ڹ����ͷ���
        if (attack) return;
        //��ȡĿ���Iso���
        Iso targetIso = targetCharacter.GetComponent<Iso>();
        //����Ŀ���,�Թ�����Χ��Ϊֹ����Χ
        PathTo(targetIso.tilePos, attackRange);
        //��ȡĿ��Ľ�ɫ���
        this.targetCharacter = targetCharacter;
    }

    /// <summary>
    /// �ڶ������ʱִ�еķ���(����ϵͳ��api��,�����Լ�ȡ������)
    /// </summary>
    void OnAnimationFinish()
    {
        //�Ƿ񹥻�״̬,�ڹ���״̬���÷�
        if (attack) attack = false;
    }

    void OnAttack1Finish()
    {

    }
    void OnAttack2Finish()
    {

    }
}
