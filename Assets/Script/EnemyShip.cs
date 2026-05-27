using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 적 우주선의 동작을 제어하는 클래스
/// 체력, 이동, 공격, 아이템 드롭 기능을 구현합니다
/// 태그: "Enemy" (필수)
/// </summary>
public class EnemyShip : NetworkBehaviour
{
    #region 변수 선언
    [Header("체력 설정")]
    [Tooltip("적의 최대 체력")]
    public int maxHealth = 100;

    [Header("이동 설정")]
    [Tooltip("적의 이동 속도")]
    public float moveSpeed = 3f;

    [Header("공격 설정")]
    [Tooltip("총알 발사 간격 (초 단위)")]
    public float fireDelay = 1f;

    [Tooltip("첫 번째 총알 발사 위치")]
    public Transform firstFirePoint;

    [Tooltip("두 번째 총알 발사 위치")]
    public Transform secondFirePoint;

    [Tooltip("발사할 총알 프리팹")]
    public GameObject bulletPrefab;

    [Header("아이템 설정")]
    [Tooltip("처치 시 드롭할 아이템 프리팹")]
    public GameObject itemDropPrefab = null;

    // 이미 죽은 상태인지 확인하는 플래그 (중복 처리 방지)
    private bool isDead = false;
    #endregion

    #region 네트워크 변수 추가
    private NetworkVariable<int> HP = new NetworkVariable<int>();
    private bool isDestroyed = false;  // 삭제 상태 추적
    #endregion

    // NetworkBehaviour의 가상 함수를 재정의,
    // 네트워크 오브젝트가 네트워크에 스폰될 때 자동으로 호출되는 생명주기 함수
    public override void OnNetworkSpawn()
    {
        if (!IsServer)          // 클라이언트인 경우
        {
            enabled = false;    // 클라이언트에서는 적 로직 실행 안 함
            return;             // 함수를 즉시 종료하여 아래 코드가 실행되지 않도록 함
        }

        // 여기서부터는 서버인 경우에만 실행됨
        HP.Value = maxHealth;   // NetworkVariable HP의 값을 maxHealth로 초기화,
                                // 모든 클라이언트에 자동 동기화됨

        // Invoke()로 fireDelay초 후에 FireBullets 함수를 호출하여 총알 발사 시작
        // 이후 반복 발사됨
        Invoke("FireBullets", fireDelay); 
    }

    /// <summary>
    /// 두 개의 발사 위치에서 총알을 발사하고 다음 발사 예약
    /// </summary>
    void FireBullets()
    {
        // 클라이언트 이거나 이미 파괴되었으면 함수 즉시 종료, 서버에서만 실행하고 파괴된 적은 총알 발사 안 함
        if (!IsServer || isDestroyed)  return;

        // 발사 위치와 총알 프리팹이 할당되어 있는지 확인
        if (bulletPrefab != null && firstFirePoint != null && secondFirePoint != null)
        {
            // 첫 번째 발사 위치에서 총알 생성
            GameObject firstBullect = Instantiate(bulletPrefab, firstFirePoint.position, Quaternion.identity);

            // 두 번째 발사 위치에서 총알 생성
            GameObject secondBullect = Instantiate(bulletPrefab, secondFirePoint.position, Quaternion.identity);

            // 첫 번째 총알의 NetworkObject 컴포넌트를 가져와서 Spawn() 호출,
            // 네트워크에 스폰하여 모든 클라이언트에서 보이게 함
            firstBullect.GetComponent<NetworkObject>().Spawn();

            // 두 번째 총알의 NetworkObject 컴포넌트를 가져와서 Spawn() 호출,
            // 네트워크에 스폰하여 모든 클라이언트에서 보이게 함
            secondBullect.GetComponent<NetworkObject>().Spawn();
        }

        // fireDelay 초 후에 다시 FireBullets 함수 호출 (반복 발사)
        Invoke("FireBullets", fireDelay);
    }

