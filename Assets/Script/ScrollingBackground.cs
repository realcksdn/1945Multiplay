using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배경 스크롤링을 처리하는 클래스
/// 텍스처 오프셋을 조정하여 무한 스크롤 효과를 구현합니다
/// </summary>
public class ScrollingBackground : MonoBehaviour
{
    [Header("스크롤 설정")]
    [Tooltip("배경이 스크롤되는 속도")]
    public float scrollSpeed = 0.01f;

    // 배경 머티리얼을 캐싱하여 성능 최적화
    private Material backgroundMaterial;

    /// <summary>
    /// 초기화: 렌더러에서 머티리얼을 가져와 캐싱
    /// </summary>
    void Start()
    {
        // Renderer 컴포넌트에서 머티리얼을 가져와 캐싱 (매 프레임 GetComponent 호출 방지)
        backgroundMaterial = GetComponent<Renderer>().material;
    }

    /// <summary>
    /// 매 프레임 배경 텍스처의 Y축 오프셋을 증가시켜 스크롤 효과 생성
    /// </summary>
    void Update()
    {
        // 현재 텍스처 오프셋의 Y값에 스크롤 속도 * 델타타임을 더해 새로운 오프셋 계산
        float newOffsetY = backgroundMaterial.mainTextureOffset.y + scrollSpeed * Time.deltaTime;

        // X축은 0으로 고정, Y축만 스크롤되도록 새 벡터 생성
        Vector2 newOffset = new Vector2(0, newOffsetY);

        // 계산된 오프셋을 머티리얼에 적용하여 배경 스크롤 효과 구현
        backgroundMaterial.mainTextureOffset = newOffset;
    }
}

