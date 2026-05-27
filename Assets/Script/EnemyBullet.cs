using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 적이 발사하는 일반 총알의 동작을 제어하는 클래스
/// 아래 방향으로 직선 이동하며 플레이어와 충돌 시 파괴됩니다
/// </summary>
public class EnemyBullet : NetworkBehaviour
{
    [Header("총알 설정")]
    [Tooltip("총알의 이동 속도")]
    public float moveSpeed = 3f;

    private NetworkObject networkObject;

    private void Awake()
    {
        networkObject = GetComponent<NetworkObject>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
            return;
        }
    }

    /// <summary>
    /// 매 프레임 총알을 아래 방향으로 이동시킵니다
    /// </summary>
    void Update()
    {
        if (!IsServer) return;

        // Vector3.down (0, -1, 0) 방향으로 일정한 속도로 이동
        // Time.deltaTime을 곱해 프레임 독립적인 이동 구현
        transform.Translate(Vector3.down * moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 총알이 카메라 뷰포트 밖으로 나갔을 때 자동 호출되는 Unity 콜백
    /// 메모리 누수 방지를 위해 총알을 파괴합니다
    /// </summary>
    private void OnBecameInvisible()
    {
        if (!IsServer) return;

        // 화면 밖으로 나간 총알은 더 이상 필요 없으므로 파괴
        // Destroy(gameObject);

        if(networkObject != null && networkObject.IsSpawned)
        {
            networkObject.Despawn();
        }
    }

    /// <summary>
    /// 다른 2D 콜라이더와 충돌 시 호출되는 Unity 콜백
    /// </summary>
    /// <param name="collision">충돌한 콜라이더 정보</param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        // 충돌한 오브젝트가 "Player" 태그를 가진 경우
        if (collision.CompareTag("Player"))
        {
            // 참고: 플레이어 파괴는 PlayerController의 체력 시스템에서 처리
            // Destroy(collision.gameObject); // 현재 비활성화됨

            // 총알 자신을 파괴 (충돌 시 총알은 사라져야 함)
            // Destroy(gameObject);

            if (networkObject != null && networkObject.IsSpawned)
            {
                networkObject.Despawn();
            }
        }
    }
}












