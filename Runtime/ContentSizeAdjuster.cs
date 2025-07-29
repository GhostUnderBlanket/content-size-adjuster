using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.RectTransform;
using static UnityEngine.UI.ContentSizeFitter;

namespace BlanketGhost.Tools
{
    [RequireComponent(typeof(RectTransform))]
    public class ContentSizeAdjuster : UIBehaviour
    {
        private RectTransform rectTransform => m_Rect ??= GetComponent<RectTransform>();
        [NonSerialized] private RectTransform m_Rect;

        [SerializeField] private bool autoAdjustOnEnable = true;
        [SerializeField] private FitMode horizontalFit = FitMode.Unconstrained;
        [SerializeField] private FitMode verticalFit = FitMode.Unconstrained;
        [SerializeField] private List<ContentSizeAdjuster> childAdjusters;
        
        private Awaitable horizontalTask;
        private CancellationTokenSource horizontalCts;

        private Awaitable verticalTask;
        private CancellationTokenSource verticalCts;

        private bool isDirty;
        
        public bool AutoAdjustOnEnable { get => autoAdjustOnEnable; set => autoAdjustOnEnable = value; }
        public FitMode HorizontalFit { get => horizontalFit; set => horizontalFit = value; }
        public FitMode VerticalFit { get => verticalFit; set => verticalFit = value; }
        public List<ContentSizeAdjuster> ChildAdjusters { get => childAdjusters; set => childAdjusters = value; }

        protected override void OnEnable()
        {
            Debug.Log($"ContentSizeAdjuster enabled on {gameObject.name}");
            base.OnEnable();

            if (autoAdjustOnEnable)
            {
                Debug.Log("Auto adjusting content size on enable.");
                _ = AdjustContentSizeAsync();
            }
            else
            {
                Debug.Log("Auto adjust on enable is disabled.");
                horizontalTask?.GetAwaiter().OnCompleted(() => { });
                verticalTask?.GetAwaiter().OnCompleted(() => { });
            }
        }

        public async Awaitable AdjustContentSizeAsync()
        {
            Debug.Log($"<color=red>AdjustContentSizeAsync called on {gameObject.name}</color>");

            int frameCount = 0;

            horizontalCts?.Cancel();
            horizontalCts = new CancellationTokenSource();
            horizontalTask = Internal_AdjustContentSize(0, horizontalCts);

            verticalCts?.Cancel();
            verticalCts = new CancellationTokenSource();
            verticalTask = Internal_AdjustContentSize(1, verticalCts);

            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("GameObject is not active in hierarchy, skipping adjustment.");
                return;
            }

            int startFrame = Time.frameCount;
            await horizontalTask;
            frameCount += Time.frameCount - startFrame;

            int afterHorizontalFrame = Time.frameCount;
            await verticalTask;
            frameCount += Time.frameCount - afterHorizontalFrame;

            Debug.Log($"<color=red>Content size adjustment completed. Frames passed: {frameCount}</color>");
        }

        private async Awaitable WaitAndAdjustContentSize(int axis, CancellationTokenSource cts)
        {
            Debug.Log($"Waiting for end of frame before adjusting axis {axis} on {gameObject.name}");
            await Awaitable.EndOfFrameAsync(cts.Token);

            _ = Internal_AdjustContentSize(axis, cts);
        }

        private async Awaitable Internal_AdjustContentSize(int axis, CancellationTokenSource cts)
        {
            FitMode fitting = (axis == 0 ? horizontalFit : verticalFit);
            Debug.Log($"Internal_AdjustContentSize called for axis {axis} with fit mode {fitting} on {gameObject.name}");

            if (fitting == FitMode.Unconstrained)
            {
                Debug.Log($"FitMode.Unconstrained for axis {axis}, skipping adjustment on {gameObject.name}");
                return;
            }

            Debug.Log($"Waiting for frame start {gameObject.name}");
            await Awaitable.NextFrameAsync(cts.Token);
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);

            foreach (ContentSizeAdjuster adjuster in childAdjusters.Where(adjuster => adjuster != null))
            {
                Debug.Log($"Adjusting child {adjuster?.gameObject.name} for axis {axis}");
                await adjuster.Internal_AdjustContentSize(axis, cts);
            }

            Debug.Log($"Waiting for next frame before adjusting size for axis {axis} on {gameObject.name}");
            await Awaitable.NextFrameAsync(cts.Token);

            float size = fitting == FitMode.MinSize ? LayoutUtility.GetMinSize(m_Rect, axis) : LayoutUtility.GetPreferredSize(m_Rect, axis);
            Debug.Log($"Setting size {size} for axis {axis} on {gameObject.name}");
            rectTransform.SetSizeWithCurrentAnchors((Axis)axis, size);

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            Debug.Log($"Layout marked for rebuild on {gameObject.name}");
        }

#if UNITY_EDITOR
        public void GetChildAdjusters()
        {
            childAdjusters = GetComponentsInChildren<ContentSizeAdjuster>(true).Where(adjuster => adjuster.gameObject != gameObject).ToList();
        }
#endif
    }
}