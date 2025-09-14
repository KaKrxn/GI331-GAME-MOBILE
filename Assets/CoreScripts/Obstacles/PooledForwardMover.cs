using UnityEngine;

[RequireComponent(typeof(PoolAgent))]
public class ForwardMover : MonoBehaviour
{
    [Header("Move")]
    public float speed = 6f;
    public bool usePhysics = false;

    [Header("Reach Detection")]
    public float reachThreshold = 0.1f;  // ระยะถือว่า "ถึง" EndPoint

    Rigidbody rb;
    PoolAgent agent;

    void Awake()
    {
        agent = GetComponent<PoolAgent>();
        if (usePhysics) rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (usePhysics) return;
        Move(Time.deltaTime);
        CheckReach();
    }

    void FixedUpdate()
    {
        if (!usePhysics) return;
        Move(Time.fixedDeltaTime);
        CheckReach();
    }

    void Move(float dt)
    {
        Vector3 delta = transform.forward * speed * dt;
        if (usePhysics && rb) rb.MovePosition(rb.position + delta);
        else transform.position += delta;
    }

    void CheckReach()
    {
        if (!agent || !agent.endPoint) return;
        if (Vector3.Distance(transform.position, agent.endPoint.position) <= reachThreshold)
        {
            agent.ReturnToPool();
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var a = GetComponent<PoolAgent>();
        if (a && a.endPoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, a.endPoint.position);
            Gizmos.DrawWireSphere(a.endPoint.position, 0.2f);
        }
    }
#endif
}
