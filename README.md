
Start of a toolset to bulk edit workitems in VSTS and bulk edit process templates.  
**Note**: check the wiki for parameter usage.

# VSTSClient.WIT
Client application to talk to Visual Studio Team Services and perform administration tasks.

Currently supported:

* Get a list of collections from the server
* Get a list of projects from the server
* Get a list of work item types in a process
* Get the number of workitems of a specific work item type in a project
* Convert all work items from a certain work item type to another one

Don't forget to add a secrets.config and include the VSTS Url and PAT in it.See LoadSecrets().


# VSTSClient.ProcessTemplate
Client application to talk to Visual Studio Team Services to export, change and import ProcessTemplate packages in bulk.

Currently supported:

* Export a list of process templates to disk

Use that list to perform these tasks:  
* Export the process template to zip files
* Unzip these files
* Perform checks on them to figure out if all templates are alike
* Update part of the template (e.g. update 'Epic.xml')
* Rezip these files
* Import these process templates back into VSTS