    /// <summary>
    /// 매 프레임 적을 아래 방향으로 이동시킵니다
    /// </summary>
    void Update()
    {
        // 클라이언트 이거나 이미 파괴되었으면 함수 즉시 종료,
        // 서버에서만 실행
        if (!IsServer || isDestroyed) return;

        // Vector2.down (0, -1) 방향으로 일정한 속도로 이동
        // Time.deltaTime을 곱해 프레임 독립적인 이동 구현
        transform.Translate(Vector2.down * moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 적이 카메라 뷰포트 밖으로 나갔을 때 자동 호출되는 Unity 콜백
    /// 메모리 누수 방지를 위해 적을 파괴합니다
    /// </summary>
    private void OnBecameInvisible()
    {
        // 클라이언트 이거나 이미 파괴되었으면 함수 즉시 종료, 서버에서만 실행
        if (!IsServer || isDestroyed) return; 

        // 네트워크 게임에서는 서버만 오브젝트 제거 결정
        if (NetworkManager.Singleton == null)
        {
            // 네트워크가 초기화되지 않은 경우 (싱글플레이어)
            Destroy(gameObject);
            return;
        }

        // 네트워크 게임인 경우
        if (NetworkManager.Singleton.IsServer)
        {
            // 서버에서만 제거 수행
            DestoryEnemy();
        }
        // 클라이언트는 아무것도 하지 않음 (서버가 결정)
    }

    /// <summary>
    /// 적 파괴하는 함수
    /// </summary>
    public void DestoryEnemy()
    {
        // NetworkObject가 null인지 확인
        if (NetworkObject == null)
        {
            Debug.LogWarning("NetworkObject가 null입니다. 이미 제거된 오브젝트일 수 있습니다.");
            return;
        }

        // 이미 Despawn 중인지 확인 (중복 방지)
        if (!NetworkObject.IsSpawned)
        {
            Debug.Log("이미 Despawn된 오브젝트입니다.");
            return;
        }

        // 서버(호스트)에서만 Despawn 수행
        if (IsServer)
        {
            // 안전하게 Despawn 수행
            try
            {
                NetworkObject.Despawn(true);  // true = 오브젝트 destroy
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Despawn 중 오류 발생: {e.Message}");
                // 네트워크 오브젝트가 아닌 경우 일반 Destroy 시도
                Destroy(gameObject);
            }
        }
        else if (IsClient)
        {
            // 클라이언트는 서버에 제거 요청을 보내야 함
            RequestDespawnServerRpc();
        }
        else
        {
            // 네트워크가 연결되지 않은 상태 (싱글플레이어 등)
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 클라이언트가 서버에 제거를 요청하는 RPC
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void RequestDespawnServerRpc()
    {
        // 서버에서 안전하게 제거
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }

    /// <summary>
    /// 적 처치 시 아이템을 드롭하는 메서드
    /// </summary>
    public void DropItem()
    {
        if(!IsServer || isDestroyed) return;

        // itemDropPrefab이 할당되어 있는 경우에만 아이템 생성
        if (itemDropPrefab != null)
        {
            // 적이 파괴되는 위치에 아이템 생성
            GameObject itemObj =  Instantiate(itemDropPrefab, transform.position, Quaternion.identity);
            itemObj.GetComponent<NetworkObject>().Spawn();
        }
    }

    /// <summary>
    /// 적이 데미지를 받는 메서드
    /// 플레이어의 총알이나 레이저에 맞았을 때 호출됩니다
    /// </summary>
    /// <param name="damage">받을 데미지 양</param>
    public void TakeDamage(int damage)
    {
        // 이미 죽었으면 추가 데미지 무시
        if (isDead) return;

        HP.Value -= damage;

        if (HP.Value <= 0)
        {
            // 즉시 죽음 상태로 설정 (중복 방지!)
            isDead = true;

            CancelInvoke("FireBullets");
            DropItem(); // 딱 1번만 실행됨!
            Destroy(gameObject);
        }
    }

    // HP 확인용
    public int GetHP()
    {
        return HP.Value;
    }
}















