using System;
using System.Web.Script.Serialization;
using Umbraco.Core;

namespace Gavlar50.KeepOut.Helpers
{
    public static class KeepOutHelper
    {
        public static JavaScriptSerializer Json = new JavaScriptSerializer();

        /// <summary>
        /// Returns the page id from its version
        /// </summary>
        /// <param name="version">The guid of the current page</param>
        /// <returns>int page id</returns>
        public static int GetIdByVersion(Guid version)
        {
            var page = ApplicationContext.Current.Services.ContentService.GetById(version);
            return page == null ? 0 : page.Id;
        }
    }
}