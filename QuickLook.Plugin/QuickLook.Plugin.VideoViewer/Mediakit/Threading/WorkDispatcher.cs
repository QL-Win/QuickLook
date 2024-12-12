using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace WPFMediaKit.Threading;

public class ShutdownFinishedEventArgs : EventArgs
{
    public bool CancelShutdown { get; set; }
}

/// <summary>
/// Custom dispatcher to deal with MTA and DShow,
/// see https://groups.google.com/forum/#!topic/wpf-disciples/kLdMgVzeWig
/// </summary>
public class WorkDispatcher
{
    /// <summary>
    /// Flag to set if the dispatcher needs to shutdown
    /// </summary>
    private bool m_shutdown;

    private bool m_shuttingDown;

    private object m_queueLock = new object();

    /// <summary>
    /// Queue of delegates to execute
    /// </summary>
    private readonly Queue<Delegate> m_queue = new Queue<Delegate>();

    /// <summary>
    /// The thread this dispatcher is running on
    /// </summary>
    private Thread m_dispatcherThread;

    /// <summary>
    /// The message to post to our message pump to wake it up
    /// for processing the managed delegate queue
    /// </summary>
    private const int WM_DISPATCHER_NOTIFY_DELEGATE = 0x0400 + 69;

    /// <summary>
    /// Windows message notifiying listener's of the pump we outta there
    /// and the pump is about to end
    /// </summary>
    private const int WM_QUIT = 0x12;

    /// <summary>
    /// The OS's thread identifier
    /// </summary>
    private int m_threadId;

    public event EventHandler<ShutdownFinishedEventArgs> ShutdownFinished;

    public event EventHandler ShutdownStarted;

    private void InvokeShutdownStarted()
    {
        ShutdownStarted?.Invoke(this, EventArgs.Empty);
    }

    ~WorkDispatcher()
    {
        BeginInvokeShutdown();
    }

    private ShutdownFinishedEventArgs InvokeShutdownFinished()
    {
        var e = new ShutdownFinishedEventArgs();
        ShutdownFinished?.Invoke(this, e);

        return e;
    }

    /// <summary>
    /// The thread that the dispatcher is running under
    /// </summary>
    public Thread DispatcherThread
    {
        get => m_dispatcherThread;
        private set
        {
            m_dispatcherThread = value;
        }
    }

    /// <summary>
    /// Flag, that the dispatcher is in the shutting down process.
    /// </summary>
    public bool ShuttingDown
    {
        get => m_shuttingDown;
        private set => m_shuttingDown = value;
    }

    /// <summary>
    /// Flag that the dispather has shut down.
    /// </summary>
    public bool Shutdown
    {
        get => m_shutdown;
        private set => m_shutdown = value;
    }

    public bool ShuttingOrShutDown
        => ShuttingDown || Shutdown;

    /// <summary>
    /// Async executes a method on our Dispatcher's thread
    /// </summary>
    /// <param name="method">The delegate to execute</param>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void BeginInvoke(Delegate method)
    {
        //if (Shutdown)
        //    return;

        lock (m_queueLock)
        {
            /* Add the delegate to our queue */
            m_queue.Enqueue(method);
        }

        /* Wake up our thread to notify that
         * it has something to execute */
        InvokeNotifyMessageDelegate();
    }

    /// <summary>
    /// Wakes up the pump to notify there are delegates to execute
    /// </summary>
    private void InvokeNotifyMessageDelegate()
    {
        /* Post the thread to our message pump */
        PostThreadMessage(m_threadId, WM_DISPATCHER_NOTIFY_DELEGATE, 0, 0);
    }

    /// <summary>
    /// Shuts down the dispatcher and completes any
    /// delegates or messages that are in the queue
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void BeginInvokeShutdown()
    {
        if (Shutdown)
            return;

        ShuttingDown = true;

        InvokeShutdownStarted();
        /* Process the pump */
        InvokeNotifyMessageDelegate();

        PostQuit();
    }

