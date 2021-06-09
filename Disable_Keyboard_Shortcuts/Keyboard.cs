/*
 * Last Edited On - June 9th 2021
 * Editor : Ameya Kherodkar
 * 
 * Keyboard.cs - Helps you to Enable/Disable all possible key combination available in the WINDOWS operating system as a shortcut keys
 * 
 * Here there are two parameter values available to pass to the constructor as a string
 *  1. None : Will Disable all the possible hotkeys/Shortcuts ALT + F4, ALT + TAB, WIN + E , WIN + P, WIN + D, WIN + R 
 *  2. PassAllKeysToNextApp : Will Enable all the possible hotkeys/Shortcuts ALT + F4, ALT + TAB, WIN + E , WIN + P, WIN + D, WIN + R 
 *  
 * Note : 
 *  1. This library needs to be initiated with the entry point of the application 
 *  2. Tried and Tested with Windows Forms and WPF Application 
 * 
 * Examples of Initialization
 *  Windows Forms :
 *  --Program.cs--
 *          public static KeyboardHook kh;
 *          [STAThread]
 *           static void Main()
 *           {
 *               Application.EnableVisualStyles();
 *               Application.SetCompatibleTextRenderingDefault(false);
 *           
 *               //Read setting from application config file
 *               using (kh = new KeyboardHook("None"))
 *               {
 *                   Application.Run(new Form1());
 *               }
 *           }
 *  
 *  WPF Application : 
 *  -- MainWindow.xaml.cs--
 *          public static KeyboardHook kh;
 *          public MainWindow()
 *           {
 *               kh = new KeyboardHook("None");
 *               InitializeComponent();
 *           }
 *          private void Window_Loaded(object sender, RoutedEventArgs e)
 *           {
 *           
 *               kh.KeyIntercepted += new KeyboardHook.KeyboardHookEventHandler(kh_Intercepted);
 *           }
 *          void kh_Intercepted(KeyboardHook.KeyboardHookEventArgs e)
 *          {
 *               if (e.PassThrough)
 *              {
 *                  this.Topmost = false;
 *               }
 *          }
 *  
 *  Reference Link : https://www.codeproject.com/Articles/14485/Low-level-Windows-API-hooks-from-C-to-stop-unwante
 */

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;


public class KeyboardHook : IDisposable
{
    /// <summary>
    /// Parameters accepted by the KeyboardHook constructor.
    /// </summary>
    public enum Parameters
    {
        None,
        PassAllKeysToNextApp
    }

    //Internal parameters
    private bool PassAllKeysToNextApp = true;

    //Keyboard API constants
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100; //256
    private const int WM_KEYUP = 0x0101; //257
    private const int WM_SYSKEYUP = 0x0105; //261
    private const int WM_SYSKEYDOWN = 0x0104; //261

    //Modifier key constants
    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;
    private const int VK_CAPITAL = 0x14;

    //Variables used in the call to SetWindowsHookEx
    private HookHandlerDelegate proc;
    private IntPtr hookID = IntPtr.Zero;
    internal delegate IntPtr HookHandlerDelegate(
        int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

    /// <summary>
    /// Event triggered when a keystroke is intercepted by the 
    /// low-level hook.
    /// </summary>
    public event KeyboardHookEventHandler KeyIntercepted;

    // Structure returned by the hook whenever a key is pressed
    internal struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        int scanCode;
        public int flags;
        int time;
        int dwExtraInfo;
    }

