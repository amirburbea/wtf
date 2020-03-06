using System.Text.Json.Serialization;

namespace Beacon.Excel.Objects.ViewServer
{
    public abstract class Request
    {
        protected Request(Command command)
        {
            this.Command = command;
        }

        [JsonConverter(typeof(CommandAttribute.CommandConverter))]
        public Command Command
        {
            get;
        }
    }
}
