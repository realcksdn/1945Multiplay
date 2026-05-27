using UnityEngine; 
using Unity.Netcode; // 네트워크 멀티플레이 기능 사용

// 네트워크 오브젝트 자동 제거 클래스
public class NetworkDestroy : NetworkBehaviour
{
    // 파괴까지 남은 시간 (인스펙터에서 수정 가능)
    [SerializeField] private float timetoDestroy = 3f;

    // NetworkObject 컴포넌트 캐싱 변수
    private NetworkObject networkObject;

    private void Awake() 
    {
        // NetworkObject 컴포넌트를 가져와서 저장 (성능 최적화)
        networkObject = GetComponent<NetworkObject>();
    }

    // 네트워크 스폰 시 호출
    public override void OnNetworkSpawn()
    {
        if (!IsServer)        // 서버가 아니면 (클라이언트면)
        {
            enabled = false;  // 스크립트 비활성화 (클라이언트는 타이머 실행 안 함)
        }
    }

    void Update() // 매 프레임 호출
    {
        if (!IsServer) return;               // 서버가 아니면 즉시 종료

        timetoDestroy -= Time.deltaTime;     // 남은 시간 감소

        if (timetoDestroy <= 0)              // 시간이 다 되면
        {
            if (networkObject != null && networkObject.IsSpawned) // 유효성 검사
            {
                // 네트워크 오브젝트 완전 제거 (모든 클라이언트 동기화)
                networkObject.Despawn(true);
            }
        }
    }
}

// 코드 설명 : http://bit.ly/4o4CZdK
// Server RPC(ServerRpc)와 Client RPC (ClientRpc) 설명 : // http://bit.ly/3VN99OT














