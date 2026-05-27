using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 플레이어의 레이저 빔 공격을 제어하는 클래스
/// 플레이어 위치를 추적하며 충돌한 적에게 지속적으로 데미지를 줍니다
/// </summary>
public class LaserBeam : NetworkBehaviour
{
    [Header("이펙트 설정")]
    [Tooltip("적 피격 시 생성되는 이펙트 프리팹")]
    public GameObject hitEffectPrefab;

    // 플레이어의 발사 위치를 저장하는 Transform (레이저가 플레이어를 따라다님)
    private Transform playerFirePoint;

    // 레이저의 공격력 (충돌할 때마다 1씩 증가하는 누적 데미지)
    private int laserDamage = 100;

    // 마지막 데미지 시간
    private float lastDamageTime = 0f;
    private float damageInterval = 0.1f;

    // 부모 PlayerController 참조 (서버 체크용)
    private PlayerController playerController;

    /// <summary>
    /// 네트워크 스폰 시 호출되는 Unity Netcode 콜백
    /// Start() 대신 OnNetworkSpawn()을 사용하여 네트워크 초기화를 안전하게 처리
    /// </summary>
    public override void OnNetworkSpawn()
    {
        // base.OnNetworkSpawn() 호출 필수 (부모 클래스의 초기화 로직 실행)
        base.OnNetworkSpawn();

        // 부모가 플레이어인지 확인
        if (transform.parent == null)
        {
            Debug.LogWarning("LaserBeam: 부모 오브젝트가 없습니다! " +
                             "firePoint의 자식으로 설정되어야 합니다.");
        }
    }

    /// <summary>
    /// 초기화: 부모 PlayerController 찾기
    /// </summary>
    void Start()
    {
        // 부모 오브젝트에서 PlayerController 컴포넌트 가져오기
        playerController = GetComponentInParent<PlayerController>();

        if (playerController == null)
        {
            Debug.LogError("LaserBeam: 부모에 PlayerController가 없습니다!");
        }

        Debug.Log("LaserBeam: 초기화 완료");
    }

    /// <summary>
    /// 충돌 중일 때 지속적으로 데미지 (OnTriggerStay2D)
    /// </summary>
    /// <param name="collision">충돌한 콜라이더 정보</param>
    private void OnTriggerStay2D(Collider2D collision)
    {
        // 서버에서만 충돌 처리 (PlayerController가 서버인지 확인)
        if (playerController == null || !playerController.IsServer)
            return;

        // 데미지 간격 체크 (0.1초마다 데미지)
        if (Time.time - lastDamageTime < damageInterval)
            return;

        // 일반 적과 충돌한 경우 (태그: "Enemy")
        if (collision.CompareTag("Enemy"))
        {
            // 적에게 데미지 적용 (매번 데미지가 1씩 증가)
            EnemyShip enemyShip = collision.gameObject.GetComponent<EnemyShip>();
            if (enemyShip != null)
            {
                enemyShip.TakeDamage(laserDamage);
                lastDamageTime = Time.time;  // 마지막 데미지 시간 업데이트
            }

            // 충돌 위치에 피격 이펙트 생성
            if (hitEffectPrefab != null)
            {
                CreateEffect(collision.gameObject.transform.position);
            }
        }
        // 보스 파츠와 충돌
        else if (collision.CompareTag("BossPart"))
        {
            // 피격 이펙트 생성
            if (hitEffectPrefab != null)
            {
                CreateEffect(collision.gameObject.transform.position);
            }

            // 보스 파츠 파괴 (파츠는 레이저 한 번에 파괴됨)
            Destroy(collision.gameObject);
        }
        // 보스 본체와 충돌
        else if (collision.CompareTag("Boss"))
        {
            // 피격 이펙트 생성
            if (hitEffectPrefab != null)
            {
                CreateEffect(collision.gameObject.transform.position);
            }

            // 보스 데미지 로직은 Boss 스크립트에서 처리
        }
    }

    /// <summary>
    /// 피격 이펙트 생성 및 네트워크 스폰
    /// </summary>
    /// <param name="position">이펙트 생성 위치</param>
    private void CreateEffect(Vector3 position)
    {
        // 이펙트 오브젝트 생성
        GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);

        // NetworkObject 컴포넌트 가져오기
        var netObj = effect.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            effect.SetActive(true);
            // 네트워크에 스폰 (모든 클라이언트에서 이펙트 표시)
            netObj.Spawn(true);
        }
    }
}




