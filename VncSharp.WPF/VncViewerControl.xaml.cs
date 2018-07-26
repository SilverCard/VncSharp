// VncSharp.WPF
// Copyright (C) 2018 Ricardo Brito 
// Copyright (C) 2011 Masanori Nakano (Modified VncSharp for WPF)
// Copyright (C) 2008 David Humphrey
//
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using VncSharp.Encodings;

namespace VncSharp.WPF
{
    /// <summary>
    /// Event Handler delegate declaration used by events that signal successful connection with the server.
    /// </summary>
    public delegate void ConnectCompleteHandler(object sender, ConnectEventArgs e);

    [ToolboxBitmap(typeof(VncViewerControl), "Resources.vncviewer.ico")]
    /// <summary>
    /// The RemoteDesktop control takes care of all the necessary RFB Protocol and GUI handling, including mouse and keyboard support, as well as requesting and processing screen updates from the remote VNC host.  Most users will choose to use the RemoteDesktop control alone and not use any of the other protocol classes directly.
    /// </summary>
    public partial class VncViewerControl : UserControl
    {
        [Description("Raised after a successful call to the Connect() method.")]
        /// <summary>
        /// Raised after a successful call to the Connect() method.  Includes information for updating the local display in ConnectEventArgs.
        /// </summary>
        public event ConnectCompleteHandler ConnectComplete;

        [Description("Raised when the VNC Host drops the connection.")]
        /// <summary>
        /// Raised when the VNC Host drops the connection.
        /// </summary>
        public event EventHandler ConnectionLost;

        private VncClient _VncClient;                           // The Client object handling all protocol-level interaction
        private int _VncPort = 5900;                         // The port to connectFromClient to on remote host (5900 is default)
        private bool passwordPending = false;            // After Connect() is called, a password might be required.
        private bool fullScreenRefresh = false;		     // Whether or not to request the entire remote screen be sent.

        private RuntimeState state = RuntimeState.Disconnected;

        [DefaultValue(0)]
        [Description("Sets the number of Bits Per Pixel for the Framebuffer--one of 8, 16, or 32")]
        /// <summary>
        /// Sets the number of Bits Per Pixel for the Framebuffer--one of 8, 16, or 32
        /// </summary>
        public int BitsPerPixel { get; set; }

        [DefaultValue(0)]
        [Description("Sets the Colour Depth of the Framebuffer--one of 3, 6, 8, or 16")]
        /// <summary>
        /// Sets the Colour Depth of the Framebuffer--one of 3, 6, 8, or 16
        /// </summary>
        public int Depth { get; set; }

        public const String CursorName = "VncCursor";

        public Cursor VncCursor { get; private set; }

        /// <summary>
        /// The Image Scale for Mouse Position Convert
        /// </summary>
        public double ImageScale { get; set; }

        /// <summary>
        /// True if the RemoteDesktop is connected and authenticated (if necessary) with a remote VNC Host; otherwise False.
        /// </summary>
        public bool IsConnected { get => state == RuntimeState.Connected; }

        public bool IsListen { get => state == RuntimeState.Listen; }

        // This is a hack to get around the issue of DesignMode returning
        // false when the control is being removed from a form at design time.
        // First check to see if the control is in DesignMode, then work up 
        // to also check any parent controls.  DesignMode returns False sometimes
        // when it is really True for the parent. Thanks to Claes Bergefall for the idea.
        protected bool DesignMode => System.ComponentModel.DesignerProperties.GetIsInDesignMode(this);

        [Description("The name of the remote desktop.")]
        /// <summary>
        /// The name of the remote desktop, or "Disconnected" if not connected.
        /// </summary>
        public string Hostname { get => _VncClient == null ? "Disconnected" : _VncClient.HostName; }

        /// <summary>
        /// The image of the remote desktop.
        /// </summary>
        public WriteableBitmap VncImageSource { get; private set; }

        private enum RuntimeState
        {
            Disconnected,
            Disconnecting,
            Connected,
            Connecting,
            Listen
        }

        public VncViewerControl() : base()
        {
            InitializeComponent();

            // EventHandler Settings
            VncImage.SizeChanged += new SizeChangedEventHandler(SizeChangedEventHandler);
            VncCursor = ((TextBlock)this.Resources[CursorName]).Cursor;
        }

        /// <summary>
        /// EventHandler for Image Size Change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SizeChangedEventHandler(object sender, RoutedEventArgs e)
        {
            if (IsConnected)
            {
                ImageScale = VncImage.ActualWidth / VncImage.Source.Width;
            }
        }


