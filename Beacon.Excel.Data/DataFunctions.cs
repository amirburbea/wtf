#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Beacon.Excel.Objects;
using Beacon.Excel.Objects.Environments;
using Beacon.Excel.Objects.Json;
using Beacon.Excel.Objects.User;
using Beacon.Excel.Objects.ViewServer;
using ExcelDna.Integration;
using Microsoft.Office.Interop.Excel;

namespace Beacon.Excel.Data
{
    public interface IDataFunctions
    {
        /*
        [ExcelFunction(Name = "Beacon.Data", Description = "Raw Get Data Function")]
        object? GetData(
            [ExcelArgument(Description = "The")] string viewServer,
            [ExcelArgument(Description = "The")] string view,
            [ExcelArgument(Description = "The")] string fields,
            [ExcelArgument(Description = "The")] string? where = default,
            [ExcelArgument(Description = "The")] string? orderBy = default,
            [ExcelArgument(Description = "The")] string? groupBy = default,
            [ExcelArgument(Description = "The")] int? limit = default,
            [ExcelArgument(Description = "The")] int? offset = default
        );
        */

        [ExcelFunction(Name = "GETFOO", Description = "Raw Get Data Function")]
        object? GetFoo(string? a1, string? a2);

        [ExcelFunction(Name = "Beacon.FxRates", Description = "Gets the Fx Rates. Returns data in the form \"Symbol,Open,Last,Net\".")]
        object? GetFxRates();

        [ExcelFunction(Name = "Beacon.LastPrice", Description = "Gets the last price for a symbol.")]
        object? GetLastPrice([ExcelArgument(Description = "The symbol for which to get the last price.")] string? symbol);

        [ExcelFunction(Name = "Beacon.OrderAccounts", Description = "Gets the order accounts.")]
        object? GetOrderAccounts();
    }

    internal sealed class DataFunctions : IDataFunctions, IDisposable
    {
        private static readonly string _fxWhereClause = CreateFxWhereClause();

        private readonly IEnvironmentManager _environmentManager;
        private readonly IObjectCache _objectCache;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly IUserManager _userManager;

        public DataFunctions(IUserManager userManager, IEnvironmentManager environmentManager, IObjectCache objectCache, JsonSerializerOptions serializerOptions)
        {
            this._userManager = userManager;
            this._environmentManager = environmentManager;
            this._objectCache = objectCache;
            this._serializerOptions = serializerOptions;
            this._userManager.UserChanged += this.OnUserChanged;
            this._environmentManager.EnvironmentChanged += this.OnEnvironmentChanged;
        }

        public void Dispose()
        {
            this._userManager.UserChanged -= this.OnUserChanged;
        }

        public object? GetData(string viewServer, string view, string fields, string? where = default, string? orderBy = default, string? groupBy = default ,int? limit = default, int? offset = default)
        {
            if (this._userManager.User == null)
            {
                return "Error: Not logged in.";
            }
            string location = DataFunctions.GetLocation();
            SelectRequest request = new SelectRequest
            {
                QueryId = location,
                Parameters = new SelectRequestParameters
                {
                    View = view,
                    Fields = fields,
                    Where = where,
                    OrderBy = orderBy,
                    GroupBy = groupBy,
                    Limit = limit,
                    Offset = offset
                }
            };
            string requestKey = JsonSerializer.Serialize(request, this._serializerOptions).GetHashCode().ToString();
            this._objectCache.Insert(requestKey, request);
            object result = XlCall.RTD(nameof(ViewServerRtdServer), null, viewServer, requestKey);
            if (result == null)
            {
                return ExcelErrorUtil.ToComError(ExcelError.ExcelErrorNA);
            }
            if (result is string dataKey)
            {
                DataContainer? dataContainer = (DataContainer?)this._objectCache.Extract(dataKey);
                if (dataContainer == null || !(dataContainer.Data is IReadOnlyList<object> list) || list.Count == 0)
                {
                    return ExcelErrorUtil.ToComError(ExcelError.ExcelErrorNA);
                }
                string[] columns = fields.Split(',');
                object[,] output = new object[list.Count, columns.Length];
                for (int row = 0; row < list.Count; row++)
                {
                    IReadOnlyDictionary<string, object?> item = (IReadOnlyDictionary<string, object?>)list[row];
                    for (int column = 0; column < columns.Length; column++)
                    {
                        output[row, column] = item.TryGetValue(columns[column], out object? value) && value != null
                            ? value
                            : ExcelErrorUtil.ToComError(ExcelError.ExcelErrorNull);
                    }
                }
                return output;
            }
            return result;
        }

        object? IDataFunctions.GetFxRates()
        {
            return this.GetData("marketdata", "marketdataWindow", "symbol,open,last,net", DataFunctions._fxWhereClause, "symbol");
        }

        object? IDataFunctions.GetLastPrice(string? symbol)
        {
            return string.IsNullOrEmpty(symbol)
                ? ExcelErrorUtil.ToComError(ExcelError.ExcelErrorValue)
                : this.GetData("marketdata", "marketdataWindow", "last", $"symbol='{symbol}'");
        }

        object? IDataFunctions.GetOrderAccounts()
        {
            return this.GetData("orders", "accountWindow", "account", null, "account");
        }

        private static void CalculateFull() => ((Application)ExcelDnaUtil.Application).CalculateFull();

        private static string CreateFxWhereClause()
        {
            string[] symbols = new[]
            {
              "EURUSD",
              "GBPUSD",
              "GBpUSD",
              "MYRUSD",
              "NZDUSD",
              "USDBRL",
              "USDCAD",
              "USDCHF",
              "USDCLP",
              "USDCNY",
              "USDCZK",
              "USDDKK",
              "USDHKD",
              "USDHUF",
              "USDIDR",
              "USDILS",
              "USDINR",
              "USDJPY",
              "USDKRW",
              "USDMXN",
              "USDNOK",
              "USDPHP",
              "USDPLN",
              "USDRUB",
              "USDSEK",
              "USDSGD",
              "USDTHB",
              "USDTRY",
              "USDTWD",
              "USDZAR",
              "ZArUSD"
            };
            StringBuilder builder = new StringBuilder("symbol IN (");
            for (int index = 0; index < symbols.Length; index++)
            {
                string symbol = symbols[index];
                if (index != 0)
                {
                    builder.Append(',');
                }
                builder.Append('\'').Append(symbol).Append('\'');
            }
            return builder.Append(')').ToString();
        }

        private static string GetLocation()
        {
            ExcelReference reference = (ExcelReference)XlCall.Excel(XlCall.xlfCaller);
            string sheetName = (string)XlCall.Excel(XlCall.xlSheetNm, reference);
            string address = (string)XlCall.Excel(XlCall.xlfAddress, 1 + reference.RowFirst, 1 + reference.ColumnFirst);
            return string.Concat(Environment.MachineName, sheetName, address);
        }

        private void OnEnvironmentChanged(object? sender, EventArgs e)
        {
            if (this._userManager.User != null)
            {
                // If user is null, we don't need to recalculate as that happened with logout.
                DataFunctions.CalculateFull();
            }
        }

        private void OnUserChanged(object? sender, EventArgs e) => DataFunctions.CalculateFull();

        public object? GetFoo(string? a1, string? a2)
        {
            throw new NotImplementedException();
        }
    }
}
