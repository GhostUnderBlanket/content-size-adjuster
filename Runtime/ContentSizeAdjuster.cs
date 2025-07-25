using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        
        private IEnumerator horizontalTask;
        private IEnumerator verticalTask;
        
        public bool AutoAdjustOnEnable { get => autoAdjustOnEnable; set => autoAdjustOnEnable = value; }
        public FitMode HorizontalFit { get => horizontalFit; set => horizontalFit = value; }
        public FitMode VerticalFit { get => verticalFit; set => verticalFit = value; }
        public List<ContentSizeAdjuster> ChildAdjusters { get => childAdjusters; set => childAdjusters = value; }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (autoAdjustOnEnable)
            {
                AdjustContentSize();
            }
            else
            {
                if (horizontalTask != null)
                    StartCoroutine(horizontalTask);

                if (verticalTask != null)
                    StartCoroutine(verticalTask);
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            AdjustContentSize();
        }

        public void AdjustContentSize()
        {
            if (horizontalTask != null)
                StopCoroutine(horizontalTask);

            if (verticalTask != null)
                StopCoroutine(verticalTask);

            horizontalTask = WaitAndAdjustContentSize(0, () => horizontalTask = null);
            verticalTask = WaitAndAdjustContentSize(1, () => verticalTask = null);

            if (!gameObject.activeInHierarchy)
                return;

            StartCoroutine(horizontalTask);
            StartCoroutine(verticalTask);
        }

        private IEnumerator WaitAndAdjustContentSize(int axis, Action onComplete)
        {
            yield return new WaitForEndOfFrame();

            AdjustContentSize(axis);
            onComplete?.Invoke();
        }

        private void AdjustContentSize(int axis)
        {
            FitMode fitting = (axis == 0 ? horizontalFit : verticalFit);

            if (fitting == FitMode.Unconstrained)
                return;

            foreach (ContentSizeAdjuster adjuster in childAdjusters)
            {
                adjuster?.AdjustContentSize(axis);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

            rectTransform.SetSizeWithCurrentAnchors((Axis)axis, fitting == FitMode.MinSize ? LayoutUtility.GetMinSize(m_Rect, axis) : LayoutUtility.GetPreferredSize(m_Rect, axis));
        }

        public void GetChildAdjusters()
        {
            childAdjusters = GetComponentsInChildren<ContentSizeAdjuster>(true).Where(adjuster => adjuster.gameObject != gameObject).ToList();
        }
    }
}