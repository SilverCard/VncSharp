namespace VncSharp
{
    // See
    // https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#client-to-server-messages

    public enum ServerClientMessageType : byte
    {
        // Must support
        FramebufferUpdate = 0,
        SetColourMapEntries = 1,
        Bell = 2,
        ServerCutText = 3,

        // Optional
        ResizeFrameBuffer = 4,
        KeyFrameUpdate = 5,
        FileTransfer = 7,
        TextChat = 11,
        KeepAlive = 13
    }
}
