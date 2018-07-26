namespace VncSharp
{
    // See
    // https://github.com/rfbproto/rfbproto/blob/master/rfbproto.rst#client-to-server-messages

    public enum ClientServerMessageType : byte
    {
        // Must support
        SetPixelFormat = 0,
        SetEncodings = 2,
        FramebufferUpdateRequest = 3,
        KeyEvent = 4,
        PointerEvent = 5,
        ClientCutText = 6,

        // Optional
        FileTransfer = 7,
        SetScale = 8,
        SetServerInput = 9,
        SetSW = 10,
        TextChat = 11,
        KeyFrameRequest = 12,
        KeepAlive = 13,
        SetDesktopSize = 251
    }
}