        /// <summary>
        /// Get a complete update of the entire screen from the remote host.
        /// </summary>
        /// <remarks>You should allow users to call FullScreenUpdate in order to correct
        /// corruption of the local image.  This will simply request that the next update be
        /// for the full screen, and not a portion of it.  It will not do the update while
        /// blocking.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">Thrown if the RemoteDesktop control is not in the Connected state.  See <see cref="VncSharpWpf.RemoteDesktop.IsConnected" />.</exception>
        public void FullScreenUpdate()
        {
            InsureConnection(true);
            fullScreenRefresh = true;
        }

        /// <summary>
        /// Insures the state of the connection to the server, either Connected or Not Connected depending on the value of the connected argument.
        /// </summary>
        /// <param name="connected">True if the connection must be established, otherwise False.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if the RemoteDesktop control is in the wrong state.</exception>
        private void InsureConnection(bool connected)
        {
            // Grab the name of the calling routine:
            string methodName = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name;

            if (connected)
            {
                System.Diagnostics.Debug.Assert(state == RuntimeState.Connected ||
                                                state == RuntimeState.Disconnecting,  // special case for Disconnect()
                                                string.Format("RemoteDesktop must be in RuntimeState.Connected before calling {0}.", methodName));
                if (state != RuntimeState.Connected && state != RuntimeState.Disconnecting)
                {
                    throw new InvalidOperationException("RemoteDesktop must be in Connected state before calling methods that require an established connection.");
                }
            }
            else
            { // disconnected
                System.Diagnostics.Debug.Assert(state == RuntimeState.Disconnected ||
                                                state == RuntimeState.Listen,
                                                string.Format("RemoteDesktop must be in RuntimeState.Disconnected before calling {0}.", methodName));
                if (state != RuntimeState.Disconnected && state != RuntimeState.Disconnecting && state != RuntimeState.Listen)
                {
                    throw new InvalidOperationException("RemoteDesktop cannot be in Connected state when calling methods that establish a connection.");
                }
            }
        }

        //private void UpdateImage()
        //{
        //    Dispatcher.Invoke(() => {

        //        var data = _VncImage.LockBits(new Rectangle(0, 0, _VncClient.Framebuffer.Width, _VncClient.Framebuffer.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        //        var s = Math.Abs(data.Stride * data.Height);
        //        VncImageSource.WritePixels(new Int32Rect(0, 0, _VncClient.Framebuffer.Width, _VncClient.Framebuffer.Height), data.Scan0, s, data.Stride);
        //        _VncImage.UnlockBits(data);
        //    });
        //}


        private void DrawCopyRectRectangle(CopyRectRectangle cpRect)
        {
            var r = cpRect.UpdateRectangle;
            var p = cpRect.Source;
            var f = cpRect.Framebuffer;

            Dispatcher.InvokeAsync(() =>
            {
                // Avoid exception if window is dragged bottom of screen
                if (r.Top + r.Height >= f.Height)
                {
                    r.Height = f.Height - r.Top - 1;
                }

                if ((r.Y * VncImageSource.PixelWidth + r.X) < (p.Y * VncImageSource.PixelWidth + p.X))
                {
                    Int32Rect srcRect = new Int32Rect(p.X, p.Y, r.Width, r.Height);
                    VncImageSource.WritePixels(srcRect, VncImageSource.BackBuffer, VncImageSource.BackBufferStride * VncImageSource.PixelHeight, VncImageSource.PixelWidth * 4, r.X, r.Y);
                }
                else
                {
                    Int32[] pixelBuf = new Int32[r.Width * r.Height];                               
                    
                    VncImageSource.CopyPixels(new Int32Rect(p.X, p.Y, r.Width, r.Height), pixelBuf, r.Width * 4, 0);
                    VncImageSource.WritePixels(new Int32Rect(0, 0, r.Width, r.Height), pixelBuf, r.Width * 4, r.X, r.Y);
                }
            });
        }

        private void DrawEncodedRectangle(EncodedRectangle r)
        {
            var ur = r.UpdateRectangle;

            Dispatcher.Invoke(() =>
            {
                VncImageSource.WritePixels(new Int32Rect(0, 0, ur.Width, ur.Height), r.Framebuffer.pixels, ur.Width * 4, ur.X, ur.Y);
            });
        }

