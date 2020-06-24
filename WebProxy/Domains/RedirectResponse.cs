namespace WebProxy.Domains
{
    public class RedirectResponse
    {
        public string Body { get; set; }
        public string UrlRedirect { get; set; }

        /// <summary>
        /// Http код
        /// </summary>
        public int HttpCode { get; set; }
    }
}
