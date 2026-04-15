# HelloCli

A tiny System.CommandLine app that uses `Microsoft.Extensions.DependencyInjection` and
references `Clidoc.SystemCommandLine` to produce its own `commands.json`.

```bash
dotnet run --project samples/HelloCli -- greet World --loud
dotnet run --project samples/HelloCli -- commands --output /tmp/hello.json
clidoc generate /tmp/hello.json --output /tmp/hello-docs
```

Open `/tmp/hello-docs/commands.html` to see the generated site.