    /// <summary>
    /// Forces execution of any messages in the queue
    /// </summary>
    public void DoEvents()
    {
        InvokeNotifyMessageDelegate();
    }

    /// <summary>
    /// Runs the message pump in the Dispatcher
    /// </summary>
    public void Run(ManualResetEventSlim resetEvent)
    {
        if (m_threadId != 0)
            throw new InvalidOperationException("Only one thread can execute in the dispatcher at a time");

        DispatcherThread = Thread.CurrentThread;

        /* Reset our flag */
        Shutdown = false;

        /* We need to store the thread id for some p/invoke later */
        m_threadId = GetCurrentThreadId();

        // Call PeekMessage to create the message queue before the event is set
        PeekMessage(out Msg msg, IntPtr.Zero, 0, 0, 0);
        resetEvent.Set();

        /* Begins the pump */
        DoMessagePump();
    }

    /// <summary>
    /// Executes all the queued delegates
    /// </summary>
    private void DoManagedMessageQueue()
    {
        Queue<Delegate> methods;

        lock (m_queueLock)
        {
            if (m_queue.Count <= 0)
                return;

            /* Copy our delegates over to safe queue
             * so we can run the delegates outside
             * this thread lock we got going on */
            methods = new Queue<Delegate>(m_queue);
            m_queue.Clear();
        }

        /* Execute all the delegates in the queue */
        while (methods.Count > 0)
        {
            var method = methods.Dequeue();

            try
            {
                if (method != null)
                    method.DynamicInvoke(null);
            }
            catch
            {
                throw;
            }
        }
    }

    /// <summary>
    /// Posts a quit message to the message queue.
    /// This lets all pump listeners know that
    /// the end is near and abandon all resources
    /// </summary>
    private void PostQuit()
    {
        PostThreadMessage(m_threadId, WM_QUIT, 0, 0);
    }

    /// <summary>
    /// The heart of the message pump.  This method
    /// will not return until the message pump has
    /// been terminated.
    /// </summary>
    private void DoMessagePump()
    {
    top:

        Msg msg;

        DoManagedMessageQueue();

        /* Blocking call to GetMessage  */
        while (GetMessage(out msg, IntPtr.Zero, 0, 0))
        {
            switch (msg.message)
            {
                case WM_DISPATCHER_NOTIFY_DELEGATE:
                    DoManagedMessageQueue();
                    continue;
                default:
                    break;
            }

            /* Trys to translate the message.
             * Only here for completeness */
            TranslateMessage(ref msg);

            /* Dispatches the win32 message to a wnd proc.
             * The real magic is here */
            DispatchMessage(ref msg);
        }

        OleUninitialize();
        CoUninitialize();

        OleUninitialize();
        CoUninitialize();
        if (InvokeShutdownFinished().CancelShutdown)
        {
            Shutdown = false;
            ShuttingDown = false;
            goto top;
        }

        Shutdown = true;
        ShuttingDown = false;
        m_threadId = 0;
        DispatcherThread = null;
    }

    #region PInvoke

    [DllImport("ole32.dll")]
    static extern void CoUninitialize();

    [DllImport("user32"), SuppressUnmanagedCodeSecurity]
    public static extern bool PostThreadMessage(int threadId, uint msg,
                                                ushort wParam, uint lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMessage(out Msg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessage(out Msg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage([In] ref Msg lpmsg);

    [DllImport("user32.dll")]
    static extern bool TranslateMessage([In] ref Msg lpMsg);

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public IntPtr hwnd;
        public int message;
        public IntPtr wParam;
        public IntPtr lParam;
        public int time;
        public int pt_x;
        public int pt_y;
    }

    [DllImport("kernel32"), SuppressUnmanagedCodeSecurity]
    static extern int GetCurrentThreadId();

    [DllImport("ole32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
    public static extern int OleUninitialize();

    #endregion PInvoke
}
