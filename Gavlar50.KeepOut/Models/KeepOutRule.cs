using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Umbraco.Core.Models;
using Gavlar50.KeepOut.Helpers;

namespace Gavlar50.KeepOut.Models
{
    public class KeepOutRule
    {
        private const string _regex = "(?<=umb://document/)[a-f0-9A-F]{32}"; // match on string beginning umb://document/ but don't include it in the match results, we just want the guid 
        public int NoAccessPage { get; set; }
        public int PageToSecure { get; set; }
        public List<string> MemberRoles { get; set; }
        public string Colour { get; set; }

        public KeepOutRule(){ }

        public KeepOutRule(IContent rule) {
            
            var noAccessVersion = Guid.Parse(Regex.Match(rule.Properties["noAccessPage"].Value.ToString(), _regex).Value);
            var pageVersionToSecure = Guid.Parse(Regex.Match(rule.Properties["pageToSecure"].Value.ToString(), _regex).Value);
            NoAccessPage = KeepOutHelper.GetIdByVersion(noAccessVersion);
            PageToSecure = KeepOutHelper.GetIdByVersion(pageVersionToSecure);
            MemberRoles = new List<string>();
            MemberRoles.AddRange(rule.Properties["deniedMemberGroups"].Value.ToString().Split(new[] { ',' }).ToList());
            var colourJson = rule.Properties["contentColour"].Value.ToString();
            Colour = "keepout-" + KeepOutHelper.Json.Deserialize<RuleColour>(rule.Properties["contentColour"].Value.ToString()).Label;
        }
    }
}