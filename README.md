# Craft_V_11

## Build before pushing

Install the repository's Git hooks once in each clone:

```sh
git config --local core.hooksPath .githooks
```

After that, `git push` builds the full solution in Release configuration and stops if the build fails or the .NET SDK is missing. Run `dotnet build Craft_V_11.slnx --configuration Release` at any time to check the build before pushing.

Git hooks are local to each clone and can be bypassed with `git push --no-verify`. The existing CI workflow provides an additional check for Craft.Extensions.
