using System.Collections.Specialized;
using System.IO;
using System.Web;

namespace Raven.Http.Abstractions
{
	public class HttpResponseAdapter : IHttpResponse
	{
		private readonly HttpResponse response;

		public HttpResponseAdapter(HttpResponse response)
		{
			this.response = response;
		}

        public string RedirectionPrefix { get; set; }

	    public NameValueCollection Headers
		{
			get { return response.Headers; }
		}

		public Stream OutputStream
		{
			get { return response.OutputStream; }
		}

		public long ContentLength64
		{
			get { return -1; }
			set { }
		}

		public int StatusCode
		{
			get { return response.StatusCode; }
			set { response.StatusCode = value; }
		}

		public string StatusDescription
		{
			get { return response.StatusDescription; }
			set { response.StatusDescription = value; }
		}

		public void Redirect(string url)
		{
			response.Redirect(RedirectionPrefix + url, false);
		}

		public void Close()
		{
			response.Close();
		}

		public string ContentType
		{
			get { return response.ContentType; }
			set { response.ContentType = value; }
		}
	}
}
