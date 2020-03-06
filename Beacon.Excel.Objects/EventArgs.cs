using System;

namespace Beacon.Excel.Objects
{
    [Serializable]
    public sealed class EventArgs<TData> : EventArgs
    {
        public EventArgs(TData data) => this.Data = data;

        public TData Data
        {
            get;
        }
    }
}