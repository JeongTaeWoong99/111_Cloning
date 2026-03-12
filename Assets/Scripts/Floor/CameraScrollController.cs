using System.Collections;
using UnityEngine;

/// <summary>
/// 카메라를 목표 y 위치까지 부드럽게 스크롤한다.
/// </summary>
public class CameraScrollController : MonoBehaviour
{
    // ── Serialized Fields ─────────────────────────────────────────
    [SerializeField] private Camera _camera;
    [SerializeField] private float  _scrollSpeed = 10f;

    // ── Public Methods ────────────────────────────────────────────
    /// <summary>
    /// 카메라를 목표 y 위치까지 부드럽게 이동시킨다.
    /// </summary>
    public IEnumerator ScrollTo(float targetY)
    {
        Vector3 pos = _camera.transform.position;

        while (!Mathf.Approximately(pos.y, targetY))
        {
            pos.y = Mathf.MoveTowards(pos.y, targetY, _scrollSpeed * Time.deltaTime);
            _camera.transform.position = pos;

            yield return null;
        }

        // 정확한 위치 고정
        pos.y = targetY;
        _camera.transform.position = pos;
    }
}
