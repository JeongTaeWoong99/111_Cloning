using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    /// <summary>
    /// UI 아이템 드래그앤드랍 컴포넌트.
    /// 드롭에 실패하면 OnEndDrag에서 자동으로 이전 부모로 복귀합니다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class DraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // ──────────────────────────────────────────
        // Private Fields
        // ──────────────────────────────────────────
        private const float DraggingAlpha = 0.6f;

        private CanvasGroup _canvasGroup;
        private Canvas      _rootCanvas;
        private Transform   _previousParent;
        private bool        _wasDropped;

        // ──────────────────────────────────────────
        // Properties
        // ──────────────────────────────────────────
        public static bool IsDragging  { get; private set; }
        public Transform PreviousParent => _previousParent;

        // ──────────────────────────────────────────
        // MonoBehaviour
        // ──────────────────────────────────────────
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();

            // Unity 6 호환: FindFirstObjectByType 사용
            _rootCanvas = FindFirstObjectByType<Canvas>();
        }

        // ──────────────────────────────────────────
        // Drag Handlers
        // ──────────────────────────────────────────
        public void OnBeginDrag(PointerEventData eventData)
        {
            IsDragging      = true;
            _wasDropped     = false;
            _previousParent = transform.parent;

            // Canvas 최상단으로 이동 → 다른 UI 위에 그려지도록
            transform.SetParent(_rootCanvas.transform);
            transform.SetAsLastSibling();

            _canvasGroup.alpha          = DraggingAlpha;
            _canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Canvas의 렌더 모드에 따라 월드/스크린 좌표 분기
            if (_rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                transform.position = eventData.position;
            }
            else
            {
                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    _rootCanvas.transform as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector3 worldPoint
                );
                transform.position = worldPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            IsDragging                  = false;
            _canvasGroup.alpha          = 1f;
            _canvasGroup.blocksRaycasts = true;

            // Refresh가 새 ItemUI를 생성했으므로 원본 제거
            if (_wasDropped)
            {
                Destroy(gameObject);
                return;
            }

            // 슬롯에 드롭되지 않은 경우 → 이전 부모로 복귀
            if (transform.parent == _rootCanvas.transform)
                ReturnToPreviousParent();
        }

        // ──────────────────────────────────────────
        // Public Methods
        // ──────────────────────────────────────────

        /// <summary>드롭 성공을 알립니다. OnEndDrag에서 gameObject를 제거합니다.</summary>
        public void NotifyDropped() => _wasDropped = true;

        /// <summary>이전 슬롯 위치로 복귀합니다.</summary>
        public void ReturnToPreviousParent()
        {
            if (_previousParent != null)
            {
                transform.SetParent(_previousParent);
                transform.localPosition = Vector3.zero;
            }
        }
    }
}
