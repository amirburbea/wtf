#nullable enable
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using Beacon.Excel.Data.Presentation;
using Beacon.Excel.Data.Properties;
using Beacon.Excel.Objects.Environments;
using Beacon.Excel.Objects.User;
using ExcelDna.Integration;
using ExcelDna.Integration.CustomUI;

namespace Beacon.Excel.Data
{
    /// <summary>
    /// Callback methods are invoked by IDispatch and as a result all methods must be <see langword="public"/> .
    /// </summary>
    [ComVisible(true)]
    public sealed class Ribbon : ExcelRibbon
    {
        private IEnvironmentManager? _environmentManager;
        private IPresentationService? _presentationService;
        private IRibbonUI? _ribbonUi;
        private IUserManager? _userManager;

        public override string GetCustomUI(string ribbonId)
        {
            using TextWriter textWriter = new StringWriter();
            using XmlWriter xmlWriter = XmlWriter.Create(textWriter, new XmlWriterSettings { Indent = true });
            xmlWriter.WriteStartElement("customUI", ExcelRibbon.NamespaceCustomUI2007);
            xmlWriter.WriteAttributeString("loadImage", nameof(Ribbon.GetEmbeddedBitmap));
            xmlWriter.WriteAttributeString("onLoad", nameof(Ribbon.OnLoad));
            xmlWriter.WriteStartElement("ribbon", ExcelRibbon.NamespaceCustomUI2007);
            xmlWriter.WriteStartElement("tabs", ExcelRibbon.NamespaceCustomUI2007);
            xmlWriter.WriteStartElement("tab", ExcelRibbon.NamespaceCustomUI2007);
            xmlWriter.WriteAttributeString("id", Constants.TabId);
            xmlWriter.WriteAttributeString("label", "Beacon");
            xmlWriter.WriteStartElement("group", ExcelRibbon.NamespaceCustomUI2007);
            xmlWriter.WriteAttributeString("id", "userGroup");
            xmlWriter.WriteAttributeString("label", "User");
            xmlWriter.WriteStartElement("button", ExcelRibbon.NamespaceCustomUI2007);
            xmlWriter.WriteAttributeString("id", Constants.LogonButtonId);
            xmlWriter.WriteAttributeString("getVisible", nameof(Ribbon.GetVisible));
            xmlWriter.WriteAttributeString("image", "login.png");
            xmlWriter.WriteAttributeString("label", "Login");
            xmlWriter.WriteAttributeString("onAction", nameof(Ribbon.OnButtonAction));
            xmlWriter.WriteEndElement(); // button
            xmlWriter.WriteStartElement("button", ExcelRibbon.NamespaceCustomUI2007);
            xmlWriter.WriteAttributeString("id", Constants.LogoutButtonId);
            xmlWriter.WriteAttributeString("getVisible", nameof(Ribbon.GetVisible));
            xmlWriter.WriteAttributeString("image", "logout.png");
            xmlWriter.WriteAttributeString("getLabel", nameof(Ribbon.GetLabel));
            xmlWriter.WriteAttributeString("onAction", nameof(Ribbon.OnButtonAction));
            xmlWriter.WriteEndElement(); // button
            xmlWriter.WriteEndElement(); // group
            xmlWriter.WriteStartElement("group", ExcelRibbon.NamespaceCustomUI2007);
            xmlWriter.WriteAttributeString("id", "optionsGroup");
            xmlWriter.WriteAttributeString("label", "Options");
            xmlWriter.WriteStartElement("dropDown", ExcelRibbon.NamespaceCustomUI2007);
            xmlWriter.WriteAttributeString("id", Constants.DataEnvironmentId);
            xmlWriter.WriteAttributeString("label", "Environment:");
            xmlWriter.WriteAttributeString("getVisible", nameof(Ribbon.GetVisible));
            xmlWriter.WriteAttributeString("getSelectedItemID", nameof(Ribbon.GetSelectedItemId));
            xmlWriter.WriteAttributeString("onAction", nameof(Ribbon.OnDropDownAction));
            foreach (DataEnvironment environment in Enum.GetValues(typeof(DataEnvironment)))
            {
                string name = Enum.GetName(typeof(DataEnvironment), environment);
                xmlWriter.WriteStartElement("item", ExcelRibbon.NamespaceCustomUI2007);
                xmlWriter.WriteAttributeString("id", name);
                xmlWriter.WriteAttributeString("label", name.ToUpperInvariant());
                xmlWriter.WriteEndElement(); // item
            }
            xmlWriter.WriteEndElement(); // dropDown
            xmlWriter.WriteEndElement(); // group
            xmlWriter.WriteEndElement(); // tab
            xmlWriter.WriteEndElement(); // tabs
            xmlWriter.WriteEndElement(); // ribbon
            xmlWriter.WriteEndElement(); // customUI
            xmlWriter.Flush();
            return textWriter.ToString();
        }

