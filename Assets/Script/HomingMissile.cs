using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 플레이어를 추적하는 유도 미사일 클래스
/// 생성 시 플레이어 위치를 계산하여 직선으로 이동합니다
/// </summary>
public class HomingMissile : NetworkBehaviour
{
    #region 변수 선언
    [Header("타겟 설정")]
    [Tooltip("추적할 플레이어 GameObject")]
    public GameObject targetPlayer;

    [Header("이동 설정")]
    [Tooltip("미사일의 이동 속도")]
    public float moveSpeed = 3f;

    // 플레이어 방향 벡터 (정규화 전)
    private Vector2 directionToPlayer;

    // 정규화된 방향 벡터 (단위 벡터)
    private Vector2 normalizedDirection;

    // 삭제 상태 추적
    private bool isDestroyed = false;
    #endregion

    /// <summary>
    /// 미사일 생성 시 플레이어를 찾고 방향을 계산합니다
    /// </summary>
    // void Start()
    public override void OnNetworkSpawn()
    {
        if ( !IsServer ) return;
        
        // "Player" 태그를 가진 GameObject를 찾아 타겟으로 설정
        targetPlayer = GameObject.FindGameObjectWithTag("Player");

        // 플레이어가 존재하지 않으면 미사일 파괴 (안전성 체크)
        if (targetPlayer == null)
        {
            Debug.LogWarning("HomingMissile: 플레이어를 찾을 수 없습니다. 미사일을 파괴합니다.");
            Destroy(gameObject);
            return;
        }

        // 미사일에서 플레이어로 향하는 방향 벡터 계산
        // 방향 벡터 = 목표 위치 - 시작 위치
        directionToPlayer = targetPlayer.transform.position - transform.position;

        // 방향 벡터를 정규화하여 크기를 1로 만듦 (방향만 유지)
        // 정규화를 통해 속도를 일정하게 유지할 수 있습니다
        normalizedDirection = directionToPlayer.normalized;
    }

    /// <summary>
    /// 매 프레임 계산된 방향으로 미사일을 이동시킵니다
    /// </summary>
    void Update()
    {
        // 서버가 아니거나 이미 파괴된 경우 함수 종료
        // IsServer: 현재 실행 환경이 서버인지 확인
        // isDestroyed: 이미 파괴 처리가 진행되었는지 확인하여 중복 실행 방지
        if (!IsServer || isDestroyed) return;

        // 정규화된 방향 * 속도 * 델타타임으로 일정한 속도 유지
        transform.Translate(normalizedDirection * moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 미사일이 카메라 뷰포트 밖으로 나갔을 때 자동 호출
    /// 메모리 누수 방지를 위해 미사일을 파괴합니다
    /// </summary>
    private void OnBecameInvisible()
    {
        // 서버가 아니거나 이미 파괴된 경우 함수 종료
        if (!IsServer || isDestroyed) return;

        // 화면 밖으로 나간 미사일 제거
        DestroyHoming();
    }

    /// <summary>
    /// 다른 2D 콜라이더와 충돌 시 호출되는 Unity 콜백
    /// </summary>
    /// <param name="collision">충돌한 콜라이더 정보</param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 충돌한 오브젝트가 "Player" 태그를 가진 경우
        if (collision.CompareTag("Player"))
        {
            // 참고: 플레이어 파괴는 PlayerController의 체력 시스템에서 처리
            // Destroy(collision.gameObject); // 현재 비활성화됨

            // 미사일 자신을 파괴 (충돌 시 미사일은 사라져야 함)
            DestroyHoming();
        }
    }

    /// <summary>
    /// 호밍 오브젝트를 네트워크에서 제거하는 메서드
    /// </summary>
    private void DestroyHoming()
    {
        // 서버가 아니거나 이미 파괴된 경우 함수 종료
        if (!IsServer || isDestroyed) return;

        // 파괴 상태를 true로 설정하여 이 메서드가 다시 실행되지 않도록 방지
        isDestroyed = true;

        // NetworkObject 컴포넌트 가져오기
        var networkObject = GetComponent<NetworkObject>();

        // NetworkObject가 null인지 확인하여 적절히 처리
        if (networkObject == null)
        {
            Debug.LogError("HomingMissile: NetworkObject가 존재하지 않습니다. Despawn을 호출할 수 없습니다.");
            Destroy(gameObject);  // 로컬에서만 오브젝트를 삭제
            return;
        }

        networkObject.Despawn();  // NetworkObject 제거
    }
}






