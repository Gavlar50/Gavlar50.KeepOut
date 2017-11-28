using System;
using System.Linq;
using System.Web;
using Umbraco.Core;
using Umbraco.Web;
using Umbraco.Web.Trees;
using System.Collections.Generic;
using Gavlar50.KeepOut.Models;
using Gavlar50.KeepOut.Helpers;
using Umbraco.Core.Services;
using Umbraco.Core.Models;
using Umbraco.Core.Events;

namespace Gavlar50.Umbraco.KeepOut.Handlers
{
    public class KeepOutHandler : ApplicationEventHandler
    {
        /// <summary>
        /// The list of rules as defined in Umbraco
        /// </summary>
        private List<KeepOutRule> Rules { get; set; }
        /// <summary>
        /// The secured root page ids, used to test curent page security 
        /// </summary>
        private List<string> RulesPages { get; set; }

        /// <summary>
        /// The parent folder that contains the rules and config
        /// </summary>
        private IContent KeepOutRulesFolder { get; set; }

        /// <summary>
        /// Gets or sets whether the rules are visualised in the content tree
        /// </summary>
        private bool VisualiseRules { get; set; }

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication,
           ApplicationContext applicationContext)
        {
            //Listen for the ApplicationInit event which then allows us to bind to the HttpApplication events.
            UmbracoApplicationBase.ApplicationInit += UmbracoApplicationBase_ApplicationInit;

            // Find and store the KeepOut Rules Folder - must exist at root
            var cs = applicationContext.Services.ContentService;
            KeepOutRulesFolder = cs.GetRootContent().FirstOrDefault(x => x.ContentType.Alias == "keepOutSecurityRules");
            if (KeepOutRulesFolder == null)
            {
            //    //TODO log here for no folder
                return;
            }

            // Load and store the rules
            RefreshRules();

            // Load and set the config
            RefreshConfig();

            // allow us to colour the nodes on render in the backend
            TreeControllerBase.TreeNodesRendering += TreeControllerBase_TreeNodesRendering;

            // subscribe to the publish event so the rules can be reloaded
            ContentService.Published += ContentService_Published;
        }

        private void ContentService_Published(global::Umbraco.Core.Publishing.IPublishingStrategy sender, global::Umbraco.Core.Events.PublishEventArgs<global::Umbraco.Core.Models.IContent> e)
        {
            // if any rules or config were published,show the reminder message to refresh the display
            if (e.PublishedEntities.Any(x => x.ContentType.Alias == "keepOutSecurityRule" || x.ContentType.Alias == "keepOutSecurityConfig"))
            {
                RefreshRules(); // ensure rules are updated on the fly
                RefreshConfig();// likewise the config
                e.Messages.Add(new EventMessage("KeepOut Security", "KeepOut Security updated. Refresh the node tree to reflect changes",EventMessageType.Warning));
            }
        }

        private void TreeControllerBase_TreeNodesRendering(TreeControllerBase sender, TreeNodesRenderingEventArgs e)
        {
            if (!VisualiseRules) return;
            switch (sender.TreeAlias)
            {
                case "content":
                    foreach (var node in e.Nodes)
                    {
                        // if the current node is secured by a rule, find it and colour the node
                        var id = int.Parse(node.Id.ToString());
                        // need IContent object to gain access to Path, node Path is always null :(
                        //var page = ApplicationContext.Current.Services.ContentService.GetById(id);
                        // get IPublishedContent from cache
                        var page = UmbracoContext.Current.ContentCache.GetById(id);
                        if (page == null) break;
                        var path = page.Path.Split(new[] { ',' }).ToList();
                        var hasRule = RulesPages.Intersect(path);
                        if (hasRule.Any())
                        {
                            var ruleIndex = RulesPages.IndexOf(hasRule.First()); // if multiple rules overlap, take the first
                            var activeRule = Rules[ruleIndex];
                            node.CssClasses.Add("keepout");
                            node.CssClasses.Add(activeRule.Colour);
                        }

                        // colour the actual rule node to indicate the rule assignment
                        if (page.ContentType.Alias == "keepOutSecurityRule")
                        {
                            node.CssClasses.Add("keepout");
                            var colourJson = page.GetProperty("contentColour").DataValue.ToString();
                            node.CssClasses.Add("keepout-" + KeepOutHelper.Json.Deserialize<RuleColour>(colourJson).Label);
                        }
                    }
                   
                    break;
            }
        }

