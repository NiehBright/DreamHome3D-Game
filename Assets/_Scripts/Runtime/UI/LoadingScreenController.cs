using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.Runtime.UI
{
    public class LoadingScreenController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform rootOverride;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image logoImage;
        [SerializeField] private Slider progressSlider;

        [Header("Appearance")]
        [SerializeField] private Sprite logoSprite;
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.85f);
        [SerializeField] private Color barBackgroundColor = new Color(1f, 1f, 1f, 0.2f);
        [SerializeField] private Color barFillColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private Vector2 logoSize = new Vector2(320f, 160f);
        [SerializeField] private Vector2 logoOffset = new Vector2(0f, 80f);
        [SerializeField] private Vector2 barSize = new Vector2(420f, 24f);
        [SerializeField] private Vector2 barOffset = new Vector2(0f, -120f);

        [Header("Behavior")]
        [SerializeField, Min(0f)] private float simulatedDuration = 2f;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool hideOnComplete = true;
        [SerializeField] private bool blockInput = true;
        [SerializeField] private bool useUnscaledTime = true;

        private Coroutine _routine;

        private void OnEnable()
        {
            if (autoStart)
            {
                ShowAndPlay();
            }
        }

        public void ShowAndPlay()
        {
            EnsureRuntimeUi();
            if (rootOverride != null)
            {
                rootOverride.gameObject.SetActive(true);
            }

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = StartCoroutine(SimulateProgress());
        }

        public void SetProgress(float value)
        {
            if (progressSlider == null)
            {
                return;
            }

            progressSlider.value = Mathf.Clamp01(value);
        }

        public void Complete()
        {
            SetProgress(1f);
            if (hideOnComplete && rootOverride != null)
            {
                rootOverride.gameObject.SetActive(false);
            }
        }

        private IEnumerator SimulateProgress()
        {
            float duration = Mathf.Max(0.01f, simulatedDuration);
            float elapsed = 0f;
            SetProgress(0f);

            while (elapsed < duration)
            {
                float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed += delta;
                SetProgress(elapsed / duration);
                yield return null;
            }

            Complete();
            _routine = null;
        }

        private void EnsureRuntimeUi()
        {
            if (rootOverride != null && progressSlider != null)
            {
                return;
            }

            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                return;
            }

            GameObject root = new GameObject("LoadingScreen", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            rootOverride = root.GetComponent<RectTransform>();
            StretchToFill(rootOverride);

            backgroundImage = root.AddComponent<Image>();
            backgroundImage.color = backgroundColor;
            backgroundImage.raycastTarget = blockInput;

            GameObject logo = new GameObject("Logo", typeof(RectTransform));
            logo.transform.SetParent(root.transform, false);
            RectTransform logoRect = logo.GetComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.5f, 0.5f);
            logoRect.anchorMax = new Vector2(0.5f, 0.5f);
            logoRect.pivot = new Vector2(0.5f, 0.5f);
            logoRect.sizeDelta = logoSize;
            logoRect.anchoredPosition = logoOffset;

            logoImage = logo.AddComponent<Image>();
            logoImage.sprite = logoSprite;
            logoImage.color = Color.white;
            logoImage.raycastTarget = false;

            GameObject sliderRoot = new GameObject("ProgressBar", typeof(RectTransform));
            sliderRoot.transform.SetParent(root.transform, false);
            RectTransform sliderRect = sliderRoot.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.sizeDelta = barSize;
            sliderRect.anchoredPosition = barOffset;

            Image sliderBackground = sliderRoot.AddComponent<Image>();
            sliderBackground.color = barBackgroundColor;
            sliderBackground.raycastTarget = false;

            progressSlider = sliderRoot.AddComponent<Slider>();
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = 0f;
            progressSlider.transition = Selectable.Transition.None;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderRoot.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0f);
            fillAreaRect.anchorMax = new Vector2(1f, 1f);
            fillAreaRect.offsetMin = new Vector2(4f, 4f);
            fillAreaRect.offsetMax = new Vector2(-4f, -4f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = barFillColor;
            fillImage.raycastTarget = false;

            progressSlider.fillRect = fillRect;
            progressSlider.targetGraphic = fillImage;
        }

        private static void StretchToFill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}

