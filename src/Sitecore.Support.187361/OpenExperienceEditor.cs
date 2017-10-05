using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.ExperienceEditor.Utils;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Pipelines.HasPresentation;
using Sitecore.Publishing;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Sites;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Specialized;
using System.Globalization;
using Sitecore.Web;

namespace Sitecore.Support.Shell.Applications.WebEdit.Commands
{
    [Serializable]
    public class OpenExperienceEditor : Command
    {
        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull((object)context, "context");
            NameValueCollection parameters = new NameValueCollection();
            bool flag = false;
            if (context.Items.Length == 1)
            {
                Item obj = context.Items[0];
                parameters["uri"] = obj.Uri.ToString();
                parameters.Add("sc_lang", obj.Language.ToString());
                parameters.Add("sc_version",
                obj.Version.Number.ToString((IFormatProvider)CultureInfo.InvariantCulture));
                if (HasPresentationPipeline.Run(obj))
                    parameters.Add("sc_itemid", obj.ID.ToString());
                else
                    flag = true;
            }
            ClientPipelineArgs args = new ClientPipelineArgs(parameters);
            if (!flag)
            {
                args.Result = "yes";
                args.Parameters.Add("needconfirmation", "false");
            }
            Context.ClientPage.Start((object)this, "Run", args);
        }

        public override CommandState QueryState(CommandContext context)
        {
            Assert.ArgumentNotNull((object)context, "context");
            if (UIUtil.IsIE() && UIUtil.GetBrowserMajorVersion() < 7 || !Settings.WebEdit.Enabled)
                return CommandState.Hidden;
            return base.QueryState(context);
        }

        protected void Run(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull((object)args, "args");
            if (!SheerResponse.CheckModified())
                return;
            if (args.Parameters["needconfirmation"] == "false" || args.IsPostBack)
            {
                if (args.Result == "no")
                    return;
                UrlString urlString = new UrlString("/");
                urlString.Add("sc_mode", "edit");
                if (!string.IsNullOrEmpty(args.Parameters["sc_itemid"]))
                    urlString.Add("sc_itemid", args.Parameters["sc_itemid"]);
                if (!string.IsNullOrEmpty(args.Parameters["sc_version"]))
                    urlString.Add("sc_version", args.Parameters["sc_version"]);
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
                    foreach (SiteInfo current in SiteContextFactory.Sites)
                    {
                        if (current.HostName.ToLowerInvariant().Equals(WebUtil.GetRequestUri().Host.ToLowerInvariant()))
                        {
                            siteContext = new SiteContext(current);
                            break;
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
            else
            {
                SheerResponse.Confirm(
                    "The current item does not have a layout for the current device.\n\nDo you want to open the start Web page instead?");
                args.WaitForPostBack();
            }
        }
    }
}
