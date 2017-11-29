# Gavlar50.KeepOut

Whereas Umbraco uses member groups to grant access to content, KeepOut lets you restrict access to content based on group membership. Note KeepOut does not work with sites which use anonymous access.

## V2.0.1 New in this version
Added umbraco logging when debug enabled to allow you to test access restrictions. Search umbracotracelog for DEBUG Gavlar50.Umbraco.KeepOut.Handlers.KeepOutHandler items.

## V2.0.0 Features in this version:
* Rules are added via content, no need to edit the web.config/restart site.
* Multiple independent rules. Each rule has its own settings so each rule can target different member groups and point to different no-access pages.
* Rules can be visualised in the content tree to see how the rules restrict access, The rule colour and content colour correspond so it is easy to see which rules control which content. Switch on this setting in the config node if required. It is off by default.

## Installation
The install creates a KeepOut Security Rules node in the root of the site. This node contains the configuration. Add rules under this node. Each rule contains the following settings:
* Page to secure. This page and all children are secured.
* No access page. The page redirected to when the member has no access
* Member groups who are denied access
* The colour used to visualise the rule in the content tree (when visualisation is enabled)
