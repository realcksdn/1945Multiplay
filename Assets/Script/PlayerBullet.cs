using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 플레이어가 발사하는 총알의 동작을 제어하는 클래스
/// 위 방향으로 이동하며 적과 충돌 시 데미지를 주고 파괴됩니다
/// </summary>
public class PlayerBullet : NetworkBehaviour
{
    #region 변수 선언
    [Header("총알 설정")]
    [Tooltip("총알의 이동 속도")]
    public float moveSpeed = 4.0f;

    [Tooltip("총알의 공격력")]
    public int attackPower = 10;

    [Header("이펙트 설정")]
    [Tooltip("적 피격 시 생성되는 이펙트 프리팹")]
    public GameObject hitEffectPrefab;
    #endregion

    /// <summary>
    /// 매 프레임 총알을 위 방향으로 이동시킵니다
    /// </summary>
    void Update()
    {
        if (!IsServer) return;  // 서버가 아니면 실행하지 않음

        // Vector2.up (0, 1) 위쪽 방향으로 일정한 속도로 이동
        // Time.deltaTime을 곱해 프레임 독립적인 이동 구현
        transform.Translate(Vector2.up * moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 다른 2D 콜라이더와 충돌 시 호출되는 Unity 콜백
    /// 적, 보스 파츠, 보스에게 데미지를 주고 이펙트를 생성합니다
    /// </summary>
    /// <param name="collision">충돌한 콜라이더 정보</param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;  // 서버가 아니면 실행하지 않음

        // 일반 적과 충돌한 경우 (태그: "Enemy")
        if (collision.CompareTag("Enemy"))
        {
            // 적에게 데미지 적용
            EnemyShip enemyShip = collision.gameObject.GetComponent<EnemyShip>();

            if (enemyShip != null)        // EnemyShip 컴포넌트가 존재하면
            {
                // EnemyShip의 TakeDamage 함수 호출, attackPower(10)만큼 데미지 적용
                enemyShip.TakeDamage(attackPower); 
            }

            // 충돌 위치에 피격 이펙트 생성
            if (hitEffectPrefab != null)  // 이펙트 프리팹이 할당되어 있으면
            {
                CreateEffect();           // 피격 이펙트 생성 함수 호출
            }

        }

        // 보스 파츠와 충돌한 경우
        else if (collision.CompareTag("BossPart"))
        {
            // 충돌 위치에 피격 이펙트 생성
            if (hitEffectPrefab != null)  // 이펙트 프리팹이 할당되어 있으면
            {
                CreateEffect();           // 피격 이펙트 생성 함수 호출
            }

            // 보스 파츠 파괴 (일반 총알로도 파츠 파괴 가능)
            Destroy(collision.gameObject);
        }

        // 보스 본체와 충돌한 경우
        else if (collision.CompareTag("Boss"))
        {
            // 충돌 위치에 피격 이펙트 생성
            if (hitEffectPrefab != null)  // 이펙트 프리팹이 할당되어 있으면
            {
                CreateEffect();           // 피격 이펙트 생성 함수 호출
            }

            // 참고: 보스 본체는 데미지를 받지만 일반 총알로는 파괴되지 않음
            // 보스 체력 시스템이 구현되면 여기에 데미지 로직 추가 필요
        }
    }

    private void CreateEffect() // 피격 이펙트를 생성하고 네트워크에 스폰하는 함수
    {
        // 이펙트 오브젝트 생성
        // Instantiate(): Unity 기본 함수, 프리팹을 복제하여 씬에 생성
        // hitEffectPrefab: 복제할 프리팹
        // transform.position: 현재 총알의 위치 (충돌 위치)
        // Quaternion.identity: 회전 없음 (0, 0, 0 회전)
        GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        // effect.GetComponent<NetworkObject>(): 생성된 이펙트에서 NetworkObject 컴포넌트 가져오기
        var netObj = effect.GetComponent<NetworkObject>(); // var는 타입 자동 추론, 여기서는 NetworkObject 타입

        if (netObj != null) // NetworkObject 컴포넌트가 존재하면
        {
            effect.SetActive(true); // 이펙트 오브젝트를 활성화 (기본적으로 활성화되어 있지만 명시적 활성화)

            // netObj.Spawn(true): 네트워크에 이펙트를 스폰
            // true: 이펙트 오브젝트에 부모 오브젝트의 네트워크 소유권을 상속 (부모의 Owner가 이펙트도 소유)
            // 서버에서 스폰하면 모든 클라이언트에서도 이펙트가 보임 (자동 동기화)
            netObj.Spawn(true); // 네트워크 오브젝트로 스폰하여 모든 플레이어에게 이펙트 표시
        }
    }
}







