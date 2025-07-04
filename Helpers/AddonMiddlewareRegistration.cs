using System.Web.Http;

namespace Addon.Helpers
{
    public static class AddonMiddlewareRegistration
    {
        public static void RegisterMessageHandlers(HttpConfiguration config)
        {
            // ลงทะเบียน Message Handler ของ Addon
            config.MessageHandlers.Add(new Addon.Handlers.AddonApiVersionRedirectHandler());
        }
    }
}
