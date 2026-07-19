using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    /// <summary>
    /// Isolated character-validation harness. It does not load scenario state or any
    /// learning-loop controller and never ships as the Episode 1 gameplay scene.
    /// </summary>
    public sealed class CharacterLabController : MonoBehaviour
    {
        private const int MotionCountValue = 5;
        private static readonly string[] MotionNames =
        {
            "Mai_HatAdjust",
            "Mai_Idle",
            "Mai_Wave",
            "Mai_Talk",
            "Mai_Walk"
        };
        [SerializeField] private Animator animator;
        private Text status;

        public int MotionCount => MotionCountValue;
        public int CurrentMotionIndex { get; private set; }
        public string CurrentMotionName => MotionNames[CurrentMotionIndex];

        public void Configure(Animator sourceAnimator)
        {
            animator = sourceAnimator;
        }

        public void PlayMotion(int index)
        {
            if (animator == null) return;
            CurrentMotionIndex = Mathf.Clamp(index, 0, MotionCountValue - 1);
            animator.Play(CurrentMotionName, 0, 0f);
            RefreshStatus();
        }

        private void Awake()
        {
            Application.runInBackground = true;
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            BuildStatus();
        }

        private void Start()
        {
            PlayMotion(1);
            string captureDirectory = CaptureDirectoryFromArguments(
                Environment.GetCommandLineArgs());
            if (!string.IsNullOrWhiteSpace(captureDirectory))
            {
                Debug.Log(
                    $"CharacterLab Metal capture requested at {captureDirectory}.");
                StartCoroutine(CaptureMotions(captureDirectory));
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.digit1Key.wasPressedThisFrame) PlayMotion(0);
            else if (keyboard.digit2Key.wasPressedThisFrame) PlayMotion(1);
            else if (keyboard.digit3Key.wasPressedThisFrame) PlayMotion(2);
            else if (keyboard.digit4Key.wasPressedThisFrame) PlayMotion(3);
            else if (keyboard.digit5Key.wasPressedThisFrame) PlayMotion(4);
            else if (keyboard.spaceKey.wasPressedThisFrame)
            {
                PlayMotion((CurrentMotionIndex + 1) % MotionCountValue);
            }
        }

        private void BuildStatus()
        {
            GameObject canvasObject = new GameObject(
                "CharacterLabCanvas",
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            status = new GameObject(
                "CharacterLabStatus",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)).GetComponent<Text>();
            status.transform.SetParent(canvas.transform, false);
            status.font =
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            status.fontSize = 18;
            status.color = new Color(.96f, .94f, .86f, 1f);
            status.alignment = TextAnchor.UpperLeft;
            status.raycastTarget = false;
            status.supportRichText = false;
            RectTransform rect = status.rectTransform;
            rect.anchorMin = new Vector2(.035f, .86f);
            rect.anchorMax = new Vector2(.50f, .96f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (status == null) return;
            status.text =
                "CHARACTER LAB - MAI\n" +
                CurrentMotionName +
                "  |  keys 1-5 or Space";
        }

        private IEnumerator CaptureMotions(string directory)
        {
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(.5f);
            float[] normalizedTimes = { .12f, .38f, .64f, .88f };
            for (int motion = 0; motion < MotionCountValue; motion++)
            {
                for (int frame = 0; frame < normalizedTimes.Length; frame++)
                {
                    CurrentMotionIndex = motion;
                    animator.Play(
                        CurrentMotionName,
                        0,
                        normalizedTimes[frame]);
                    animator.Update(0f);
                    RefreshStatus();
                    yield return new WaitForEndOfFrame();
                    string path = Path.Combine(
                        directory,
                        $"{CurrentMotionName}_{frame + 1:00}.png");
                    ScreenCapture.CaptureScreenshot(path);
                    yield return new WaitForSecondsRealtime(.25f);
                }
            }
            yield return new WaitForSecondsRealtime(.5f);
            Application.Quit(0);
        }

        internal static string CaptureDirectoryFromArguments(string[] args)
        {
            if (args == null) return null;
            for (int index = 0; index < args.Length - 1; index++)
            {
                if (string.Equals(
                        args[index],
                        "-agriverse-lab-capture-dir",
                        StringComparison.Ordinal))
                {
                    return args[index + 1];
                }
            }
            return null;
        }
    }
}
