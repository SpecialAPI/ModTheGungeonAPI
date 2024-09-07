#pragma warning disable 0626
#pragma warning disable 0649

using UnityEngine;
using System.Collections.Generic;
using SGUI;
using System.ComponentModel;

public class ETGModDebugLogMenu : ETGModMenu {

    public static ETGModDebugLogMenu Instance { get; protected set; }
    public ETGModDebugLogMenu() {
        Instance = this;
    }

    /// <summary>
    /// All debug logged text lines. Feel free to add your lines here!
    /// </summary>
    protected static List<LoggedText> _AllLoggedText = new List<LoggedText>();
    protected static int _LoggedTextAddIndex = 0;

    public static Dictionary<LogType, Color> Colors = new Dictionary<LogType, Color>() {
        { LogType.Log,       new Color(1f, 1f, 1f, 0.8f) },
        { LogType.Assert,    new Color(1f, 0.2f, 0.2f, 1f) },
        { LogType.Error,     new Color(1f, 0.4f, 0.4f, 1f) },
        { LogType.Exception, new Color(1f, 0.1f, 0.1f, 1f) },
        { LogType.Warning,   new Color(1f, 1f, 0.2f, 1f) }
    };

    public override void Start()
    {
        GUI = new SGroup
        {
            Visible = false,
            Border = 20f,
            OnUpdateStyle = (SElement elem) => elem.Fill(),
            AutoLayout = (SGroup g) => g.AutoLayoutVertical,
            ScrollDirection = SGroup.EDirection.Vertical,
        };

        GUI.Children.AddRange(LogCommandInfo);
    }

    protected static BindingList<SElement> LogCommandInfo
    {
        get
        {
            var l = new BindingList<SElement>();

            if (ETGModGUI.LogClear != KeyCode.None)
                l.Add(new SLabel($"Press the \"{ETGModGUI.LogClear}\" key to clear all messages") { Foreground = Color.green });

            if (ETGModGUI.LogLeaveErrors != KeyCode.None)
                l.Add(new SLabel($"Press the \"{ETGModGUI.LogLeaveErrors}\" key to clear all messages but errors") { Foreground = Color.green });

            if (ETGModGUI.LogLeaveExceptions != KeyCode.None)
                l.Add(new SLabel($"Press the \"{ETGModGUI.LogLeaveExceptions}\" key to clear all messages but exceptions") { Foreground = Color.green });

            if (ETGModGUI.LogBottom != KeyCode.None)
                l.Add(new SLabel($"Press the \"{ETGModGUI.LogBottom}\" key to move to the bottom of the log") { Foreground = Color.green });

            if (ETGModGUI.LogTop != KeyCode.None)
                l.Add(new SLabel($"Press the \"{ETGModGUI.LogTop}\" key to move to the top of the log") { Foreground = Color.green });

            return l;
        }
    }

    public override void Update()
    {
        if (!(ETGModGUI.LogEnabled ?? true))
        {
            GUI.Enabled = false;
            GUI.Visible = false;

            return;
        }

        if (Input.GetKeyDown(ETGModGUI.LogClear))
        {
            var listOfText = _AllLoggedText;

            GUI.Children.Clear();
            GUI.ContentSize.y = 0;
            GUI.Children.AddRange(LogCommandInfo);

            listOfText.Clear();

            _LoggedTextAddIndex = 0;
            GUI.ScrollPosition.y = 0f;
        }
        else if (Input.GetKeyDown(ETGModGUI.LogLeaveErrors))
        {
            if (_AllLoggedText.Exists(x => x.LogType != LogType.Exception && x.LogType != LogType.Error))
            {
                var listOfText = _AllLoggedText;
                var toAddBack = new List<LoggedText>();

                GUI.Children.Clear();
                GUI.ContentSize.y = 0;
                GUI.Children.AddRange(LogCommandInfo);

                foreach (LoggedText t in listOfText)
                {
                    if (t.LogType is LogType.Exception or LogType.Error)
                        toAddBack.Add(new LoggedText(t.LogMessage, t.Stacktace, t.LogType) { LogCount =  t.LogCount });
                }

                listOfText.Clear();
                _LoggedTextAddIndex = 0;
                listOfText.AddRange(toAddBack);
                GUI.ScrollPosition.y = 0f;
            }
        }
        else if (Input.GetKeyDown(ETGModGUI.LogLeaveExceptions))
        {
            if (_AllLoggedText.Exists(x => x.LogType != LogType.Exception))
            {
                var listOfText = _AllLoggedText;
                var toAddBack = new List<LoggedText>();

                GUI.Children.Clear();
                GUI.ContentSize.y = 0;
                GUI.Children.AddRange(LogCommandInfo);

                foreach (LoggedText t in listOfText)
                {
                    if (t.LogType is LogType.Exception)
                        toAddBack.Add(new LoggedText(t.LogMessage, t.Stacktace, t.LogType) { LogCount = t.LogCount });
                }

                listOfText.Clear();
                _LoggedTextAddIndex = 0;
                listOfText.AddRange(toAddBack);
                GUI.ScrollPosition.y = 0f;
            }
        }

        if (_LoggedTextAddIndex < _AllLoggedText.Count)
        {
            _AllLoggedText[_LoggedTextAddIndex].Start();
            ++_LoggedTextAddIndex;
        }

        if (Input.GetKeyDown(ETGModGUI.LogBottom))
            GUI.ScrollPosition.y = float.MaxValue;

        if (Input.GetKeyDown(ETGModGUI.LogTop))
            GUI.ScrollPosition.y = 0f;
    }

