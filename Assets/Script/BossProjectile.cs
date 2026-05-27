using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 보스가 발사하는 발사체(미사일)의 동작을 제어하는 클래스
/// 다양한 방향으로 이동 가능하며, 플레이어와 충돌 시 파괴됩니다
/// </summary>
public class BossProjectile : NetworkBehaviour
{
    [Header("발사체 설정")]
    [Tooltip("발사체의 이동 속도")]
    public float moveSpeed = 3f;

    private NetworkObject networkObject;

    // 발사체의 이동 방향 벡터 (기본값: 아래 방향)
    private Vector2 movementDirection = Vector2.down;

    /// <summary>
    /// 초기화 함수 (현재 미사용)
    /// </summary>
    // void Start()
    private void Awake()
    {
        networkObject = GetComponent<NetworkObject>();
    }

    /// <summary>
    /// 매 프레임 발사체를 지정된 방향으로 이동
    /// </summary>
    void Update()
    {
        if (!IsServer) return;

        // 이동 방향 * 속도 * 델타타임으로 프레임 독립적 이동 구현
        transform.Translate(movementDirection * moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 발사체의 이동 방향을 설정하는 메서드
    /// BossController의 원형 탄막 패턴에서 호출됩니다
    /// </summary>
    /// <param name="direction">이동 방향 벡터 (정규화된 값 권장)</param>
    public void SetDirection(Vector2 direction)
    {
        // 전달받은 방향 벡터를 발사체의 이동 방향으로 설정
        movementDirection = direction;
    }

    /// <summary>
    /// 발사체가 카메라 뷰포트 밖으로 나갔을 때 자동 호출되는 Unity 콜백
    /// 메모리 누수 방지를 위해 발사체를 파괴합니다
    /// </summary>
    private void OnBecameInvisible()
    {
        // 화면 밖으로 나간 발사체는 더 이상 필요 없으므로 파괴
        //Destroy(gameObject);
        if (IsServer)
        {
            // NetworkObject 컴포넌트가 존재하고 현재 네트워크에 스폰된 상태인지 확인
            // null 체크와 IsSpawned 체크로 이중 삭제 방지
            if (networkObject != null && networkObject.IsSpawned)//true)
            {
                
                networkObject.Despawn();
            }
            {
                networkObject.Despawn();
            }
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
            // 참고: 플레이어 파괴는 PlayerController에서 체력 시스템으로 처리
            // Destroy(collision.gameObject); // 현재 비활성화됨

            // NetworkObject 컴포넌트가 존재하고 현재 네트워크에 스폰된 상태인지 확인
            // null 체크와 IsSpawned 체크로 이중 삭제 방지
            if (networkObject != null && networkObject.IsSpawned)
            {
                // 발사체를 네트워크에서 제거 (모든 클라이언트에서 동시에 제거됨)
                // Despawn()은 오브젝트를 오브젝트 풀로 반환하여 재사용 가능하게 함
                // 충돌 후 발사체는 사라져야 하므로 네트워크에서 제거
                networkObject.Despawn();
            }
        }
    }
}

