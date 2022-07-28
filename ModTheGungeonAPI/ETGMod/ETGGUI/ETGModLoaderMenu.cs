using BepInEx;
using BepInEx.Bootstrap;
using SGUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class ETGModLoaderMenu : ETGModMenu
{
    public SGroup ModListGroup;

    public SGroup ModOnlineListGroup;

    public static ETGModLoaderMenu Instance { get; protected set; }
    public ETGModLoaderMenu()
    {
        Instance = this;
    }

    public override void Start()
    {
        GUI = new SGroup
        {
            Visible = false,
            OnUpdateStyle = (SElement elem) => elem.Fill(),
            Children = {
                new SLabel("BepInEx " + typeof(Paths).Assembly.GetName().Version.ToString() + "") {
                    Foreground = Color.white,
                    OnUpdateStyle = elem => elem.Size.x = elem.Parent.InnerSize.x
                },

                (ModListGroup = new SGroup {
                    Background = new Color(0f, 0f, 0f, 0f),
                    AutoLayout = (SGroup g) => g.AutoLayoutVertical,
                    ScrollDirection = SGroup.EDirection.Vertical,
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Size = new Vector2(
                            Mathf.Max(576f, elem.Parent.InnerSize.x * 0.5f),
                            Mathf.Max(662f, elem.Parent.InnerSize.y * 0.6f)
                        );
                        elem.Position = new Vector2(0f, elem.Backend.LineHeight * 2.5f);
                    },
                }),
                new SLabel("ENABLED MODS") {
                    Foreground = Color.gray,
                    OnUpdateStyle = delegate (SElement elem) {
                        elem.Position = new Vector2(ModListGroup.Position.x, ModListGroup.Position.y - elem.Backend.LineHeight - 4f);
                    },
                },
            }
        };
    }

    public override void OnOpen()
    {
        RefreshMods();
        base.OnOpen();
    }

    protected Coroutine _C_RefreshMods;
    public void RefreshMods()
    {
        _C_RefreshMods?.StopGlobal();
        _C_RefreshMods = _RefreshMods().StartGlobal();
    }
    protected virtual IEnumerator _RefreshMods()
    {
        ModListGroup.Children.Clear();
        var info = Chainloader.PluginInfos.Values.ToList();
        for (int i = 0; i < info.Count; i++)
        {
            var mod = info[i];
            ModListGroup.Children.Add(new SLabel($"{mod.Metadata.Name} {mod.Metadata.Version}") {
                With = { new SFadeInAnimation() }});
            yield return null;
        }
        yield break;
    }
}
