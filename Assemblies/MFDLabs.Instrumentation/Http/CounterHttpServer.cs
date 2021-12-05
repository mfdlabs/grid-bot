using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MFDLabs.Instrumentation
{
    public sealed class CounterHttpServer
    {
        public CounterHttpServer(ICounterRegistry counterRegistry, int portNumber, Action<Exception> exceptionHandler)
        {
            if (portNumber < _MinPortNumber || portNumber > _MaxPortNumber) 
                throw new ArgumentOutOfRangeException(nameof(portNumber), string.Format("Invalid value port portNumber: {0}.  Must be between {1} and {2} inclusive.", portNumber, _MinPortNumber, _MaxPortNumber));
            _PortNumber = portNumber;
            _CounterRegistry = counterRegistry ?? throw new ArgumentNullException(nameof(counterRegistry));
            _ExceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        }

        public void Start()
        {
            _IsRunning = true;
            var context = default(HttpListenerContext);
            Task.Run(() =>
            {
                _HttpListener = new HttpListener();
                _HttpListener.Prefixes.Add($"http://*:{_PortNumber}/");
                _HttpListener.Start();
                while (_IsRunning)
                {
                    try
                    {
                        context = _HttpListener.GetContext();
                        Task.Run(() => HandleRequest(context));
                    }
                    catch (Exception ex) { _ExceptionHandler(ex); }
                }
            });
        }
        public void Stop()
        {
            _IsRunning = false;
            _HttpListener?.Close();
            _HttpListener = null;
        }
        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                var counters = (from counter in _CounterRegistry.GetCounterValues()
                                orderby counter.Key.Category, counter.Key.Name, counter.Key.Instance
                                select counter).ToArray();
                var responseBuilder = new StringBuilder();
                responseBuilder.AppendLine("<table border=\"1\" cellpadding=\"3\" cellspacing=\"0\">");
                responseBuilder.AppendLine("<tr><th>Category</th><th>Name</th><th>Instance</th><th>Value</th></tr>");
                foreach (var counter in counters)
                {
                    var escapedCategory = WebUtility.HtmlEncode(counter.Key.Category);
                    var escapedName = WebUtility.HtmlEncode(counter.Key.Name);
                    var escapedInstance = WebUtility.HtmlEncode(counter.Key.Instance);
                    responseBuilder.AppendLine(
                        string.Format(
                            "<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td></tr>",
                            escapedCategory,
                            escapedName,
                            escapedInstance,
                            counter.Value
                        )
                    );
                }
                responseBuilder.AppendLine("</table>");
                var encodedResponse = Encoding.UTF8.GetBytes(responseBuilder.ToString());
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/html";
                context.Response.OutputStream.Write(encodedResponse, 0, encodedResponse.Length);
                context.Response.OutputStream.Close();
            }
            catch (Exception ex) { _ExceptionHandler(ex); }
        }

        private const int _MaxPortNumber = 49151;
        private const int _MinPortNumber = 0;
        private readonly ICounterRegistry _CounterRegistry;
        private readonly int _PortNumber;
        private readonly Action<Exception> _ExceptionHandler;
        private HttpListener _HttpListener;
        private bool _IsRunning;
    }
}
