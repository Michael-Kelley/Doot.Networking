using System;



namespace Doot
{
    public class ChatMessageEventArgs : EventArgs
    {
        public string UserId { get;}
        public string RoomId { get;}
        public string Message { get; }

        public ChatMessageEventArgs(string userId, string roomId, string message)
        {
            UserId = userId;
            RoomId = roomId;
            Message = message;
        }
    }
}