        public Bitmap GetEmbeddedBitmap(string name) => ResourceBitmaps.GetBitmap(name);

        public string? GetLabel(IRibbonControl control)
        {
            return this._userManager?.User == null || control.Id != Constants.LogoutButtonId
                ? null
                : $"Logout {this._userManager.User.FirstName} {this._userManager.User.LastName}";
        }

        public string? GetSelectedItemId(IRibbonControl control)
        {
            if (control.Id == Constants.DataEnvironmentId && this._environmentManager != null)
            {
                return Enum.GetName(typeof(DataEnvironment), this._environmentManager.Environment);
            }
            return null;
        }

        public bool GetVisible(IRibbonControl control)
        {
            return this._userManager != null && (this._userManager.User == null) == (control.Id == Constants.LogonButtonId);
        }

        public override void OnBeginShutdown(ref Array custom)
        {
            if (this._userManager != null)
            {
                this._userManager.UserChanged -= this.UserManager_UserChanged;
            }
            if (this._environmentManager != null)
            {
                this._environmentManager.EnvironmentChanged -= this.EnvironmentManager_EnvironmentChanged;
            }
            Settings.Default.Save();
            base.OnBeginShutdown(ref custom);
        }

        public void OnButtonAction(IRibbonControl control)
        {
            if (this._userManager != null && this._presentationService != null && this._environmentManager != null)
            {
                switch (control.Id)
                {
                    case Constants.LogoutButtonId:
                        this._userManager.Logout();
                        break;
                    case Constants.LogonButtonId:
                        this._presentationService.ShowLoginDialog();
                        break;
                }
            }
        }

        public void OnDropDownAction(IRibbonControl control, string selectedId, int selectedIndex)
        {
            if (control.Id == Constants.DataEnvironmentId && this._environmentManager != null && selectedIndex != -1)
            {
                this._environmentManager.Environment = (DataEnvironment)Enum.Parse(typeof(DataEnvironment), selectedId);
            }
        }

        public void OnLoad(IRibbonUI ribbonUi)
        {
            this._ribbonUi = ribbonUi;
            this._userManager = Container.Instance.Resolve<IUserManager>();
            this._presentationService = Container.Instance.Resolve<IPresentationService>();
            this._environmentManager = Container.Instance.Resolve<IEnvironmentManager>();
            this._userManager.UserChanged += this.UserManager_UserChanged;
            this._environmentManager.EnvironmentChanged += this.EnvironmentManager_EnvironmentChanged;
        }

        private void EnvironmentManager_EnvironmentChanged(object sender, EventArgs e)
        {
            if (this._ribbonUi == null)
            {
                return;
            }
            this._ribbonUi.InvalidateControl(Constants.DataEnvironmentId);
            NativeMethods.SetForegroundWindow(ExcelDnaUtil.WindowHandle);
        }

        private void UserManager_UserChanged(object sender, EventArgs e)
        {
            if (this._ribbonUi == null)
            {
                return;
            }
            this._ribbonUi.InvalidateControl(Constants.LogonButtonId);
            this._ribbonUi.InvalidateControl(Constants.LogoutButtonId);
            this._ribbonUi.InvalidateControl(Constants.DataEnvironmentId);
            NativeMethods.SetForegroundWindow(ExcelDnaUtil.WindowHandle);
        }

        private static class Constants
        {
            public const string DataEnvironmentId = "dataEnvironment";
            public const string LogonButtonId = "logonButton";
            public const string LogoutButtonId = "logoutButton";
            public const string TabId = "beacon";
        }
    }
}