        // This event handler deals with Frambebuffer Updates coming from the host. An
        // EncodedRectangle object is passed via the VncEventArgs (actually an IDesktopUpdater
        // object so that *only* Draw() can be called here--Decode() is done elsewhere).
        // The VncClient object handles thread marshalling onto the UI thread.
        protected void VncUpdate(object sender, VncEventArgs e)
        {
            var du = e.DesktopUpdater;

            if (du is CopyRectRectangle) DrawCopyRectRectangle((CopyRectRectangle)du);
            else if (du is EncodedRectangle) DrawEncodedRectangle((EncodedRectangle)du);
            else throw new NotImplementedException("Drawing of this encoding is not implemented.");     
            
            if (state == RuntimeState.Connected)
            {
                _VncClient.RequestScreenUpdate(fullScreenRefresh);

                // Make sure the next screen update is incremental
                fullScreenRefresh = false;
            }
        }


        /// <summary>
        /// Connect to a VNC Host and determine whether or not the server requires a password.
        /// </summary>
        /// <param name="host">The IP Address or Host Name of the VNC Host.</param>
        /// <param name="password">Password for authentication.</param>
        /// <param name="display">The Display number (used on Unix hosts).</param>
        /// <exception cref="System.ArgumentNullException">Thrown if host is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if display is negative.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if the RemoteDesktop control is already Connected.  See <see cref="VncSharpWpf.RemoteDesktop.IsConnected" />.</exception>
        public async Task ConnectAsync(String host, String password, int display = 0)
        {
            // TODO: Should this be done asynchronously so as not to block the UI?  Since an event 
            // indicates the end of the connection, maybe that would be a better design.
            InsureConnection(false);

            if (host == null) throw new ArgumentNullException("host");
            if (display < 0) throw new ArgumentOutOfRangeException("display", display, "Display number must be a positive integer.");

            ShowLabelText($"Connecting to VNC host {host}:{_VncPort} please wait... ");

            // Start protocol-level handling and determine whether a password is needed
            _VncClient = new VncClient();
            _VncClient.ConnectionLost += new EventHandler(VncClientConnectionLost);

            try
            {
                passwordPending = await Task.Run(() =>
                {
                    return _VncClient.Connect(host, display, VncPort);
                });
            }
            catch (Exception ex)
            {
                ShowLabelText(ex.Message);
                throw;
            }

            if (passwordPending)
            {
                // Server needs a password, so call which ever method is refered to by the GetPassword delegate.
                await Task.Run(() => Authenticate(password));
            }
            else
            {
                Initialize();
            }
        }

        /// <summary>
        /// Authenticate with the VNC Host using a user supplied password.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown if the RemoteDesktop control is already Connected.  See <see cref="VncSharpWpf.RemoteDesktop.IsConnected" />.</exception>
        /// <exception cref="System.NullReferenceException">Thrown if the password is null.</exception>
        /// <param name="password">The user's password.</param>
        public void Authenticate(string password)
        {
            InsureConnection(false);
            if (!passwordPending) throw new InvalidOperationException("Authentication is only required when Connect() returns True and the VNC Host requires a password.");
            if (password == null) throw new NullReferenceException(nameof(password));

            passwordPending = false;  // repeated calls to Authenticate should fail.
            if (_VncClient.Authenticate(password))
            {
                Initialize();
            }
            else
            {
                throw new VncProtocolException("Failed to authenticate.");
            }
        }

        /// <summary>
        /// After protocol-level initialization and connecting is complete, the local GUI objects have to be set-up, and requests for updates to the remote host begun.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown if the RemoteDesktop control is already in the Connected state.  See <see cref="VncSharpWpf.RemoteDesktop.IsConnected" />.</exception>		
        protected void Initialize()
        {
            // Finish protocol handshake with host now that authentication is done.
            InsureConnection(false);
            _VncClient.Initialize(BitsPerPixel, Depth);
            SetState(RuntimeState.Connected);

            // Create a buffer on which updated rectangles will be drawn and draw a "please wait..." 
            // message on the buffer for initial display until we start getting rectangles
            SetupDesktop();

            // Tell the user of this control the necessary info about the desktop in order to setup the display
            OnConnectComplete(new ConnectEventArgs(_VncClient.Framebuffer.Width,
                                                   _VncClient.Framebuffer.Height,
                                                   _VncClient.Framebuffer.DesktopName));

            // Start getting updates from the remote host (vnc.StartUpdates will begin a worker thread).
            _VncClient.VncUpdate += new VncUpdateHandler(VncUpdate);
            _VncClient.StartUpdates();
        }

