namespace Addon.Service
{
    public class VendorPortalService
    {
        private readonly HttpClient _client;
        private readonly string openAPIUrl;
        private NameValueCollection config;

        public VendorPortalService(string conn)
        {
            config = Ext.GetAppSetting();
            _client = new HttpClient();
            openAPIUrl = config["VendorPortalURL"] ?? "";
            _client.BaseAddress = new Uri(openAPIUrl);
        }

        public async Task<string> GetVendorPortalData(string endpoint)
        {
            try
            {
                var response = await _client.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                // Handle exceptions as needed
                throw new Exception("Error fetching data from Vendor Portal", ex);
            }
    }
}