// Copyright © 2018 Paddy Xu
//
// This file is part of QuickLook program.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace QuickLook.Plugin.PDFViewer;

public sealed class NotifyTaskCompletion<TResult> : INotifyPropertyChanged
{
    private readonly TResult _loading;

    public NotifyTaskCompletion(Task<TResult> task, TResult loading = default(TResult))
    {
        Task = task;
        _loading = loading;
        if (!task.IsCompleted)
        {
            var _ = WatchTaskAsync(task);
        }
    }

    public Task<TResult> Task { get; }
    public TResult Result => Task.Status == TaskStatus.RanToCompletion ? Task.Result : _loading;
    public TaskStatus Status => Task.Status;
    public bool IsCompleted => Task.IsCompleted;
    public bool IsNotCompleted => !Task.IsCompleted;

    public bool IsSuccessfullyCompleted => Task.Status ==
                                           TaskStatus.RanToCompletion;

    public bool IsCanceled => Task.IsCanceled;
    public bool IsFaulted => Task.IsFaulted;
    public AggregateException Exception => Task.Exception;
    public Exception InnerException => Exception?.InnerException;
    public string ErrorMessage => InnerException?.Message;

    public event PropertyChangedEventHandler PropertyChanged;

    private async Task WatchTaskAsync(Task task)
    {
        try
        {
            await task;
        }
        catch
        {
            // ignored
        }

        var propertyChanged = PropertyChanged;
        if (propertyChanged == null)
            return;
        propertyChanged(this, new PropertyChangedEventArgs("Status"));
        propertyChanged(this, new PropertyChangedEventArgs("IsCompleted"));
        propertyChanged(this, new PropertyChangedEventArgs("IsNotCompleted"));
        if (task.IsCanceled)
        {
            propertyChanged(this, new PropertyChangedEventArgs("IsCanceled"));
        }
        else if (task.IsFaulted)
        {
            propertyChanged(this, new PropertyChangedEventArgs("IsFaulted"));
            propertyChanged(this, new PropertyChangedEventArgs("Exception"));
            propertyChanged(this,
                new PropertyChangedEventArgs("InnerException"));
            propertyChanged(this, new PropertyChangedEventArgs("ErrorMessage"));
        }
        else
        {
            propertyChanged(this,
                new PropertyChangedEventArgs("IsSuccessfullyCompleted"));
            propertyChanged(this, new PropertyChangedEventArgs("Result"));
        }
    }
}
