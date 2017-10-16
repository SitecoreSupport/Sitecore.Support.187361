using Sitecore.Configuration;
using System;
using System.Collections.Specialized;
using System.Globalization;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Pipelines.HasPresentation;
using Sitecore.Publishing;
using Sitecore.Shell.DeviceSimulation;
using Sitecore.Sites;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using Sitecore.ExperienceEditor.Utils;
using Sitecore.Shell.Framework.Commands;


namespace Sitecore.Support.Shell.Framework.Commands.System
{
    [Serializable]
    public class Preview : Command
    {
        /// <summary>
        /// Executes the command in the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public override void Execute(CommandContext context)
        {
            //Items.Preview();
            Assert.ArgumentNotNull((object)context, "context");
            NameValueCollection parameters = new NameValueCollection();
            bool flag = false;
            if (context.Items.Length == 1)
            {
                Item obj = context.Items[0];
                parameters["uri"] = obj.Uri.ToString();
                parameters.Add("sc_lang", obj.Language.ToString());
                //parameters.Add("sc_version", obj.Version.Number.ToString((IFormatProvider)CultureInfo.InvariantCulture));
                if (HasPresentationPipeline.Run(obj))
                    parameters.Add("sc_itemid", obj.ID.ToString());
             
            }
            ClientPipelineArgs args = new ClientPipelineArgs(parameters);
          
            Context.ClientPage.Start((object)this, "Run", args);
        }

        /// <summary>
        /// Queries the state of the command.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The state of the command.</returns>
        public override CommandState QueryState(CommandContext context)
        {
            if (!Settings.Preview.Enabled)
            {
                return CommandState.Hidden;
            }
            return CommandState.Enabled;
        }

        protected void Run(ClientPipelineArgs args)
        {
            UrlString urlString = new UrlString("/");
            urlString.Add("sc_mode", "preview");
            if (!string.IsNullOrEmpty(args.Parameters["sc_itemid"]))
                urlString.Add("sc_itemid", args.Parameters["sc_itemid"]);
            //if (!string.IsNullOrEmpty(args.Parameters["sc_version"]))
            //    urlString.Add("sc_version", args.Parameters["sc_version"]);
            SiteContext siteContext = (SiteContext)null;
            if (!string.IsNullOrEmpty(args.Parameters["uri"]))
            {
                Item obj = Database.GetItem(ItemUri.Parse(args.Parameters["uri"]));
                if (obj == null)
                {
                    SheerResponse.Alert("Item not found.");
                    return;
                }
                siteContext = LinkManager.GetPreviewSiteContext(obj);
            }

            // get start item of current site
            if (siteContext == null)
            {
                //compare site name in case there are any wild cards in it

                foreach (SiteInfo current in SiteContextFactory.Sites)
                {
                    string host = WebUtil.GetRequestUri().Host.ToLowerInvariant();
                    string currentHost = current.HostName.ToLowerInvariant();
                    if (!string.IsNullOrEmpty(currentHost))
                    {
                        var hostNames = currentHost.Split('|');
                        foreach (var hostName in hostNames)
                        {
                            var splitIt = hostName.Split('*');
                            if (splitIt.Length > 1)
                            {
                                string startWith = splitIt[0];
                                string endWith = splitIt[splitIt.Length - 1];

                                if (host.StartsWith(startWith, StringComparison.OrdinalIgnoreCase) &&
                                    host.EndsWith(endWith, StringComparison.OrdinalIgnoreCase))
                                {
                                    siteContext = new SiteContext(current);
                                    break;
                                }
                            }
                            else
                            {
                                siteContext = new SiteContext(current);
                                break;

                            }
                        }
                    }
                }
            }


            SiteContext site = siteContext ?? Factory.GetSite(Settings.Preview.DefaultSite);
            if (site == null)
            {
                SheerResponse.Alert(Translate.Text("Site \"{0}\" not found",
                    (object)Settings.Preview.DefaultSite));
            }
            else
            {
                string parameter = args.Parameters["sc_lang"];
                if (string.IsNullOrEmpty(parameter))
                    parameter = WebEditUtility.ResolveContentLanguage(site).ToString();
                if (!string.IsNullOrEmpty(args.Parameters["sc_lang"]))
                    urlString.Add("sc_lang", parameter);
                urlString["sc_site"] = site.Name;
                PreviewManager.RestoreUser();
                Context.ClientPage.ClientResponse.Eval("window.open('" + (object)urlString + "', '_blank')");
            }
        }
    }
}
