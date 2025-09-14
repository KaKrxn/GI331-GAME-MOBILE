using UnityEngine;

/// ตัวกลางระหว่างวัตถุกับ PoolManager
public class PoolAgent : MonoBehaviour
{
    public string poolId;              // พูลของวัตถุนี้ (ตั้งอัตโนมัติตอน Acquire)
    public Transform endPoint;         // จุดจบทาง (ตั้งจาก PoolManager)
    public PoolManager owner;          // อ้างผู้จัดการพูล

    public void Configure(PoolManager mgr, string id, Transform end)
    {
        owner = mgr;
        poolId = id;
        endPoint = end;
    }

    /// เรียกตอน “ถึง EndPoint” เพื่อคืนของเข้า Pool
    public void ReturnToPool()
    {
        if (owner) owner.Return(gameObject);
        else gameObject.SetActive(false); // fallback
    }
}
