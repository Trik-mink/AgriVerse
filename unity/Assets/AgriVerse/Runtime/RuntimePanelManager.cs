using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    /// <summary>
    /// The sole owner of the left activity region. Controllers register their stage root and
    /// request a stage; this class atomically hides every other registered root first.
    /// It also owns the one shared instruction slot below the scenario title.
    /// </summary>
    public enum RuntimeActivityStage { Investigation, Interviews, Plan, Consequences, Feedback, Brief }

    public sealed class RuntimePanelManager : MonoBehaviour
    {
        private static RuntimePanelManager instance;
        private readonly Dictionary<RuntimeActivityStage, GameObject> panels = new Dictionary<RuntimeActivityStage, GameObject>();
        private Text instructionText;

        public RuntimeActivityStage? ActiveStage { get; private set; }
        public int ActivePanelCount
        {
            get
            {
                int count = 0;
                foreach (GameObject panel in panels.Values) if (panel != null && panel.activeSelf) count++;
                return count;
            }
        }

        public int ActiveInstructionTextCount => instructionText != null && instructionText.gameObject.activeInHierarchy ? 1 : 0;

        /// <summary>Every active long-form content region must use the shared scroll contract.</summary>
        public int ActiveScrollableContentCount => FindObjectsByType<ScrollRect>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;

        public static RuntimePanelManager GetOrCreate()
        {
            if (instance != null) return instance;
            RuntimePanelManager existing = FindFirstObjectByType<RuntimePanelManager>();
            if (existing != null) { instance = existing; return instance; }
            GameObject managerObject = new GameObject("RuntimePanelManager");
            instance = managerObject.AddComponent<RuntimePanelManager>();
            return instance;
        }

        public void Register(RuntimeActivityStage stage, GameObject panel)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));
            panels[stage] = panel;
            panel.SetActive(ActiveStage.HasValue && ActiveStage.Value == stage);
        }

        public void Show(RuntimeActivityStage stage)
        {
            ActiveStage = stage;
            foreach (KeyValuePair<RuntimeActivityStage, GameObject> entry in panels)
            {
                if (entry.Value != null) entry.Value.SetActive(entry.Key == stage);
            }
        }

        public bool IsShowing(RuntimeActivityStage stage) => ActiveStage.HasValue && ActiveStage.Value == stage;

        public void SetInstruction(string value)
        {
            EnsureInstructionSlot();
            instructionText.text = value ?? string.Empty;
        }

        private void EnsureInstructionSlot()
        {
            if (instructionText != null) return;
            GameObject canvasObject = new GameObject("RuntimeInstructionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            instructionText = new GameObject("RuntimeInstruction", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            instructionText.transform.SetParent(canvas.transform, false);
            instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            instructionText.fontSize = 18;
            instructionText.color = Color.white;
            instructionText.alignment = TextAnchor.UpperLeft;
            instructionText.verticalOverflow = VerticalWrapMode.Overflow;
            instructionText.raycastTarget = false;
            RectTransform rect = instructionText.rectTransform;
            rect.anchorMin = new Vector2(.03f, .86f);
            rect.anchorMax = new Vector2(.97f, .91f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
