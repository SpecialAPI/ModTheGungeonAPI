using UnityEditor;
using UnityEngine;
using SGUI;
using System.Threading;
using System.Collections;

public class SGUIEditorWindow : EditorWindow {

    public SGUIRoot Root;

    public bool Dark = false;

    public ERefreshMode RefreshMode = ERefreshMode.Fluid;
    public float FluidRefreshTimestep = 1f / 25f;
    protected float _LastFluidRender = 0f;

    public virtual void Awake() {
        Root = SGUIRoot.SetupEditor();
        if (Dark) {
            Root.Foreground = new Color(1f, 1f, 1f, 1f);
            Root.Background = new Color(0f, 0f, 0f, 0.85f);
            Root.Children.Add(new SRect(new Color(0.1f, 0.1f, 0.1f)) {
                OnUpdateStyle = elem => elem.Fill(0)
            });
        } else {
            Root.Foreground = new Color(0f, 0f, 0f, 1f);
            Root.Background = new Color(1f, 1f, 1f, 0.85f);
        }

        EditorApplication.update += OnEditorUpdate;
    }

    public virtual void OnFocus() {
        if (Root == null) Awake();
        SGUIRoot.Main = Root;
    }

    public virtual void Update() {
        if (this == null) {
            return;
        }

        if (Root == null) Awake();
        Root.Size = position.size;
        if (RefreshMode == ERefreshMode.Default) {
            Root.Update();
        } else if (RefreshMode == ERefreshMode.OnUpdate) {
            Root.Update();
            Repaint();
        }
    }

    public void OnEditorUpdate() {
        if (this == null) {
            EditorApplication.update -= OnEditorUpdate;
            return;
        }

        if (RefreshMode == ERefreshMode.Fluid && SGUIRoot.TimeUnscaled - _LastFluidRender >= FluidRefreshTimestep) {
            Root.Size = position.size;
            Root.Update();
            Repaint();
            _LastFluidRender = SGUIRoot.TimeUnscaled;
        }
    }

    public void OnInspectorUpdate() {
        if (RefreshMode == ERefreshMode.OnInspectorUpdate) {
            Root.Size = position.size;
            Root.Update();
            Repaint();
        }
    }

    public virtual void OnGUI() {
        if (Root == null) Awake();
        Root.Size = position.size;
        Root.OnGUI();
   }

    public enum ERefreshMode {
        Default,
        OnInspectorUpdate,
        OnUpdate,
        Fluid
    }

}