        /// <summary>
        /// The ApplicationInit event which then allows us to bind to the HttpApplication events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UmbracoApplicationBase_ApplicationInit(object sender, EventArgs e)
        {
            var umbracoApp = (HttpApplication)sender;
            umbracoApp.PreRequestHandlerExecute += UmbracoApplication_PreRequestHandlerExecute;
        }

        /// <summary>
        /// The event that fires whenever a resource is requested
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UmbracoApplication_PreRequestHandlerExecute(object sender, EventArgs e)
        {
            var context = ((UmbracoApplicationBase)sender).Context;

            // if no context, session or anonymous user return
            if (context == null) return;
            if (context.Session == null) return;
            if (context.Request.LogonUserIdentity == null) return;
            if (context.Items == null) return;

            var umbPageId = context.Items["pageID"];
            var umbPage = context.Items["UmbPage"];
            var umbracoContext = (UmbracoContext)context.Items["Umbraco.Web.UmbracoContext"];
            if (umbPageId == null || umbPage == null || umbracoContext == null) return;

            // if accessing via the umbraco admin return
            var isUmbraco = umbPage.ToString().StartsWith("/umbraco/");
            if (isUmbraco) return;
            var umbracoHelper = new UmbracoHelper(umbracoContext);
            if (!umbracoContext.PageId.HasValue) return;

            // First we check if this page is part of a rule
            var page = umbracoHelper.TypedContent(umbracoContext.PageId.Value);
            var path = page.Path.Split(new[] { ',' }).ToList();
            // if the current page should be secured, the page path will contain the root page that was secured
            // this is how we know that this is a descendant of the secured page
            var hasRule = RulesPages.Intersect(path); 
            if (hasRule.Any())
            {
                var ruleIndex = RulesPages.IndexOf(hasRule.First()); // if multiple rules overlap, take the first
                var activeRule = Rules[ruleIndex];

                // now we have found a rule we check if it applies to the current member
                var memberRoles = System.Web.Security.Roles.GetRolesForUser(context.Request.LogonUserIdentity.Name).ToList();
                if (!memberRoles.Any()) return;
                var appliesToUser = activeRule.MemberRoles.Intersect(memberRoles);
                if (appliesToUser.Any())
                {
                    // member is in a group that has been denied access, so redirect to the no access page defined by the rule
                    var noAccessPage = umbracoHelper.NiceUrl(activeRule.NoAccessPage);
                    umbracoContext.HttpContext.Response.Redirect(noAccessPage);
                }
            }
        }

        /// <summary>
        /// Refresh the rules so any changes are reflected immediately without site restart
        /// </summary>
        private void RefreshRules()
        {
            var cs = ApplicationContext.Current.Services.ContentService;
            Rules = new List<KeepOutRule>();
            RulesPages = new List<string>();
            var rules = cs.GetChildren(KeepOutRulesFolder.Id).Where(x => x.ContentType.Alias == "keepOutSecurityRule").ToList();
            foreach (var rule in rules)
            {
                var keepOutRule = new KeepOutRule(rule);
                Rules.Add(keepOutRule);
                RulesPages.Add(keepOutRule.PageToSecure.ToString());
            }
        }

        /// <summary>
        /// Refresh the config so any changes are reflected immediately without site restart
        /// </summary>
        private void RefreshConfig()
        {
            var cs = ApplicationContext.Current.Services.ContentService;
            var config = cs.GetChildren(KeepOutRulesFolder.Id).FirstOrDefault(x => x.ContentType.Alias == "keepOutSecurityConfig");
            if (config == null)
            {
                VisualiseRules = false;
            }
            else
            {
                VisualiseRules = config.Properties["showRuleCoverage"].Value.ToString() == "1" ? true : false;
            }
        }
    }
}