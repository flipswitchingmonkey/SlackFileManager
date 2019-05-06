# SlackFileManager

A C#/WPF desktop wrapper for the Slack Web API to make managing files a little easier.

Slack, for whatever reason, does not offer a proper interface for administrators to manage uploaded files in their Slack environment. The only way is some opaque procedure by asking Support to delete them all for them, which is not ideal.

However, Slack _does_ offer quite an extensive API which will let you files.list and file.delete. Thus, this application.

The target audience are Slack owners or administrators, not end users.

- You'll need to register an app under https://api.slack.com/apps to receive an Oauth Token to be used for authentication.
- You'll also need to set up the following permissions:
channels:read
groups:read
files:read
files:write:user
users:read

Since the API is rate-limited, requests for a large amount of file listings are made at intervals. Same goes for deletion.

