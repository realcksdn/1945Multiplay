using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 보스 헤드 무기 시스템
/// 애니메이션 이벤트에서 호출되어 다양한 방향으로 발사체를 발사합니다
/// </summary>
public class BossWeapon : NetworkBehaviour
{
    [Header("발사체 설정")]
    [Tooltip("보스 헤드가 발사하는 미사일 프리팹")]
    [SerializeField]
    private GameObject bossProjectilePrefab;

    /// <summary>
    /// [호환성] 이전 애니메이션 이벤트용
    /// 오른쪽 아래 대각선(45도) 방향으로 발사
    /// </summary>
    public void RightDownLaunch()
    {
        // 오른쪽 아래 대각선 방향 (1, -1)을 정규화하여 전달
        // 정규화: 벡터의 크기를 1로 만들어 방향만 유지
        FireProjectile(new Vector2(1, -1).normalized);
    }

    /// <summary>
    /// [호환성] 이전 애니메이션 이벤트용
    /// 수직 아래 방향으로 발사
    /// </summary>
    public void DownLaunch()
    {
        FireProjectile(new Vector2(0, -1).normalized);
    }

    /// <summary>
    /// [호환성] 이전 애니메이션 이벤트용
    /// 왼쪽 아래 대각선(135도) 방향으로 발사
    /// </summary>
    public void LeftDownLaunch()
    {
        // 왼쪽 아래 대각선 방향 (-1, -1)을 정규화하여 전달
        FireProjectile(new Vector2(-1, -1).normalized);
    }

    // ========================================
    // 공통 발사 로직
    // ========================================

    /// <summary>
    /// 발사체를 생성하고 방향을 설정하는 공통 메서드
    /// </summary>
    /// <param name="direction">발사 방향 벡터</param>
    private void FireProjectile(Vector2 direction)
    {
        // 프리팹이 할당되지 않았으면 오류 방지
        if (!IsServer)
        {
            Debug.LogError("BossWeapon: bossProjectilePrefab이 할당되지 않았습니다!");
            return;
        }

        // 방향 벡터가 정규화되었는지 확인 (선택적 안전 체크)
        if (Mathf.Abs(direction.magnitude - 1f) > 0.01f)
        {
            Debug.LogWarning($"BossWeapon: 방향 벡터가 정규화되지 않았습니다. 크기:" +
                             $" {direction.magnitude}. 자동으로 정규화합니다.");
            direction.Normalize();  // 자동 정규화
        }

        // 현재 보스 무기의 위치에 발사체 인스턴스 생성
        // transform.position: 생성 위치 (보스 무기의 현재 위치)
        // Quaternion.identity: 회전 없음 (기본 회전값)
        GameObject projectile = Instantiate(bossProjectilePrefab, transform.position, Quaternion.identity);

        // 생성된 발사체에서 NetworkObject 컴포넌트를 가져옴
        NetworkObject networkObject = projectile.GetComponent<NetworkObject>();

        // NetworkObject 컴포넌트가 존재하는지 확인
        if (networkObject == null)
        {
            // 컴포넌트가 없으면 에러 로그 출력
            Debug.LogError("BossWeapon: 발사체 프리팹에 NetworkObject 컴포넌트가 없습니다!");

            // 잘못 생성된 로컬 오브젝트 즉시 삭제
            Destroy(projectile);
            return;
        }

        // 네트워크에 발사체를 스폰하여 모든 클라이언트에 동기화
        // Spawn() 호출 시 서버와 모든 클라이언트에서 오브젝트가 활성화됨
        networkObject.Spawn();

        // 발사체에서 BossProjectile 스크립트 컴포넌트를 가져옴
        BossProjectile projectileScript = projectile.GetComponent<BossProjectile>();

        // BossProjectile 컴포넌트가 존재하는지 확인
        if (projectileScript != null)
        {
            // 발사체의 이동 방향을 설정
            // SetDirection()은 발사체가 날아갈 방향을 지정하는 메서드
            projectileScript.SetDirection(direction);
        }
        else
        {
            // BossProjectile 컴포넌트가 없으면 에러 로그 출력
            Debug.LogError("BossWeapon: 발사체에 BossProjectile 컴포넌트가 없습니다!");

            // 방향을 설정할 수 없으므로 발사체를 네트워크에서 제거
            if (networkObject.IsSpawned)
            {
                networkObject.Despawn();
            }
        }
    }
}