        private void SetState(RuntimeState newState)
        {
            state = newState;

            // Set mouse pointer according to new state
            switch (state)
            {
                case RuntimeState.Connected:
                    Dispatcher.Invoke(() => Cursor = VncCursor);
                    break;
                // All other states should use the normal cursor.
                case RuntimeState.Disconnected:
                default:
                    Dispatcher.Invoke(() => Cursor = Cursors.Arrow);
                    break;
            }
        }

        /// <summary>
        /// Creates and initially sets-up the local bitmap that will represent the remote desktop image.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown if the RemoteDesktop control is not already in the Connected state. See <see cref="VncSharpWpf.RemoteDesktop.IsConnected" />.</exception>
        protected void SetupDesktop()
        {
            InsureConnection(true);

            // Create a new bitmap to cache locally the remote desktop image.  Use the geometry of the
            // remote framebuffer, and 32bpp pixel format (doesn't matter what the server is sending--8,16,
            // or 32--we always draw 32bpp here for efficiency).
            //desktop = new Bitmap(vnc.Framebuffer.Width, vnc.Framebuffer.Height, PixelFormat.Format32bppPArgb);

            List<System.Windows.Media.Color> colors = new List<System.Windows.Media.Color>() {
            System.Windows.Media.Colors.Red,
            System.Windows.Media.Colors.Blue,
            System.Windows.Media.Colors.Green};

            BitmapPalette myPalette = new BitmapPalette(colors);

            Dispatcher.Invoke(new Action(() =>
            {
                VncImageSource = new WriteableBitmap(_VncClient.Framebuffer.Width, _VncClient.Framebuffer.Height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32, myPalette);
                VncImage.Source = VncImageSource;
            }));

            ShowLabelText("Connecting to VNC host, please wait...");
        }

        /// <summary>
        /// Stops the remote host from sending further updates and disconnects.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown if the RemoteDesktop control is not already in the Connected state. See <see cref="VncSharpWpf.RemoteDesktop.IsConnected" />.</exception>
        public void Disconnect()
        {
            InsureConnection(true);
            _VncClient.ConnectionLost -= new EventHandler(VncClientConnectionLost);
            _VncClient.Disconnect();

            if (VncImage.Dispatcher.CheckAccess())
            {
                VncImage.Source = null;
                this.Label.Visibility = Visibility.Hidden;
            }
            else
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    VncImage.Source = null;
                    this.Label.Visibility = Visibility.Hidden;
                }));
            }

            SetState(RuntimeState.Disconnected);
            OnConnectionLost();
        }

        /// <summary>
        /// RemoteDesktop listens for ConnectionLost events from the VncClient object.
        /// </summary>
        /// <param name="sender">The VncClient object that raised the event.</param>
        /// <param name="e">An empty EventArgs object.</param>
        protected void VncClientConnectionLost(object sender, EventArgs e)
        {
            // If the remote host dies, and there are attempts to write
            // keyboard/mouse/update notifications, this may get called 
            // many times, and from main or worker thread.
            // Guard against this and invoke Disconnect once.
            if (state == RuntimeState.Connected)
            {
                SetState(RuntimeState.Disconnecting);
                Disconnect();
            }
            else if (state == RuntimeState.Listen)
            {
                throw new NotImplementedException();
            }

            ShowLabelText("Disconnected.");
        }

        /// <summary>
        /// Dispatches the ConnectionLost event if any targets have registered.
        /// </summary>
        /// <param name="e">An EventArgs object.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if the RemoteDesktop control is in the Connected state.</exception>
        protected void OnConnectionLost()
        {
            ConnectionLost?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Dispatches the ConnectComplete event if any targets have registered.
        /// </summary>
        /// <param name="e">A ConnectEventArgs object with information about the remote framebuffer's geometry.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if the RemoteDesktop control is not in the Connected state.</exception>
        protected void OnConnectComplete(ConnectEventArgs e)
        {
            ConnectComplete?.Invoke(this, e);
            HideLabel();
        }

        public void ShowLabelText(String text)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Label.Content = text;
                Label.Visibility = Visibility.Visible;
            }));
        }

        public void HideLabel()
        {
            Dispatcher.BeginInvoke(new Action(() => Label.Visibility = Visibility.Hidden));
        }

        [DefaultValue(5900)]
        [Description("The port number used by the VNC Host (typically 5900)")]
        /// <summary>
        /// The port number used by the VNC Host (typically 5900).
        /// </summary>
        public int VncPort
        {
            get => _VncPort;
            set
            {
                // Ignore attempts to use invalid port numbers
                if (value < 1 | value > 65535) value = 5900;
                _VncPort = value;
            }
        }
    }
}