    public static void Log(string log)
    {
        Logger(log, LogType.Log);
    }

    public static void LogWarning(string log)
    {
        Logger(log, LogType.Warning);
    }

    public static void LogError(string log)
    {
        Logger(log, LogType.Error);
    }

    protected static string GetStackTrace()
    {
        string stack = System.Environment.StackTrace;
        int index = stack.LastIndexOf("at UnityEngine.Debug.Log", System.StringComparison.InvariantCulture);
        if (index == -1) {
            index = stack.IndexOf("at UnityEngine.Application.CallLogCallback", System.StringComparison.InvariantCulture);
        }
        if (index == -1) {
            index = stack.IndexOf("at ETGModDebugLogMenu.Logger", System.StringComparison.InvariantCulture);
        }
        return stack.Substring(1 + stack.IndexOf('\n', index));
    }

    public static void Logger(string text, LogType type) => Logger(text, null, type);

    public static void Logger(string text, string stackTrace, LogType type)
    {
        if (!(ETGModGUI.LogEnabled ?? true))
            return;

        if (string.IsNullOrEmpty(stackTrace))
            stackTrace = GetStackTrace();

        LoggedText entry;

        if (_AllLoggedText.Count != 0)
        {
            entry = _AllLoggedText[_AllLoggedText.Count - 1];

            if (entry.LogMessage == text &&
                entry.Stacktace == stackTrace &&
                entry.LogType == type)
            {
                entry.LogCount++;
                return;
            }
        }

        entry = new LoggedText(text, stackTrace, type);
        _AllLoggedText.Add(entry);
    }

    protected class LoggedText
    {
        public string LogMessage;
        public string Stacktace;
        public LogType LogType;

        public bool IsStacktraceShown;

        protected int _LogCount = 1;
        public int LogCount
        {
            get => _LogCount;

            set
            {
                _LogCount = value;

                if (GUIMessage != null)
                    GUIMessage.Text = LogMessageFormatted;
            }
        }

        public string LogMessageFormatted
        {
            get
            {
                if (LogCount == 1)
                    return LogMessage;

                return "(" + LogCount + ") " + LogMessage;
            }
        }

        public SButton GUIMessage;
        public SLabel GUIStacktrace;

        public LoggedText(string logMessage, string stackTrace, LogType type)
        {
            LogMessage = logMessage;
            Stacktace = stackTrace;
            LogType = type;
        }

        public void Start()
        {
            if (Instance?.GUI == null)
                return;

            var color = Colors[LogType];

            GUIMessage = new SButton(LogMessageFormatted)
            {
                Parent = Instance.GUI,
                Border = Vector2.zero,
                Background = new Color(0, 0, 0, 0),
                Foreground = color,
                OnClick = delegate (SButton button)
                {
                    IsStacktraceShown = !IsStacktraceShown;
                    Instance.GUI.UpdateStyle();
                },
                With = { new SFadeInAnimation() }
            };

            GUIStacktrace = new SLabel(Stacktace)
            {
                Parent = Instance.GUI,
                Foreground = color,
                OnUpdateStyle = delegate (SElement elem)
                {
                    elem.Size.y = IsStacktraceShown ? elem.Size.y : 0f;
                    elem.Visible = IsStacktraceShown;
                }
            };
        }

    }

}
