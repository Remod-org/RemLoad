# RemLoad - A remote plugin loader for Rust Oxide

### What is this?
 *  RemLoad is a Harmony mod plugin that patches into the CSharpPluginLoader to detect and intercept files when called with http(s) in the directory name.  This is just a proof of concept, but is usable.

### What it does:
 1. Provides for a basic means of retrieving plugins remotely.

 2. Allows for basic authentication at the remote site.

 3. Deletes the remotely-loaded plugin(s) once this plugin itself is unloaded.

### What it does not do:
 1. Secure the downloaded file(s).  This makes it perhaps acceptable in the traditional sense of paid plugins, but does not offer a true subscription model.

 See the one command here, rload.  This will show you how to call a manual load of your remote plugin.  The plugin will be downloaded, assuming auth is present and works, into the standard plugins folder.

If you can make this better, please do.  But, let me know what you've done so it can be improved for everyone.  By nature of the licensing, sharing of the code is required on request.  Please make it available anyway.

## Configuration
```json
{
  "Options": {
    "Provider": "remod",
    "Username": "remload",
    "Key": "FAKEKEY"
  },
  "debug": false,
  "Version": {
    "Major": 1,
    "Minor": 0,
    "Patch": 1
  }
}
```

## Basic how-to
  1. Setup a private folder on your site (Apache used here as an example.
  2. Add an .htaccess file for Apache, which may be as simple as:

```
AuthType Basic
AuthName "Authentication Required"
AuthUserFile "/etc/htpasswd"
Require valid-user

Order allow,deny
Allow from all
```
  3. Ensure that you create the AuthUserFile in the right location (not in your web tree, please).

  4. Add plugin files to the folder.


Note that this could be used with multiple "providers," but would require that those be added to the code:

```cs
    private Dictionary<string, string> sites = new Dictionary<string, string>()
    {
        { "remod", "https://code.remod.org/private/" },
        { "yours", "https://yoursitehere/private/" }
    };
```

These providers could also simply point to folders with each protected plugin, one per folder, etc.

