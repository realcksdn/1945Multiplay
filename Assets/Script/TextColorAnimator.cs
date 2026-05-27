using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// TextMeshPro 텍스트의 색상을 부드럽게 전환하는 애니메이터 클래스
/// 보스 등장 경고 텍스트 등에 사용되어 시각적 효과를 줍니다
/// </summary>
public class TextColorAnimator : MonoBehaviour
{
    [Header("색상 전환 설정")]
    [Tooltip("한 색상에서 다른 색상으로 전환되는 시간 (초)")]
    [SerializeField]
    private float transitionDuration = 0.1f;

    // TextMeshProUGUI 컴포넌트를 캐싱하여 성능 최적화
    private TextMeshProUGUI warningText;

    /// <summary>
    /// Awake는 오브젝트가 비활성화 상태일 때도 호출됩니다
    /// 비활성화된 오브젝트의 컴포넌트를 가져올 때는 Awake를 사용합니다
    /// </summary>
    private void Awake()
    {
        // TextMeshProUGUI 컴포넌트를 가져와 캐싱
        warningText = GetComponent<TextMeshProUGUI>();

        // 컴포넌트가 없으면 경고 표시
        if (warningText == null)
        {
            Debug.LogError("TextColorAnimator: TextMeshProUGUI 컴포넌트가 필요합니다!");
        }
    }

    /// <summary>
    /// 오브젝트가 활성화될 때 호출되는 Unity 콜백
    /// 보스 등장 시 텍스트가 활성화되면서 색상 전환 애니메이션 시작
    /// </summary>
    private void OnEnable()
    {
        // warningText가 유효한지 확인 후 코루틴 시작
        if (warningText != null)
        {
            // 색상 전환 루프 코루틴 시작
            StartCoroutine("ColorTransitionLoop");
        }
    }

    /// <summary>
    /// 오브젝트가 비활성화될 때 호출되는 Unity 콜백
    /// 코루틴을 명시적으로 중지합니다
    /// </summary>
    private void OnDisable()
    {
        // 코루틴 중지
        StopCoroutine("ColorTransitionLoop");
    }

    /// <summary>
    /// 하얀색과 빨간색을 반복적으로 전환하는 코루틴
    /// 경고 효과를 주기 위해 무한 반복합니다
    /// </summary>
    /// <returns>코루틴 열거자</returns>
    IEnumerator ColorTransitionLoop()
    {
        // 무한 반복 (오브젝트가 비활성화될 때까지)
        while (true)
        {
            // 하얀색에서 빨간색으로 전환
            yield return StartCoroutine(TransitionColor(Color.white, Color.red));

            // 빨간색에서 하얀색으로 전환
            yield return StartCoroutine(TransitionColor(Color.red, Color.white));
        }
    }

    /// <summary>
    /// 시작 색상에서 끝 색상으로 부드럽게 전환하는 코루틴
    /// Lerp를 사용하여 자연스러운 색상 변화를 구현합니다
    /// </summary>
    /// <param name="startColor">시작 색상</param>
    /// <param name="endColor">끝 색상</param>
    /// <returns>코루틴 열거자</returns>
    IEnumerator TransitionColor(Color startColor, Color endColor)
    {
        // 경과 시간 초기화
        float elapsedTime = 0.0f;

        // 전환 진행률 (0 ~ 1)
        float progress = 0.0f;

        // 전환이 완료될 때까지 반복 (progress < 1)
        while (progress < 1)
        {
            // 이전 프레임 이후 경과 시간을 누적
            elapsedTime += Time.deltaTime;

            // 전환 진행률 계산 (경과 시간 / 전체 전환 시간)
            progress = elapsedTime / transitionDuration;

            // Color.Lerp를 사용하여 시작 색상과 끝 색상 사이를 보간
            // progress가 0이면 startColor, 1이면 endColor, 중간값이면 두 색상의 중간
            if (warningText != null)
            {
                warningText.color = Color.Lerp(startColor, endColor, progress);
            }

            // 다음 프레임까지 대기
            yield return null;
        }
    }
}

