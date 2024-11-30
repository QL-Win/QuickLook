using System;
using System.Threading;

namespace WPFMediaKit.Threading;

public abstract class WorkDispatcherObject
{
    private bool m_restartThread;

    public bool CheckAccess()
    {
        lock (Dispatcher)
        {
            if (Dispatcher.DispatcherThread == Thread.CurrentThread)
                return true;

            return false;
        }
    }

    public void VerifyAccess()
    {
        lock (Dispatcher)
        {
            if (Dispatcher.DispatcherThread != Thread.CurrentThread)
                throw new InvalidOperationException("Not the same thread");
        }
    }

    public void EnsureThread(ApartmentState apartmentState)
    {
        lock (Dispatcher)
        {
            if (Dispatcher.ShuttingDown)
            {
                m_restartThread = true;
                return;
            }

            if (Dispatcher.DispatcherThread == null)
            {
                StartNewDispatcherThread(apartmentState);
            }
        }
    }

    private void StartNewDispatcherThread(ApartmentState apartmentState)
    {
        var reset = new ManualResetEventSlim();

        var t = new Thread((ThreadStart)delegate
        {
            Thread.CurrentThread.Name = string.Format("WorkDispatcherThread");

            Dispatcher.Run(reset);
        })
        {
            IsBackground = true
        };

        t.SetApartmentState(apartmentState);

        t.Priority = ThreadPriority.Normal;

        /* Starts the thread and creates the object */
        t.Start();

        /* We wait until our dispatcher is initialized and
         * the new Dispatcher is running */
        reset.Wait();
    }

    protected WorkDispatcherObject()
    {
        Dispatcher = new WorkDispatcher();
        Dispatcher.ShutdownFinished += Dispatcher_ShutdownFinished;
    }

    private void Dispatcher_ShutdownFinished(object sender, ShutdownFinishedEventArgs e)
    {
        lock (Dispatcher)
        {
            /* If our restart thread flag is set,
             * then we want to cancel our dispatcher's
             * shutdown and have it continue it's
             * processing of messages and delegates */
            e.CancelShutdown = m_restartThread;
            m_restartThread = false;
        }
    }

    public WorkDispatcher Dispatcher
    {
        get;
        private set;
    }
}