    #region Constructors
    /// <summary>
    /// Sets up a keyboard hook to trap all keystrokes without 
    /// passing any to other applications.
    /// </summary>
    public KeyboardHook()
    {
        proc = new HookHandlerDelegate(HookCallback);
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            hookID = NativeMethods.SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    /// <summary>
    /// Sets up a keyboard hook with custom parameters.
    /// </summary>
    /// <param name="param">A valid name from the Parameter enum; otherwise, the 
    /// default parameter Parameter.None will be used.</param>
    public KeyboardHook(string param)
        : this()
    {
        if (!String.IsNullOrEmpty(param) && Enum.IsDefined(typeof(Parameters), param))
        {
            SetParameters((Parameters)Enum.Parse(typeof(Parameters), param));
        }
    }

    /// <summary>
    /// Sets up a keyboard hook with custom parameters.
    /// </summary>
    /// <param name="param">A value from the Parameters enum.</param>
    public KeyboardHook(Parameters param)
        : this()
    {
        SetParameters(param);
    }
    
    private void SetParameters(Parameters param)
    {
        switch (param)
        {
            case Parameters.None:
                PassAllKeysToNextApp = false;
                break;
            case Parameters.PassAllKeysToNextApp:
                PassAllKeysToNextApp = true;
                break;
        }
    }
    #endregion

    #region Check Modifier keys
    /// <summary>
    /// Checks whether Alt, Shift, Control or CapsLock
    /// is enabled at the same time as another key.
    /// Modify the relevant sections and return type 
    /// depending on what you want to do with modifier keys.
    /// </summary>
    private void CheckModifiers()
    {
        StringBuilder sb = new StringBuilder();

        if ((NativeMethods.GetKeyState(VK_CAPITAL) & 0x0001) != 0)
        {
            //CAPSLOCK is ON
            sb.AppendLine("Capslock is enabled.");
        }

        if ((NativeMethods.GetKeyState(VK_SHIFT) & 0x8000) != 0)
        { 
            //SHIFT is pressed
            sb.AppendLine("Shift is pressed.");
        }
        if ((NativeMethods.GetKeyState(VK_CONTROL) & 0x8000) != 0)
        {
            //CONTROL is pressed
            sb.AppendLine("Control is pressed.");
        }
        if ((NativeMethods.GetKeyState(VK_MENU) & 0x8000) != 0)
        {
            //ALT is pressed
            sb.AppendLine("Alt is pressed.");
        }
        Console.WriteLine(sb.ToString());
    }
    #endregion Check Modifier keys

    #region Hook Callback Method
    /// <summary>
    /// Processes the key event captured by the hook.
    /// </summary>
    bool AllowKey= true;
    private IntPtr HookCallback(
        int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam)
    {
        if (!PassAllKeysToNextApp)
        {
            if (nCode >= 0 &&
                (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                //Disable Left and Right ALT button functionality
                if ((lParam.flags == 32) && ((lParam.vkCode == 164) || (lParam.vkCode == 165)))
                {
                    AllowKey = false;
                }
            }
            else if (nCode >= 0 &&
                (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                //Disable Left and Right WINDOW button functionality
                if ((lParam.flags == 1) && ((lParam.vkCode == 91) || (lParam.vkCode == 92)))
                {
                    AllowKey = false;
                }
            }
            OnKeyIntercepted(new KeyboardHookEventArgs(lParam.vkCode, AllowKey));

            //If this key is being suppressed, return a dummy value
            if (AllowKey == false)
                return (System.IntPtr)1;
        }
        //Pass key to next application
        return NativeMethods.CallNextHookEx(hookID, nCode, wParam, ref lParam);

    }
    #endregion

    #region Event Handling
    /// <summary>
    /// Raises the KeyIntercepted event.
    /// </summary>
    /// <param name="e">An instance of KeyboardHookEventArgs</param>
    public void OnKeyIntercepted(KeyboardHookEventArgs e)
    {
        if (KeyIntercepted != null)
            KeyIntercepted(e);
    }

    /// <summary>
    /// Delegate for KeyboardHook event handling.
    /// </summary>
    /// <param name="e">An instance of InterceptKeysEventArgs.</param>
    public delegate void KeyboardHookEventHandler(KeyboardHookEventArgs e);

    /// <summary>
    /// Event arguments for the KeyboardHook class's KeyIntercepted event.
    /// </summary>
    public class KeyboardHookEventArgs : System.EventArgs
    {

        private string keyName;
        private int keyCode;
        private bool passThrough;

        /// <summary>
        /// The name of the key that was pressed.
        /// </summary>
        public string KeyName
        {
            get { return keyName; }
        }

        /// <summary>
        /// The virtual key code of the key that was pressed.
        /// </summary>
        public int KeyCode
        {
            get { return keyCode; }
        }

        /// <summary>
        /// True if this key combination was passed to other applications,
        /// false if it was trapped.
        /// </summary>
        public bool PassThrough
        {
            get { return passThrough; }
        }

        public KeyboardHookEventArgs(int evtKeyCode, bool evtPassThrough)
        {
            keyName = ((Keys)evtKeyCode).ToString();
            keyCode = evtKeyCode;
            passThrough = evtPassThrough;
        }

    }

    #endregion

    #region IDisposable Members
    /// <summary>
    /// Releases the keyboard hook.
    /// </summary>
    public void Dispose()
    {
        NativeMethods.UnhookWindowsHookEx(hookID);
    }
    #endregion

    #region Native methods

    [ComVisibleAttribute(false),
     System.Security.SuppressUnmanagedCodeSecurity()]
    internal class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook,
            HookHandlerDelegate lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        public static extern short GetKeyState(int keyCode);
        
    } 
    #endregion
}


