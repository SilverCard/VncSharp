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

        private Bitmap desktopBitmap;

        private VncClient vnc;                           // The Client object handling all protocol-level interaction
        private int port = 5900;                         // The port to connectFromClient to on remote host (5900 is default)
        private bool passwordPending = false;            // After Connect() is called, a password might be required.
        private bool fullScreenRefresh = false;		     // Whether or not to request the entire remote screen be sent.
        private VncDesktopTransformPolicy desktopPolicy;
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

            // Use a simple desktop policy for design mode.  This will be replaced in Connect()
            desktopPolicy = new VncDesignModeDesktopPolicy(this);

            // EventHandler Settings
            VncImage.SizeChanged += new SizeChangedEventHandler(SizeChangedEventHandler);
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
        /// The Image Scale for Mouse Position Convert
        /// </summary>
        public double ImageScale { get; set; }

        [DefaultValue(5900)]
        [Description("The port number used by the VNC Host (typically 5900)")]
        /// <summary>
        /// The port number used by the VNC Host (typically 5900).
        /// </summary>
        public int VncPort
        {
            get => port;
            set
            {
                // Ignore attempts to use invalid port numbers
                if (value < 1 | value > 65535) value = 5900;
                port = value;
            }
        }

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
        public string Hostname { get => vnc == null ? "Disconnected" : vnc.HostName; }

        /// <summary>
        /// The image of the remote desktop.
        /// </summary>
        public WriteableBitmap VncImageSource { get; private set; }

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
        
        private void UpdateImage()
        {
            Dispatcher.Invoke(() => {

                var data = desktopBitmap.LockBits(new Rectangle(0, 0, vnc.Framebuffer.Width, vnc.Framebuffer.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var s = Math.Abs(data.Stride * data.Height);
                VncImageSource.WritePixels(new Int32Rect(0, 0, vnc.Framebuffer.Width, vnc.Framebuffer.Height), data.Scan0, s, data.Stride);
                desktopBitmap.UnlockBits(data);
            });
        }

        // This event handler deals with Frambebuffer Updates coming from the host. An
        // EncodedRectangle object is passed via the VncEventArgs (actually an IDesktopUpdater
        // object so that *only* Draw() can be called here--Decode() is done elsewhere).
        // The VncClient object handles thread marshalling onto the UI thread.
        protected void VncUpdate(object sender, VncEventArgs e)
        {
            e.DesktopUpdater.Draw(desktopBitmap);
            UpdateImage();

            if (state == RuntimeState.Connected)
            {
                vnc.RequestScreenUpdate(fullScreenRefresh);

                // Make sure the next screen update is incremental
                fullScreenRefresh = false;
            }
        }


        /// <summary>
        /// Connect to a VNC Host and determine whether or not the server requires a password.
        /// </summary>
        /// <param name="host">The IP Address or Host Name of the VNC Host.</param>
        /// <param name="display">The Display number (used on Unix hosts).</param>
        /// <param name="viewOnly">Determines whether mouse and keyboard events will be sent to the host.</param>
        /// <param name="scaled">Determines whether to use desktop scaling or leave it normal and clip.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if host is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if display is negative.</exception>
        /// <exception cref="System.InvalidOperationException">Thrown if the RemoteDesktop control is already Connected.  See <see cref="VncSharpWpf.RemoteDesktop.IsConnected" />.</exception>
        public async Task ConnectAsync(string host, String password, int display = 0)
        {
            // TODO: Should this be done asynchronously so as not to block the UI?  Since an event 
            // indicates the end of the connection, maybe that would be a better design.
            InsureConnection(false);

            if (host == null) throw new ArgumentNullException("host");
            if (display < 0) throw new ArgumentOutOfRangeException("display", display, "Display number must be a positive integer.");

            ShowLabelText($"Connecting to VNC host {host}:{port} please wait... ");

            // Start protocol-level handling and determine whether a password is needed
            vnc = new VncClient();
            vnc.ConnectionLost += new EventHandler(VncClientConnectionLost);

            try
            {
                passwordPending = await Task.Run(() =>
                {
                    return vnc.Connect(host, display, VncPort);
                });
            }
            catch (Exception ex)
            {
                ShowLabelText(ex.Message);
                throw;
            }


            desktopPolicy = new VncWpfDesktopPolicy(vnc, this);

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
        /// Event Handler for Event "ConnectedFromServer" 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="authentication"></param>
        public void ConnectedFromServerEventHandler(object sender, bool authentication)
        {
            throw new NotImplementedException();
            //if (authentication)
            //{
            //    // Server needs a password, so call which ever method is refered to by the GetPassword delegate.
            //    string password = GetPassword();

            //    if (password == null)
            //    {
            //        // No password could be obtained (e.g., user clicked Cancel), so stop connecting
            //        return;
            //    }
            //    else
            //    {
            //        Authenticate(password);
            //    }
            //}
            //else
            //{
            //    // No password needed, so go ahead and Initialize here
            //    Dispatcher.Invoke(new Action(() => {
            //        Initialize();
            //    }));
            //}
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
            if (vnc.Authenticate(password))
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
            vnc.Initialize(BitsPerPixel, Depth);
            SetState(RuntimeState.Connected);

            // Create a buffer on which updated rectangles will be drawn and draw a "please wait..." 
            // message on the buffer for initial display until we start getting rectangles
            SetupDesktop();

            // Tell the user of this control the necessary info about the desktop in order to setup the display
            OnConnectComplete(new ConnectEventArgs(vnc.Framebuffer.Width,
                                                   vnc.Framebuffer.Height,
                                                   vnc.Framebuffer.DesktopName));

            // Start getting updates from the remote host (vnc.StartUpdates will begin a worker thread).
            vnc.VncUpdate += new VncUpdateHandler(VncUpdate);
            vnc.StartUpdates();
        }

        private void SetState(RuntimeState newState)
        {
            state = newState;

            // Set mouse pointer according to new state
            switch (state)
            {
                case RuntimeState.Connected:
                    // Change the cursor to the "vnc" custor--a see-through dot
                    //Cursor = new Cursor(GetType(), "Resources.vnccursor.cur");
                    Dispatcher.Invoke(new Action(() =>
                    {
                        //Cursor = new Cursor("VncSharpWpf.Resources.vnccursor.cur");
                        Cursor = ((TextBlock)this.Resources["VncCursor"]).Cursor;
                    }));
                    break;
                // All other states should use the normal cursor.
                case RuntimeState.Disconnected:
                default:
                    Dispatcher.Invoke(new Action(() =>
                    {
                        Cursor = Cursors.Arrow;
                    }));
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

            List<System.Windows.Media.Color> colors = new List<System.Windows.Media.Color>();
            colors.Add(System.Windows.Media.Colors.Red);
            colors.Add(System.Windows.Media.Colors.Blue);
            colors.Add(System.Windows.Media.Colors.Green);
            BitmapPalette myPalette = new BitmapPalette(colors);

            desktopBitmap = new Bitmap(vnc.Framebuffer.Width, vnc.Framebuffer.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            Dispatcher.Invoke(new Action(() =>
            {
                VncImageSource = new WriteableBitmap(vnc.Framebuffer.Width, vnc.Framebuffer.Height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32, myPalette);
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
            vnc.ConnectionLost -= new EventHandler(VncClientConnectionLost);
            vnc.Disconnect();

            if (VncImage.Dispatcher.CheckAccess())
            {
                VncImage.Source = null;
                this.waitLabel.Visibility = Visibility.Hidden;
            }
            else
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    VncImage.Source = null;
                    this.waitLabel.Visibility = Visibility.Hidden;
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
            if (ConnectionLost != null)
            {
                ConnectionLost(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Dispatches the ConnectComplete event if any targets have registered.
        /// </summary>
        /// <param name="e">A ConnectEventArgs object with information about the remote framebuffer's geometry.</param>
        /// <exception cref="System.InvalidOperationException">Thrown if the RemoteDesktop control is not in the Connected state.</exception>
        protected void OnConnectComplete(ConnectEventArgs e)
        {
            if (ConnectComplete != null)
            {
                ConnectComplete(this, e);
            }

            HideLabel();
        }

        public void ShowLabelText(String text)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                waitLabel.Content = text;
                waitLabel.Visibility = Visibility.Visible;
            }));
        }

        public void HideLabel()
        {
            Dispatcher.BeginInvoke(new Action(() => waitLabel.Visibility = Visibility.Hidden));
        }
    }
}
