using Unity.Netcode;
using UnityEngine;

// 플레이어 색상을 변경하는 간단한 스크립트
public class PlayerColorController : NetworkBehaviour
{
    #region 변수 선언
    // 색상 변경 강도 (0=원본 색상 유지, 1=완전히 변경)
    [SerializeField]
    [Range(0f, 1f)] private float colorIntensity = 0.6f;  // Inspector에서 0~1 슬라이더로 조절 가능

    private SpriteRenderer spriteRenderer;                // 이 오브젝트의 스프라이트 렌더러 컴포넌트
    private Color originalColor;                          // 게임 시작 시 스프라이트의 원래 색상

    // 네트워크로 동기화되는 플레이어 타입 변수 (0=클라이언트, 1=호스트)
    private NetworkVariable<int> playerType = new NetworkVariable<int>(0); // 기본값 0으로 초기화
    #endregion

    // Unity 생명주기: 오브젝트가 생성될 때 가장 먼저 호출됨
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>(); // 이 게임오브젝트에서 SpriteRenderer 컴포넌트 찾기

        // 스프라이트 렌더러가 존재하는지 확인
        if (spriteRenderer != null) // null 체크로 에러 방지
        {
            originalColor = spriteRenderer.color; // 현재 색상을 원본 색상으로 저장
        }
    }

    // Netcode 생명주기: 네트워크 오브젝트가 스폰될 때 호출됨
    public override void OnNetworkSpawn()
    {
        // 이 플레이어가 로컬 플레이어(내가 조종하는 플레이어)인지 확인
        if (IsOwner)
        {
            // NetworkManager가 호스트 모드인지 확인하여 타입 결정
            int type = NetworkManager.Singleton.IsHost ? 1 : 0; // 호스트면 1, 클라이언트면 0

            SetPlayerTypeServerRpc(type); // 서버에 내 타입 정보 전송 (RPC 호출)
        }

        playerType.OnValueChanged += OnColorChanged; // NetworkVariable 값이 바뀌면 OnColorChanged 함수 호출하도록 등록
        UpdateColor(playerType.Value);               // 현재 playerType 값으로 색상 즉시 업데이트
    }

    // ServerRpc: 클라이언트에서 호출하면 서버에서 실행되는 함수
    [ServerRpc] // 이 속성이 있어야 RPC로 작동함
    void SetPlayerTypeServerRpc(int type) // 매개변수로 플레이어 타입 받기
    {
        playerType.Value = type; // NetworkVariable 값 변경 (자동으로 모든 클라이언트에 동기화됨)
    }

    // NetworkVariable의 값이 변경되었을 때 자동으로 호출되는 콜백 함수
    void OnColorChanged(int oldValue, int newValue) // 이전 값과 새로운 값을 매개변수로 받음
    {
        UpdateColor(newValue); // 새로운 타입 값으로 색상 업데이트
    }

    // 실제로 스프라이트 색상을 변경하는 함수
    void UpdateColor(int type) // 플레이어 타입을 매개변수로 받음
    {
        if (spriteRenderer == null) return; // 스프라이트 렌더러가 없으면 함수 종료 (안전 장치)

        // 타입에 따라 목표 색상 결정 (삼항 연산자 사용)
        Color targetColor = (type == 1) ? Color.blue : Color.red; // 1이면 파란색, 아니면 빨간색

        // Color.Lerp로 두 색상을 섞음 (선형 보간)
        // colorIntensity가 0이면 originalColor, 1이면 targetColor, 0.5면 정확히 중간색
        Color blendedColor = Color.Lerp(originalColor, targetColor, colorIntensity);

        spriteRenderer.color = blendedColor; // 계산된 블렌드 색상을 스프라이트에 적용
    }
}

// playerType.OnValueChanged += OnColorChanged;
// 코드 설명 링크 : http://bit.ly/47lxNMY








