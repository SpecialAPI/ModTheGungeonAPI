#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ETGGUI;
using System;

public class ETGModGUI : MonoBehaviour
{
    public static bool? ConsoleEnabled = null;
    public static bool? LogEnabled = null;
    public static bool? LoaderEnabled = null;

    public static KeyCode ConsoleKey1 = KeyCode.F2;
    public static KeyCode ConsoleKey2 = KeyCode.Slash;
    public static KeyCode ConsoleKey3 = KeyCode.BackQuote;

    public static KeyCode LogKey1 = KeyCode.F3;
    public static KeyCode LogKey2 = KeyCode.None;
    public static KeyCode LogKey3 = KeyCode.None;

    public static KeyCode LoaderKey1 = KeyCode.F1;
    public static KeyCode LoaderKey2 = KeyCode.None;
    public static KeyCode LoaderKey3 = KeyCode.None;

    public enum MenuOpened
    {
        None,
        ModLoader,
        Logger,
        Console,
    };

    private static MenuOpened _CurrentMenu = MenuOpened.None;
    public static MenuOpened CurrentMenu
    {
        get => _CurrentMenu;

        set
        {
            var change = _CurrentMenu != value;

            if (change)
                CurrentMenuInstance.OnClose();

            _CurrentMenu = value;

            if (change)
            {
                CurrentMenuInstance.OnOpen();
                UpdateTimeScale();
                UpdatePlayerState();
            }
        }
    }

    public static GameObject MenuObject;
    public readonly static ETGModNullMenu NullMenu = new();
    public static ETGModConsole ConsoleMenu;
    public static ETGModDebugLogMenu LoggerMenu;
    public static ETGModLoaderMenu LoaderMenu;

    public static float? StoredTimeScale = null;

    public static bool UseDamageIndicators = false;

    public static IETGModMenu CurrentMenuInstance => CurrentMenu switch
    {
        MenuOpened.Console => ConsoleMenu,
        MenuOpened.Logger => LoggerMenu,
        MenuOpened.ModLoader => LoaderMenu,

        _ => NullMenu,
    };

    /// <summary>
    /// Creates a new object with this script on it.
    /// </summary>
    public static void Create()
    {
        if (MenuObject != null)
            return;

        MenuObject = new()
        {
            name = "ModLoaderMenu"
        };

        MenuObject.AddComponent<ETGModGUI>();
        DontDestroyOnLoad(MenuObject);
    }

    public void Awake()
    {
        LoggerMenu = new ETGModDebugLogMenu();
        ConsoleMenu = new ETGModConsole();
        LoaderMenu = new ETGModLoaderMenu();
    }

    public static void Start()
    {
        LoaderMenu.Start();
        LoggerMenu.Start();
        ConsoleMenu.Start();
    }

    public void Update()
    {
        if (((ConsoleEnabled ?? true) || CurrentMenu == MenuOpened.Console) && (Input.GetKeyDown(ConsoleKey1) || Input.GetKeyDown(ConsoleKey2) || Input.GetKeyDown(ConsoleKey3)))
        {
            if (CurrentMenu == MenuOpened.Console)
                CurrentMenu = MenuOpened.None;

            else
                CurrentMenu = MenuOpened.Console;

            UpdateTimeScale();
            UpdatePlayerState();
        }

        if (((LogEnabled ?? true) || CurrentMenu == MenuOpened.Logger) && (Input.GetKeyDown(LogKey1) || Input.GetKeyDown(LogKey2) || Input.GetKeyDown(LogKey3)))
        {
            if (CurrentMenu == MenuOpened.Logger)
                CurrentMenu = MenuOpened.None;

            else
                CurrentMenu = MenuOpened.Logger;
        }

        if (((LoaderEnabled ?? true) || CurrentMenu == MenuOpened.ModLoader) && (Input.GetKeyDown(LoaderKey1) || Input.GetKeyDown(LoaderKey2) || Input.GetKeyDown(LoaderKey3)))
        {
            if (CurrentMenu == MenuOpened.ModLoader)
                CurrentMenu = MenuOpened.None;

            else
                CurrentMenu = MenuOpened.ModLoader;

            UpdateTimeScale();
            UpdatePlayerState();
        }

        if (CurrentMenu != MenuOpened.None && Input.GetKeyDown(KeyCode.Escape))
            CurrentMenu = MenuOpened.None;

        CurrentMenuInstance.Update();
    }

    public static void UpdateTimeScale() {
        if (StoredTimeScale.HasValue) {
            Time.timeScale = (float)StoredTimeScale;
            StoredTimeScale = null;
        }
    }

    public static void UpdatePlayerState() {
        if (GameManager.Instance?.PrimaryPlayer != null) {
            bool set = CurrentMenu == MenuOpened.None;
            GameManager.Instance.PrimaryPlayer.enabled = set;
            CameraController cam = Camera.main?.GetComponent<CameraController>();
            if (cam != null) {
                cam.enabled = set;
            }
        }
    }

    // Font f;

    public void OnGUI() {
        if (ETGModGUI.CurrentMenu != ETGModGUI.MenuOpened.None) {
            if (!StoredTimeScale.HasValue) {
                StoredTimeScale = Time.timeScale;
                Time.timeScale = 0;
            }
        }

        CurrentMenuInstance.OnGUI();
        //RandomSelector.OnGUI();

    }

    internal static IEnumerator ListAllItemsAndGuns() {

        yield return new WaitForSeconds(1);

        int count = 0;

        while (PickupObjectDatabase.Instance == null)
            yield return new WaitForEndOfFrame();

        for (int i = 0; i < PickupObjectDatabase.Instance.Objects.Count; i++) {
            PickupObject obj = PickupObjectDatabase.Instance.Objects[i];

            if (obj==null) 
                continue;
            if (obj.encounterTrackable==null)
                continue;
            if (obj.encounterTrackable.journalData==null)
                continue;

            string name = obj.encounterTrackable.journalData.GetPrimaryDisplayName(true).Replace(' ', '_').ToLower();

            count++;

            // Handle Master Rounds specially because we actually care about the order
            if (name == "master_round") {
                string objectname = obj.gameObject.name;
                int floornumber = 420;
                switch (objectname.Substring("MasteryToken_".Length)) {
                case "Castle": // Keep of the Lead Lord
                    floornumber = 1;
                    break;
                case "Gungeon": // Gungeon Proper
                    floornumber = 2;
                    break;
                case "Mines":
                    floornumber = 3;
                    break;
                case "Catacombs": // Hollow
                    floornumber = 4;
                    break;
                case "Forge":
                    floornumber = 5;
                    break;
                }
                name = name + "_" + floornumber;
            }



            if (ETGModConsole.AllItems.ContainsKey(name)) {
                int appendindex = 2;
                while (ETGModConsole.AllItems.ContainsKey (name + "_" + appendindex.ToString())) {
                    appendindex++;
                }
                name = name + "_" + appendindex.ToString ();
            }
            ETGModConsole.AllItems.Add(name, i);
            if (count >= 30) {
                count = 0;
                yield return null;
            }
        }

        //Add command arguments.
        Debug.Log(ETGModConsole.AllItems.Values.Count + " give command args");

    }

}

