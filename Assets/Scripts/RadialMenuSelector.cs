using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class RadialMenuSelector : MonoBehaviour
{
    [Serializable]
    public class RadialItem
    {
        public string name;
        public Transform target;      // 对应那个圆形按钮的Transform（UI物体）
        public UnityEvent onSelect;   // 选中后要做的事（New Task / My Tasks / Items）
        [HideInInspector] public IRadialHighlight highlighter;
        [HideInInspector] public float lastWeight;
    }

    [Header("Refs")]
    public Transform controller;             // 拖 Left/Right Hand Controller（或它的 attachTransform）
    public Camera playerCamera;              // 不填就用 Camera.main
    public List<RadialItem> items = new List<RadialItem>();

    [Header("Selection")]
    [Tooltip("允许选中的最大夹角（度）。手柄前方与目标方向夹角小于该值则可选。")]
    public float selectConeAngle = 25f;
    [Tooltip("高亮权重的平滑速度。")]
    public float highlightLerp = 12f;
    [Tooltip("最近目标与次近目标的最小角度差，避免抖动切换。")]
    public float hysteresisDeg = 5f;

    [Header("Input")]
    [Tooltip("按下即确认选择 Trigger/A等 绑定到你的 Action Map 上。")]
    public InputActionReference confirmAction;

    RadialItem _current;

    void Awake()
    {
        if (!playerCamera) playerCamera = Camera.main;

        // 把每个条目上可选的高亮组件缓存（可选）
        foreach (var it in items)
        {
            if (!it.target) continue;
            it.highlighter = it.target.GetComponent<IRadialHighlight>();
            if (it.highlighter == null)
                it.highlighter = it.target.gameObject.AddComponent<RadialHighlight_Scale>(); // 默认加一个缩放高亮
        }

        if (confirmAction != null)
        {
            confirmAction.action.performed += OnConfirm;
            confirmAction.action.Enable();
        }
    }

    void OnDestroy()
    {
        if (confirmAction != null)
            confirmAction.action.performed -= OnConfirm;
    }

    void Update()
    {
        if (!controller) return;

        // 手柄的“前方”向量
        Vector3 fwd = controller.forward;

        // 逐个计算与手柄前方的夹角，挑最小的
        RadialItem best = null;
        float bestAng = 999f;

        foreach (var it in items)
        {
            if (!it.target) continue;
            Vector3 dir = (it.target.position - controller.position).normalized;
            float ang = Vector3.Angle(fwd, dir);
            if (ang < bestAng)
            {
                bestAng = ang;
                best = it;
            }
        }

        // 基于阈值 + 迟滞确定当前项
        if (best != null)
        {
            if (_current == null)
            {
                // 进入选中
                if (bestAng <= selectConeAngle) _current = best;
            }
            else
            {
                // 已有选中，只有当最小角度比阈值小很多才切换，避免抖动
                if (best != _current && bestAng < selectConeAngle - hysteresisDeg)
                    _current = best;

                // 若当前选中的角度已经超过阈值，则取消选中
                Vector3 curDir = (_current.target.position - controller.position).normalized;
                float curAng = Vector3.Angle(fwd, curDir);
                if (curAng > selectConeAngle) _current = null;
            }
        }
        else
        {
            _current = null;
        }

        // 更新高亮权重（0~1），并做平滑
        foreach (var it in items)
        {
            float targetW = (it == _current) ? 1f : 0f;
            it.lastWeight = Mathf.Lerp(it.lastWeight, targetW, Time.deltaTime * highlightLerp);
            it.highlighter?.Apply(it.lastWeight);
        }
    }

    void OnConfirm(InputAction.CallbackContext ctx)
    {
        if (_current != null)
            _current.onSelect?.Invoke();
    }
}