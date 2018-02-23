# VSTSClient
Client application to talk to Visual Studio Team Services and perform administration tasks.

Currently supported:

* Get a list of collections from the server
* Get a list of projects from the server
* Get a list of work item types in a process
* Get the number of workitems of a specific work item type in a project
* Convert all work items from a certain work item type to another one

Don't forget to add a secrets.config and include the VSTS Url and PAT in it.See LoadSecrets().
