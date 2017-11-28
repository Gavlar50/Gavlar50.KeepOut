#Gavlar50.KeepOut

Whereas Umbraco uses member groups to grant access to content, KeepOut lets you restrict access to content based on group membership. It has been completely rewritten for Umbraco 7 and is now much simpler to set up and configure. Note KeepOut does not work with sites which use anonymous access.

## Features new in this version:
* Rules and config are now added via content, no need to edit the web.config/restart site.
* Multiple independent rules. Each rule has its own settings so each rule can target different member groups and point to different no-access pages.
* Rules can be visualised in the content tree to see how the rules restrict access, The rule colour and content colour correspond so it is easy to see which rules control which content. Switch on this setting in the config node if required. It is off by default.

## Installation
The install creates a KeepOut Security folder in the root of the site. This folder contains the configuration. Add rules to this folder. Each rule contains the following settings:
* Page to secure. This page and all children are secured.
* No access page. The page redirected to when the member has no access
* Member groups who are denied access
* The colour used to visualise the rule in the content tree (when visualisation is enabled)