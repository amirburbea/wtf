namespace Beacon.Excel.Data
{
    /*
    [ComVisible(true)]
    public sealed class RibbonUi : ExcelRibbon
    {
        private IRibbonUI _uiCtl;

        [ComVisible(true)]
        public sealed class PaneHost : Forms.UserControl
        {
            public ElementHost Host;

            public PaneHost()
            {
                SuspendLayout();

                Host = new ElementHost()
                {
                    BackColor = System.Drawing.Color.Transparent,
                    BackColorTransparent = true,
                    Dock = Forms.DockStyle.Fill,
                    Location = new System.Drawing.Point(0, 0),
                    Name = "Host",
                    Size = new System.Drawing.Size(180, 600),
                    TabIndex = 0,
                    TabStop = false,
                    Text = "Host",
                    Child = null
                };

                AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
                AutoScaleMode = Forms.AutoScaleMode.Font;
                AutoScroll = true;
                Controls.Add(Host);
                Margin = new Forms.Padding(0);
                Name = "PaneHost";
                Size = new System.Drawing.Size(180, 600);

                ResumeLayout(false);
            }
        }

        public override string GetCustomUI(string RibbonID)
        {
            return "<customUI xmlns=\"http://schemas.microsoft.com/office/2006/01/customui\"  onLoad=\"OnLoadCust\">" +
           "<ribbon>" + "<tabs>" + "<tab id=\"WPFTP\" label=\"WPFTPT\">" + "<group id=\"WPFTPg\" label=\"WPFTPT\">" + "<toggleButton id=\"WPFTPb\" label=\"Show Task Pane\" getPressed=\"GetPaneVisible\" onAction=\"TogglePane\" />" +
                       "</group>                    </tab>                </tabs>            </ribbon>        </customUI>";
        }

        public override void OnConnection(object application, ext_ConnectMode connectMode,
            object addInInst, ref Array custom)
        {
            base.OnConnection(application, connectMode, addInInst, ref custom);
        }

        public override void OnDisconnection(ext_DisconnectMode removeMode, ref Array custom)
        {
            TaskPaneMgr.Shutdown();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            base.OnDisconnection(removeMode, ref custom);
        }

        public bool GetPaneVisible(IRibbonControl ctl) => TaskPaneMgr.PaneVisible;

        public void OnLoadCust(IRibbonUI obj)
        {
            _uiCtl = obj;
            Globals.OnTaskPaneVisibilityChanged += () => _uiCtl?.InvalidateControl("WPFTPb");
            obj.Invalidate();
        }

        public void TogglePane(IRibbonControl ctl, bool state)
        {
            TaskPaneMgr.PaneVisible = state;
        }
    }

    static class Globals
    {
        public static event Action OnTaskPaneVisibilityChanged; // TaskPaneMgr cannot host its own event

        public static void NotifyTaskPaneVisibilityChanged() => OnTaskPaneVisibilityChanged?.Invoke();
    }

    static class TaskPaneMgr
    {
        static CustomTaskPane _ctp;
        static IntPtr _ctpParentHwnd;
        static int _paneWidth = 180;
        static UIElement _wpfPane;

        public static bool PaneVisible
        {
            get { return _ctp != null && _ctpParentHwnd == ExcelDnaUtil.WindowHandle && _ctp.Visible; }
            set
            {
                if (value)
                {
                    if (!PaneVisible)
                    {
                        EnsurePaneCreated();
                        try
                        {
                            // hide the ElementHost while toggling function pane visibility,
                            // to reduce flicker under Excel 2013/2016
                            var eh = ((RibbonUi.PaneHost)_ctp.ContentControl).Host;
                            eh.Visible = false;
                            _ctp.Visible = true;
                            eh.Visible = true;
                        }
                        catch (COMException)
                        {
                            _ctp = null;
                            EnsurePaneCreated();
                            var eh = ((RibbonUi.PaneHost)_ctp.ContentControl).Host;
                            eh.Visible = false;
                            _ctp.Visible = true;
                            eh.Visible = true;
                        }
                    }
                }
                else if (_ctp != null)
                {
                    try
                    {
                        _ctp.Visible = false;
                    }
                    catch (COMException) { } // ignore 'The taskpane has been deleted...'
                }
            }
        }

        public static void Shutdown()
        {
            try
            {
                _ctp?.Delete();
            }
            catch { }
        }

        // in actual code this will of course be replaced with a WPF UserControl class :)
        static UIElement GetWpfTaskPane() => (UIElement)XamlServices.Parse(
@"<UserControl xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <DockPanel LastChildFill='false'>
        <ComboBox Margin='4'
                  HorizontalAlignment='Center'
                  DockPanel.Dock='Top'>
            <ComboBoxItem Content='Item 1' IsSelected='True' />
            <ComboBoxItem Content='Item 2' />
            <ComboBoxItem Content='Item 3' />
            <ComboBoxItem Content='Item 4' />
            <ComboBoxItem Content='Item 5' />
            <ComboBoxItem Content='Item 6' />
            <ComboBoxItem Content='Item 7' />
            <ComboBoxItem Content='Item 8' />
        </ComboBox>
    </DockPanel>
</UserControl>");

        static void EnsurePaneCreated()
        {
            if (_wpfPane == null)
            {
                _wpfPane = GetWpfTaskPane();
            }
            if (_ctp == null || _ctpParentHwnd != ExcelDnaUtil.WindowHandle)
            {
                if (_ctp != null)
                {
                    // if we have a task pane belonging to an inactive window, delete it
                    try
                    {
                        ((RibbonUi.PaneHost)_ctp.ContentControl).Host.Child = null;
                        _ctp.Delete();
                    }
                    catch (COMException) { } // ignore 'The taskpane has been deleted...'
                }
                _ctp = CustomTaskPaneFactory.CreateCustomTaskPane(new RibbonUi.PaneHost(), "MY Task Pane");
                try
                {
                    ((RibbonUi.PaneHost)_ctp.ContentControl).Host.Child = _wpfPane;
                }
                catch (InvalidOperationException)
                {
                    // happens if the pane is already marked as child to another object
                    ((RibbonUi.PaneHost)_ctp.ContentControl).Host.Child = _wpfPane = GetWpfTaskPane();
                }
                _ctpParentHwnd = ExcelDnaUtil.WindowHandle;
                _ctp.Width = _paneWidth;
                _ctp.VisibleStateChange += p =>
                {
                    _paneWidth = _ctp.Width;
                    Globals.NotifyTaskPaneVisibilityChanged();
                };
            }
        }
    }*/
}
