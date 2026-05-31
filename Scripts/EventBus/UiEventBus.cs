using System;
using Godot;

namespace CardBase.Scripts;

public class UiEventBus
{
    public event EventHandler<NotificationArgs> NotificationEventHandler;

    public void EmitNotification(NotificationArgs args)
    {
        NotificationEventHandler?.Invoke(this, args);
    }
}

public enum NotificationEventType
{
    WARNING,
    ERROR,
    SUCCESS
}

public class NotificationArgs
{
    public readonly NotificationEventType EventType;
    
    public readonly string Message;
    
    public NotificationArgs(NotificationEventType eventType,  string message)
    {
        this.EventType = eventType;
        this.Message = message;
    }
}