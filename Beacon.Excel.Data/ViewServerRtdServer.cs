#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Beacon.Excel.Objects;
using Beacon.Excel.Objects.ViewServer;
using ExcelDna.Integration;
using ExcelDna.Integration.Rtd;

namespace Beacon.Excel.Data
{
    [ComVisible(true), ProgId(nameof(ViewServerRtdServer))]
    public sealed class ViewServerRtdServer : ExcelRtdServer
    {
        private readonly List<ViewServerTopic> _topics = new List<ViewServerTopic>();

        protected override object? ConnectData(Topic topic, IList<string> topicInfo, ref bool newValues)
        {
            ViewServerTopic viewServerTopic = (ViewServerTopic)topic;
            viewServerTopic.Connect();
            this._topics.Add(viewServerTopic);
            return null;
        }

        protected override Topic CreateTopic(int topicId, IList<string> topicInfo)
        {
            if (topicInfo.Count != 2)
            {
                throw new ArgumentException(nameof(topicInfo));
            }
            string viewServer = topicInfo[0];
            SelectRequest request = (SelectRequest)Container.Instance.Resolve<IObjectCache>().Extract(topicInfo[1])!;
            return new ViewServerTopic(this, topicId, viewServer, request);
        }

        protected override void DisconnectData(Topic topic)
        {
            using ViewServerTopic viewServerTopic = (ViewServerTopic)topic;
            this._topics.Remove(viewServerTopic);
            base.DisconnectData(topic);
        }

        internal sealed class ViewServerTopic : Topic, IDisposable
        {
            private readonly ViewServerConnection _connection;
            private readonly IObjectCache _objectCache;
            private readonly Request _request;
            private readonly string _viewServer;

            public ViewServerTopic(ViewServerRtdServer server, int topicId, string viewServer, Request request)
                : base(server, topicId, ExcelErrorUtil.ToComError(ExcelError.ExcelErrorNA))
            {
                this._connection = Container.Instance.Resolve<ViewServerConnection>();
                this._objectCache = Container.Instance.Resolve<IObjectCache>();
                this._viewServer = viewServer;
                switch ((this._request = request).Command)
                {
                    case Command.Select:
                        this._connection.DataResponseReceived += this.Connection_DataResponseReceived;
                        break;
                    case Command.Metadata:
                        this._connection.MetadataResponseReceived += this.Connection_MetadataResponseReceived;
                        break;
                }
            }

            public async void Connect()
            {
                try
                {
                    await this._connection.Initialize(this._viewServer).ConfigureAwait(false);
                    this._connection.IsLoggedInChanged += this.Connection_IsLoggedInChanged;
                }
                catch
                {
                    this.UpdateValue(ExcelError.ExcelErrorNA);
                }
            }

            public void Dispose() => this._connection.Dispose();

            private async void Connection_DataResponseReceived(object sender, ResponseEventArgs e)
            {
                string key = Guid.NewGuid().ToString();
                this._objectCache.Insert(key, e.Response.Data);
                this.UpdateValue(key);
                await Task.Delay(2000).ConfigureAwait(false);
                if (this._connection.IsLoggedIn)
                {
                    await this._connection.Send(this._request).ConfigureAwait(false);
                }
            }

            private async void Connection_IsLoggedInChanged(object sender, EventArgs e)
            {
                if (this._connection.IsLoggedIn)
                {
                    await this._connection.Send(this._request).ConfigureAwait(false);
                }
            }

            private void Connection_MetadataResponseReceived(object sender, ResponseEventArgs e)
            {
                throw new NotImplementedException();
            }
        }
    }
}
